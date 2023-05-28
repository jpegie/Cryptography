using NetMQ.Sockets;
using NetMQ;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Newtonsoft.Json;

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
            if (isUserVerified)
            {
                clientMessage.AddMessageFrame($"You are actual '{clientMessage.Sender}'");
                MessagingHelper.Response(_socket, clientMessage);

                var responseToClientMessage = new ValuedMessage(clientMessage.Sender, clientMessage.Receiver, MessageType.Default);
                var messageToClient = MessagingHelper.ComposeMessage(JsonConvert.SerializeObject(responseToClientMessage));
                MessagingHelper.TrySendMessage(_socket, messageToClient);
            }
            else
            {

                clientMessage.AddMessageFrame($"You are dickhead, but not '{clientMessage.Sender}'");
                MessagingHelper.Response(_socket, clientMessage);
            }
        }
    }

    private bool VerifySenderGenuine(ValuedMessage message)
    {
        var isVerified = true;
        message.Frames.Clear();
        message.AddMessageFrame("Verification started...");
        MessagingHelper.Response(_socket, message);
        message.Type = MessageType.Verification;
        for (int round_i = 0; round_i < 40; ++round_i)
        {
            message.Frames.Clear();
            message.AddFrameAsRequestingValue("x"); //добавляю X как запрос
            MessagingHelper.Response(_socket, message); //отправка запроса на получение значения <x>
            var x = MessagingHelper.ParseValuedMessage(_socket.ReceiveMultipartMessage()); //получение <x> ответвым сообщением
            
            message.Frames.Clear();
            var e = RandomNumberGenerator.GetInt32(0, 2); //генерация <e>
            message.AddFrameAsRequestingValue("y"); //y как запрос
            message.AddFrame("e", e); //добавляю e как параметр
            MessagingHelper.Response(_socket, message); //отправка запроса на получение значения <x>
            
            var y = MessagingHelper.ParseValuedMessage(_socket.ReceiveMultipartMessage()); //получение <y> ответвым сообщением
            
            var yValue = (BigInteger)y!.Frames["y"];
            var xValue = (BigInteger)x!.Frames["x"];

            if (yValue == 0)
            {
                isVerified = false;
                break;
            }
            else 
            {
                var leftSide = BigInteger.ModPow(yValue, 2, _modulo);
                var rightSide = xValue * BigInteger.ModPow(_registratedUsers[message.Sender], e, _modulo) % _modulo;
                if (leftSide != rightSide)
                {
                    isVerified = false;
                    break;
                }
            }
        }
        message.AddMessageFrame("Verification finished...");
        MessagingHelper.Response(_socket, message);
        return isVerified;
    }

    public void RegisterClient(ValuedMessage message)
    {
        if (!_registratedUsers.ContainsKey(message.Sender))
            _registratedUsers.Add(message.Sender, BigInteger.Parse(message.Frames["PublicKey"].ToString()!));
        //message.SwapSenderWithReceiver();
        message.AddFrame("Status", "Registration completed");
        MessagingHelper.Response(_socket, message);
    }
}
