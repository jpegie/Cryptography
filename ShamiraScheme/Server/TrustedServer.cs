using NetMQ.Sockets;
using NetMQ;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using Server.Helpers;
using SecretSharing.SecretEncryption;

namespace Server;
internal class TrustedServer
{
    RouterSocket _socket;
    Dictionary<string, BigInteger> _registratedUsers;
    BigInteger _modulo, _p, _q;
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

    private void HandleReceivingMessage(NetMQMessage message)
    {
        var clientMessage = MessagingHelper.ParseValuedMessage(message);
        switch (clientMessage!.Type)
        {
            case MessageType.Registration:
                RegisterClient(clientMessage);
                break;
            case MessageType.Modulo:
                var responseMessage = clientMessage.Clone(withFrames: false);
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
            _registratedUsers.Add(message.Sender, BigInteger.Parse(message.Frames[FramesNames.PUBLIC_KEY].ToString()!));
        message.AddFrame(FramesNames.STATUS, "Registration completed");
        MessagingHelper.Response(_socket, message, responseBy: Consts.SERVER_IDENTITY);
    }
}
