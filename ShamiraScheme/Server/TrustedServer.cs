using NetMQ.Sockets;
using NetMQ;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using Server.Helpers;
using SecretSharing.SecretEncryption;
using System.Security;

namespace Server;
internal class TrustedServer
{
    const long CLIENT_STARTER_BALANCE = 100;
    const long MAX_BANKNOTE_VALUE = 21; //10101 -> 13*7*3
    /*
     * Возможная сдача:
     * 1) 1
     * 2) 4
     * 3) 16
     * 4) 17
     * 4) 20
     */
    RouterSocket _socket;
    Dictionary<string, BigInteger> _registratedUsers;
    Dictionary<string, long> _clientsBalances;
    BigInteger _modulo, _p, _q;
    BigInteger _secretExponent;
    private int _sharesCount;
    public TrustedServer(BigInteger p, BigInteger q, string host)
    {
        _p = p; 
        _q = q;
        _modulo = _p * _q;
        _socket = new RouterSocket($"@{host}:{Consts.PORT}");
        _socket.Options.Identity = Encoding.UTF8.GetBytes(Consts.SERVER_IDENTITY);
        _socket.Options.RouterHandover = true;
        _registratedUsers = new Dictionary<string, BigInteger>();
        _clientsBalances = new Dictionary<string, long>();
        _secretExponent = ExponentHelper.GetExponentWithModulo(_q, _p, MAX_BANKNOTE_VALUE);
    }
    public void StartReceiving()
    {
        Console.WriteLine("Server started...");
        while (true)
        {
            var message = _socket.ReceiveMultipartMessage();
            HandleReceivingMessage(message);
        }
    }

    private BigInteger SecretExponent
    {
        get => _secretExponent;
    }
    private void HandleReceivingMessage(NetMQMessage message)
    {
        var clientMessage = MessagingHelper.ParseValuedMessage(message);
        ValuedMessage? responseMessage = null;
        BigInteger exponent = 0;
        switch (clientMessage!.Type)
        {
            case MessageType.BanknoteVerification:
                var sign = BigInteger.Parse(clientMessage.Frames[FramesNames.SIGNED_BANKNOTE].ToString()!);
                var unsignedBanknote = clientMessage.Frames[FramesNames.BANKNOTE_TO_VERIFY];
                var banknoteValue = (long)clientMessage.Frames[FramesNames.BANKNOTE_VALUE];
                exponent = ExponentHelper.GetExponentWithModulo(_q, _p, banknoteValue);
                var repeatedSign = SignBanknote(clientMessage, exponent);
                var isVerified = sign == repeatedSign;
                responseMessage = clientMessage.Clone(withFrames: false);
                responseMessage.AddFrame(FramesNames.BANKNOTE_TO_VERIFY, unsignedBanknote);
                responseMessage.AddFrame(FramesNames.VERIFICATION_STATUS, isVerified.ToString());
                MessagingHelper.Response(_socket, responseMessage, responseBy: Consts.SERVER_IDENTITY);
                break;
            case MessageType.BanknoteRequest:
                _clientsBalances[clientMessage.Sender] -= MAX_BANKNOTE_VALUE;
                responseMessage = clientMessage.Clone(withFrames: true);
                responseMessage.Type = MessageType.BanknoteResponse;
                var signedBanknote = SignBanknote(clientMessage, _secretExponent);
                responseMessage.AddFrame(FramesNames.SIGNED_BANKNOTE, signedBanknote.ToString());
                responseMessage.AddFrame(FramesNames.BALANCE, _clientsBalances[clientMessage.Sender]);
                MessagingHelper.Response(_socket, responseMessage, responseBy: Consts.SERVER_IDENTITY);
                break;
            case MessageType.Payment:
                var costValue = (long)clientMessage.Frames[FramesNames.COST_VALUE];
                var changeValue = (long)clientMessage.Frames[FramesNames.CHANGE_VALUE];
                var costString = (string)clientMessage.Frames[FramesNames.COST]; // эту купюру нужно отправить продавцу, то есть получателю сообщения (она уже подписана)
                var changeUnsigned = (string)clientMessage.Frames[FramesNames.CHANGE_TO_SIGN]; //эту купюру нужно подписать с помощью обратного для значения сдачи и отправить покупателю, то есть отправителю сообщения
                
                _clientsBalances[clientMessage.Sender] += changeValue;
                _clientsBalances[clientMessage.Receiver] += costValue;

                exponent = ExponentHelper.GetExponentWithModulo(_q, _p, changeValue);
                var changeSigned = SignBanknote(clientMessage, exponent);

                responseMessage = clientMessage.Clone(withFrames: false);
                responseMessage.Type = MessageType.BanknoteResponse;
                responseMessage.AddFrame(FramesNames.UNSIGNED_BANKNOTE, changeUnsigned.ToString());
                responseMessage.AddFrame(FramesNames.SIGNED_BANKNOTE, changeSigned.ToString());
                responseMessage.AddFrame(FramesNames.BALANCE, _clientsBalances[clientMessage.Sender]);
                MessagingHelper.Response(_socket, responseMessage, responseBy: Consts.SERVER_IDENTITY);

                var messageToSeller = new ValuedMessage(clientMessage.Sender, clientMessage.Receiver, MessageType.Payment);
                messageToSeller.AddFrame(FramesNames.SIGNED_BANKNOTE, costString);
                messageToSeller.AddFrame(FramesNames.BANKNOTE_VALUE, costValue);
                messageToSeller.AddFrame(FramesNames.BALANCE, _clientsBalances[clientMessage.Receiver]);
                //отправка денег продавцу 
                var msg = MessagingHelper.ComposeMessage(
                    messageToSeller.Receiver,
                    MessagingHelper.SerializeMessage(messageToSeller));
                MessagingHelper.TrySendMessage(_socket, msg);

                break;

            case MessageType.Registration:
                RegisterClient(clientMessage);
                break;
            case MessageType.Modulo:
                responseMessage = clientMessage.Clone(withFrames: false);
                responseMessage.AddFrame(FramesNames.MODULO, _modulo);
                MessagingHelper.Response(_socket, responseMessage, responseBy: Consts.SERVER_IDENTITY);
                break;
            case MessageType.Default:
                //на время верификации обработка сообщений отдается полностью методу VerifySenderGenuine
                var isUserVerified = VerifySenderGenuine(clientMessage);
                var responseMessageToSender = clientMessage.Clone();
                if (isUserVerified)
                {
                    responseMessageToSender.UpdateMessageFrame($"You are actual '{clientMessage.Sender}'");
                    MessagingHelper.Response(_socket, responseMessageToSender, responseBy: Consts.SERVER_IDENTITY);
                    //отправка сообщения получателю
                    var messageToClient = MessagingHelper.ComposeMessage(
                        clientMessage.Receiver,
                        MessagingHelper.SerializeMessage(clientMessage));
                    MessagingHelper.TrySendMessage(_socket, messageToClient);
                }
                else
                {
                    responseMessageToSender.UpdateMessageFrame($"You are dickhead, but not '{responseMessageToSender.Sender}'");
                    MessagingHelper.Response(_socket, responseMessageToSender, responseBy: Consts.SERVER_IDENTITY);
                }
                break;
            case MessageType.Encrypt:
                byte[] encryptedMessage = EncryptClientMessage(clientMessage);
                responseMessageToSender = clientMessage.Clone(withFrames: false);
                responseMessageToSender.Type = MessageType.Encrypt;
                responseMessageToSender.AddFrame(FramesNames.Data, Convert.ToHexString(encryptedMessage));
                MessagingHelper.Response(_socket, responseMessageToSender, responseBy: Consts.SERVER_IDENTITY);
                break;
            case MessageType.Decrypt:
                byte[] decryptedMessage = DecryptClientMessage(clientMessage);
                responseMessageToSender = clientMessage.Clone(withFrames: false);
                responseMessageToSender.Type = MessageType.Decrypt;
                responseMessageToSender.AddFrame(FramesNames.Data, Convert.ToHexString(decryptedMessage));
                MessagingHelper.Response(_socket, responseMessageToSender, responseBy: Consts.SERVER_IDENTITY);
                break;
        }
    }

    private byte[] EncryptClientMessage(ValuedMessage clientMessage)
    {
        int players = Convert.ToInt32(clientMessage.Frames[FramesNames.Players]);
        int required = Convert.ToInt32(clientMessage.Frames[FramesNames.Required]);
        _sharesCount = required;
        int polynomsCount = 3;
        var byteKey = KeyGenerator.GenerateKey(polynomsCount * 16);
        var key = KeyGenerator.GenerateDoubleBytesKey(byteKey);
        var hexKey = KeyGenerator.GetHexKey(key);
        byte[] message = Convert.FromHexString(clientMessage.Frames[FramesNames.Data].ToString()!);
        ShareKeys(key, players, required);
        return Encryption.Encrypt(message, hexKey);
    }
    private byte[] DecryptClientMessage(ValuedMessage clientMessage)
    {
        var dataToDecrypt = Convert.FromHexString(clientMessage.Frames[FramesNames.Data].ToString()!);
        var shares = new string[_sharesCount];

        var recievers = new List<string> { clientMessage.Sender };
        foreach(var reciever in _registratedUsers.Keys)
        {
            if (!recievers.Contains(reciever))
            {
                recievers.Add(reciever);
            }
        }

        var requestKeyMessage = new ValuedMessage(
            Consts.SERVER_IDENTITY,
            "",
            MessageType.KeyRequest);
        for (int i = 0; i < _sharesCount; i++)
        {
            requestKeyMessage.Receiver = recievers[i];
            requestKeyMessage.ClearFrames();
            requestKeyMessage.AddFrameAsRequestingValue(FramesNames.Key);
            var serealizedMessage = MessagingHelper.SerializeMessage(requestKeyMessage);
            var netMQMessage = MessagingHelper.ComposeMessage(requestKeyMessage.Receiver, serealizedMessage);
            MessagingHelper.TrySendMessage(_socket, netMQMessage);
            var key = MessagingHelper.ParseValuedMessage(_socket.ReceiveMultipartMessage());
            shares[i] = key!.Frames[FramesNames.Key].ToString()!;
        }
        var generatedKey = SharesManager.CombineKey(shares);
        var hexKey = KeyGenerator.GetHexKey(generatedKey);

        return Encryption.Decrypt(dataToDecrypt, hexKey);
    }

    private BigInteger SignBanknote(ValuedMessage message, BigInteger exponent)
    {
        var signingFrames = new string[]
        {
            FramesNames.BANKNOTE_TO_VERIFY,
            FramesNames.UNSIGNED_BANKNOTE,
            FramesNames.CHANGE_TO_SIGN
        };
        var signingFrame = signingFrames
            .Select(f => f)
            .Where(f => message.Frames.ContainsKey(f))
            .First();

        var banknoteToSignString = (string)message.Frames[signingFrame];
        var banknoteToSign = BigInteger.Parse(banknoteToSignString);
        var signedBanknote = BankHelper.SignBanknote(banknoteToSign, exponent, _modulo);
        return signedBanknote;
    }
    private void ShareKeys(ushort[] key, int players, int required)
    {
        var splitted = SharesManager.SplitKey(key, players, required);
        var keyMessage = new ValuedMessage(
          Consts.SERVER_IDENTITY,
          "",
          MessageType.KeyDelivery);
        var recievers = _registratedUsers.Keys.ToList();
        if (recievers.Count < required)
        {
            throw new Exception("Кол-во пользователей должно быть >= кол-ва требуемых пользователей для шифрования/расшифрования");
        }
        for (int i = 0; i < splitted.Length; i++)
        {
            keyMessage.Receiver = recievers[i];
            keyMessage.ClearFrames();
            keyMessage.AddFrame(FramesNames.Key, splitted[i]);
            var serialized = MessagingHelper.SerializeMessage(keyMessage);
            var message = MessagingHelper.ComposeMessage(keyMessage.Receiver, serialized);
            MessagingHelper.TrySendMessage(_socket, message);
        }
    }

    private bool VerifySenderGenuine(ValuedMessage message)
    {
        var isVerified = true;
        var verificationMessage = message.Clone();
        verificationMessage.UpdateMessageFrame("Verification started...");
        MessagingHelper.Response(_socket, verificationMessage, responseBy: Consts.SERVER_IDENTITY);
        verificationMessage.Type = MessageType.Verification;
        for (int round_i = 0; round_i < Consts.VERIFICATION_ROUNDS_AMOUNT; ++round_i)
        {
            verificationMessage.ClearFrames();
            verificationMessage.AddFrame(FramesNames.ROUND, round_i + 1);
            verificationMessage.AddFrameAsRequestingValue(FramesNames.X); //добавляю X как запрос
            MessagingHelper.Response(_socket, verificationMessage, responseBy: Consts.SERVER_IDENTITY); //отправка запроса на получение значения <x>
            var x = MessagingHelper.ParseValuedMessage(_socket.ReceiveMultipartMessage()); //получение <x> ответвым сообщением

            verificationMessage.ClearFrames(excludeFrame: FramesNames.ROUND);
            var e = RandomNumberGenerator.GetInt32(0, 2); //генерация <e>
            verificationMessage.AddFrameAsRequestingValue(FramesNames.Y); //y как запрос
            verificationMessage.AddFrame(FramesNames.E, e); //добавляю e как параметр
            MessagingHelper.Response(_socket, verificationMessage, responseBy: Consts.SERVER_IDENTITY); //отправка запроса на получение значения <x>

            var y = MessagingHelper.ParseValuedMessage(_socket.ReceiveMultipartMessage()); //получение <y> ответвым сообщением
            
            var yValue = BigInteger.Parse(y!.Frames[FramesNames.Y].ToString()!);
            var xValue = BigInteger.Parse(x!.Frames[FramesNames.X].ToString()!);

            if (yValue == 0)
            {
                isVerified = false;
                break;
            }
            else 
            {
                var leftSide = BigInteger.ModPow(yValue, 2, _modulo);
                var rightSide = xValue * BigInteger.ModPow(_registratedUsers[verificationMessage.Sender], e, _modulo) % _modulo;
                if (leftSide != rightSide)
                {
                    isVerified = false;
                    break;
                }
            }
        }
        verificationMessage.ClearFrames();
        verificationMessage.Type = MessageType.Default;
        verificationMessage.UpdateMessageFrame("Verification finished...");
        MessagingHelper.Response(_socket, verificationMessage, responseBy: Consts.SERVER_IDENTITY);
        return isVerified;
    }

    public void RegisterClient(ValuedMessage message)
    {
        if (!_registratedUsers.ContainsKey(message.Sender))
        {
            _registratedUsers.Add(message.Sender, BigInteger.Parse(message.Frames[FramesNames.PUBLIC_KEY].ToString()!));
            _clientsBalances.Add(message.Sender, CLIENT_STARTER_BALANCE);
        } 
        message.AddFrame(FramesNames.STATUS, "Registration completed");
        message.AddFrame(FramesNames.BALANCE, _clientsBalances[message.Sender]);
        message.AddFrame(FramesNames.MAX_BANKNOTE, MAX_BANKNOTE_VALUE);
        MessagingHelper.Response(_socket, message, responseBy: Consts.SERVER_IDENTITY);
    }
}
