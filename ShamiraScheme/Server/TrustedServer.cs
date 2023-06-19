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
    private void HandleReceivingMessage(NetMQMessage message)
    {
        var clientMessage = MessagingHelper.ParseValuedMessage(message);
        var unsignedBanknote = "";
        ValuedMessage? responseMessage = null;
        BigInteger exponent = 0;
        switch (clientMessage!.Type)
        {
            case MessageType.BanknoteVerification:
                unsignedBanknote = clientMessage.Frames[FramesNames.BANKNOTE_TO_VERIFY].ToString()!;
                var isVerified = BankHelper.VerifyBanknote(clientMessage, _p, _q, _modulo);
                responseMessage = clientMessage.Clone(withFrames: false);
                responseMessage.AddFrame(FramesNames.BANKNOTE_TO_VERIFY, unsignedBanknote);
                responseMessage.AddFrame(FramesNames.VERIFICATION_STATUS, isVerified.ToString());
                MessagingHelper.Response(_socket, responseMessage, responseBy: Consts.SERVER_IDENTITY);
                break;
            case MessageType.BanknoteRequest:
                _clientsBalances[clientMessage.Sender] -= MAX_BANKNOTE_VALUE;
                responseMessage = clientMessage.Clone(withFrames: true);
                responseMessage.Type = MessageType.BanknoteResponse;
                unsignedBanknote = clientMessage.Frames[FramesNames.UNSIGNED_BANKNOTE].ToString()!;
                var signedBanknote = BankHelper.SignBanknote(unsignedBanknote, _secretExponent, _modulo);
                responseMessage.AddFrame(FramesNames.SIGNED_BANKNOTE, signedBanknote.ToString());
                responseMessage.AddFrame(FramesNames.BALANCE, _clientsBalances[clientMessage.Sender]);
                MessagingHelper.Response(_socket, responseMessage, responseBy: Consts.SERVER_IDENTITY);
                break;
            case MessageType.Payment:
                var costValue = long.Parse(clientMessage.Frames[FramesNames.COST_VALUE].ToString()!);
                var changeValue = long.Parse(clientMessage.Frames[FramesNames.CHANGE_VALUE].ToString()!);
                var costString = clientMessage.Frames[FramesNames.COST].ToString()!; // эту купюру нужно отправить продавцу, то есть получателю сообщения (она уже подписана)
                var changeUnsigned = clientMessage.Frames[FramesNames.CHANGE_TO_SIGN].ToString()!; //эту купюру нужно подписать с помощью обратного для значения сдачи и отправить покупателю, то есть отправителю сообщения
                
                _clientsBalances[clientMessage.Sender] += changeValue;
                _clientsBalances[clientMessage.Receiver] += costValue;

                exponent = ExponentHelper.GetExponentWithModulo(_q, _p, changeValue);
                var changeSigned = BankHelper.SignBanknote(changeUnsigned, exponent, _modulo);

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
        }
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
