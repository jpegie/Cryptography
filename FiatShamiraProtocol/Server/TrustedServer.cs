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
    BigInteger _modulo;

    public TrustedServer()
    {
        _socket = new RouterSocket($"@{Consts.SERVER_HOST}:{Consts.PORT}");
        _socket.Options.Identity = Encoding.UTF8.GetBytes(Consts.SERVER_IDENTITY);
        _socket.Options.RouterHandover = true;
        _registratedUsers = new Dictionary<string, BigInteger>();
        _modulo = 29 * 17;
    }
    public void StartReceiving()
    {
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
                var reponseMessageToSender = clientMessage.Clone();
                if (isUserVerified)
                {
                    reponseMessageToSender.UpdateMessageFrame($"You are actual '{clientMessage.Sender}'");
                    MessagingHelper.Response(_socket, reponseMessageToSender, responseBy: Consts.SERVER_IDENTITY);
                    //отправка сообщения получателю
                    var messageToClient = MessagingHelper.ComposeMessage(
                        clientMessage.Receiver,
                        MessagingHelper.SerializeMessage(clientMessage));
                    MessagingHelper.TrySendMessage(_socket, messageToClient);
                }
                else
                {
                    reponseMessageToSender.UpdateMessageFrame($"You are dickhead, but not '{reponseMessageToSender.Sender}'");
                    MessagingHelper.Response(_socket, reponseMessageToSender, responseBy: Consts.SERVER_IDENTITY);
                }
                break;
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
