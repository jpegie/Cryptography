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

    public TrustedServer(int port = 12346)
    {
        _socket = new RouterSocket($"@tcp://127.0.0.1:{port}");
        _socket.Options.Identity = Encoding.UTF8.GetBytes("Server");
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
        if (clientMessage!.Type == MessageType.Registration)
        {
            RegisterClient(clientMessage);
        }
        else
        {
            //на время верификации обработка сообщений отдается полностью методу VerifySenderGenuine
            var isUserVerified = VerifySenderGenuine(clientMessage);
            var reponseMessageToSender = clientMessage.Clone();

            if (isUserVerified)
            {
                reponseMessageToSender.UpdateMessageFrame($"You are actual '{clientMessage.Sender}'");
                MessagingHelper.Response(_socket, reponseMessageToSender);
                //отправка сообщения получателю
                var messageToClient = MessagingHelper.ComposeMessage(
                    clientMessage.Receiver, 
                    JsonConvert.SerializeObject(clientMessage));
                MessagingHelper.TrySendMessage(_socket, messageToClient);
            }
            else
            {

                reponseMessageToSender.UpdateMessageFrame($"You are dickhead, but not '{reponseMessageToSender.Sender}'");
                MessagingHelper.Response(_socket, reponseMessageToSender);
            }
        }
    }

    private bool VerifySenderGenuine(ValuedMessage message)
    {
        var isVerified = true;
        var verificationMessage = new ValuedMessage(
            message.Sender, 
            message.Receiver, 
            MessageType.Default);
        verificationMessage.UpdateMessageFrame("Verification started...");
        MessagingHelper.Response(_socket, verificationMessage);
        verificationMessage.Type = MessageType.Verification;
        for (int round_i = 0; round_i < 40; ++round_i)
        {
            verificationMessage.Frames.Clear();
            verificationMessage.AddFrameAsRequestingValue("x"); //добавляю X как запрос
            MessagingHelper.Response(_socket, verificationMessage); //отправка запроса на получение значения <x>
            var x = MessagingHelper.ParseValuedMessage(_socket.ReceiveMultipartMessage()); //получение <x> ответвым сообщением

            verificationMessage.Frames.Clear();
            var e = RandomNumberGenerator.GetInt32(0, 2); //генерация <e>
            verificationMessage.AddFrameAsRequestingValue("y"); //y как запрос
            verificationMessage.AddFrame("e", e); //добавляю e как параметр
            MessagingHelper.Response(_socket, verificationMessage); //отправка запроса на получение значения <x>
            
            var y = MessagingHelper.ParseValuedMessage(_socket.ReceiveMultipartMessage()); //получение <y> ответвым сообщением
            
            var yValue = BigInteger.Parse(y!.Frames["y"].ToString()!);
            var xValue = BigInteger.Parse(x!.Frames["x"].ToString()!);

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
        verificationMessage.Type = MessageType.Default;
        verificationMessage.UpdateMessageFrame("Verification finished...");
        MessagingHelper.Response(_socket, verificationMessage);
        return isVerified;
    }

    public void RegisterClient(ValuedMessage message)
    {
        if (!_registratedUsers.ContainsKey(message.Sender))
            _registratedUsers.Add(message.Sender, BigInteger.Parse(message.Frames["PublicKey"].ToString()!));
        message.AddFrame("Status", "Registration completed");
        MessagingHelper.Response(_socket, message);
    }
}
