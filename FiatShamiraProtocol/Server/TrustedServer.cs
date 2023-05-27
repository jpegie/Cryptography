using NetMQ.Sockets;
using NetMQ;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

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
        var clientMessage = new ReceivedMessage(message);
        if (clientMessage.IsRegistrationMessage)
        {
            RegisterClient(clientMessage);
        }
        else
        {
            //на время верификации обработка сообщений отдается полностью методу VerifySenderGenuine
            var isUserVerified = VerifySenderGenuine(clientMessage);
            if (isUserVerified)
            {
                MessagingHelper.Response(_socket, clientMessage, $"You are actual '{clientMessage.SenderString}'");
                //var msg = MessagingHelper.ComposeMessage(clientMessage.ReceiverString, clientMessage.SenderString, clientMessage.ReceiverString, clientMessage.MessageString);
                //MessagingHelper.TrySendMessage(_socket, msg);
            }
            else
            {
                MessagingHelper.Response(_socket, clientMessage, $"You are dickhead, but not '{clientMessage.SenderString}'");
            }
        }
    }

    private bool VerifySenderGenuine(ReceivedMessage message)
    {
        var isVerified = true;
        MessagingHelper.Response(_socket, message, "Verification started...");
        for (int round_i = 0; round_i < 40; ++round_i)
        {
            MessagingHelper.Response(_socket, message, "Verification", "x", round_i.ToString()); //отправка запроса на получение значения <x>
            var x = new ValuedMessage(_socket.ReceiveMultipartMessage()); //получение <x> ответвым сообщением
            var e = RandomNumberGenerator.GetInt32(0, 2); //генерация <e>
            MessagingHelper.Response(_socket, message, "Verification", $"y;{e}", round_i.ToString()); //отправка запроса на получение значения <x>
            var y = new ValuedMessage(_socket.ReceiveMultipartMessage()); //получение <y> ответвым сообщением
            if (y.ValueBigInteger == 0)
            {
                isVerified = false;
                break;
            }
            else if (BigInteger.ModPow(y.ValueBigInteger, 2, _modulo) != (x.ValueBigInteger * BigInteger.ModPow(_registratedUsers[message.SenderString], e, _modulo)) % _modulo)
            {
                isVerified = false;
                break;
            }
        }
        MessagingHelper.Response(_socket, message, "Verification finished...");
        return isVerified;
    }

    public void RegisterClient(ReceivedMessage message)
    {
        if (!_registratedUsers.ContainsKey(message.SenderString))
            _registratedUsers.Add(message.SenderString, message.PublicKeyBigInteger);
        MessagingHelper.Response(_socket, message, "Registration completed!");
    }
}
