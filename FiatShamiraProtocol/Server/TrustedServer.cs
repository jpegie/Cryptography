using NetMQ.Sockets;
using NetMQ;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;
using Server.Helpers;

namespace Server;
internal class TrustedServer
{
    RouterSocket _socket;
    Dictionary<string, BigInteger> _registratedUsers;
    BigInteger _modulo, _p, _q;

    public TrustedServer(BigInteger p, BigInteger q)
    {
        _p = p; 
        _q = q;
        _modulo = _p * _q;
        _socket = new RouterSocket($"@{Consts.SERVER_HOST}:{Consts.PORT}");
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
        var clientMessage = MessagingHelper.ParseSignedMessage(message);
        switch (clientMessage!.Value.Type)
        {
            case MessageType.Registration:
                RegisterClient(clientMessage);
                break;
            case MessageType.Modulo:
                var responseMessage = clientMessage.Clone(withFrames: false);
                responseMessage.Value.AddFrame(FramesNames.MODULO, _modulo);
                MessagingHelper.Response(_socket, responseMessage, responseBy: Consts.SERVER_IDENTITY);
                break;
            case MessageType.Default:
                //на время верификации обработка сообщений отдается полностью методу VerifySenderGenuine
                var isUserVerified = VerifySenderGenuine(clientMessage);
                var reponseMessageToSender = clientMessage.Clone();
                if (isUserVerified)
                {
                    reponseMessageToSender.Value.UpdateMessageFrame($"You are actual '{clientMessage.Value.Sender}'");
                    MessagingHelper.Response(_socket, reponseMessageToSender, responseBy: Consts.SERVER_IDENTITY);
                    //отправка сообщения получателю
                    var messageToClient = MessagingHelper.ComposeMessage(
                        clientMessage.Value.Receiver,
                        MessagingHelper.SerializeMessage(clientMessage));
                    MessagingHelper.TrySendMessage(_socket, messageToClient);
                }
                else
                {
                    reponseMessageToSender.Value.UpdateMessageFrame($"You are dickhead, but not '{reponseMessageToSender.Value.Sender}'");
                    MessagingHelper.Response(_socket, reponseMessageToSender, responseBy: Consts.SERVER_IDENTITY);
                }
                break;
        }
    }

    private bool VerifySenderGenuine(SignedMessage message)
    {
        var isVerified = true;
        var verificationMessage = message.Clone();
        verificationMessage.Value.UpdateMessageFrame("Verification started...");
        MessagingHelper.Response(_socket, verificationMessage, responseBy: Consts.SERVER_IDENTITY);
        verificationMessage.Value.Type = MessageType.Verification;
        for (int round_i = 0; round_i < Consts.VERIFICATION_ROUNDS_AMOUNT; ++round_i)
        {
            verificationMessage.Value.ClearFrames();
            verificationMessage.Value.AddFrame(FramesNames.ROUND, round_i + 1);
            verificationMessage.Value.AddFrameAsRequestingValue(FramesNames.X); //добавляю X как запрос
            MessagingHelper.Response(_socket, verificationMessage, responseBy: Consts.SERVER_IDENTITY); //отправка запроса на получение значения <x>
            var x = MessagingHelper.ParseSignedMessage(_socket.ReceiveMultipartMessage()); //получение <x> ответвым сообщением

            verificationMessage.Value.ClearFrames(excludeFrame: FramesNames.ROUND);
            var e = RandomNumberGenerator.GetInt32(0, 2); //генерация <e>
            verificationMessage.Value.AddFrameAsRequestingValue(FramesNames.Y); //y как запрос
            verificationMessage.Value.AddFrame(FramesNames.E, e); //добавляю e как параметр
            MessagingHelper.Response(_socket, verificationMessage, responseBy: Consts.SERVER_IDENTITY); //отправка запроса на получение значения <x>

            var y = MessagingHelper.ParseSignedMessage(_socket.ReceiveMultipartMessage()); //получение <y> ответвым сообщением
            
            var yValue = BigInteger.Parse(y!.Value.Frames[FramesNames.Y].ToString()!);
            var xValue = BigInteger.Parse(x!.Value.Frames[FramesNames.X].ToString()!);

            if (yValue == 0)
            {
                isVerified = false;
                break;
            }
            else 
            {
                var leftSide = BigInteger.ModPow(yValue, 2, _modulo);
                var rightSide = xValue * BigInteger.ModPow(_registratedUsers[verificationMessage.Value.Sender], e, _modulo) % _modulo;
                if (leftSide != rightSide)
                {
                    isVerified = false;
                    break;
                }
            }
        }
        verificationMessage.Value.ClearFrames();
        verificationMessage.Value.Type = MessageType.Default;
        verificationMessage.Value.UpdateMessageFrame("Verification finished...");
        MessagingHelper.Response(_socket, verificationMessage, responseBy: Consts.SERVER_IDENTITY);
        return isVerified;
    }

    public void RegisterClient(SignedMessage message)
    {
        if (!_registratedUsers.ContainsKey(message.Value.Sender))
            _registratedUsers.Add(message.Value.Sender, BigInteger.Parse(message.Value.Frames[FramesNames.PUBLIC_KEY].ToString()!));
        message.Value.AddFrame(FramesNames.STATUS, "Registration completed");
        MessagingHelper.Response(_socket, message, responseBy: Consts.SERVER_IDENTITY);
    }
}
