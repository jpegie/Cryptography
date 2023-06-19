using NetMQ.Sockets;
using NetMQ;
using System.Numerics;
using System.Text;
using Server.Helpers;

namespace Server;
internal class TrustedServer
{
    RouterSocket _socket;
    Dictionary<string, BigInteger> _registratedUsers;

    public TrustedServer (string host)
    {
        _registratedUsers = new Dictionary<string, BigInteger>();
        _socket = new RouterSocket($"@{host}:{Consts.PORT}");
        _socket.Options.Identity = Encoding.UTF8.GetBytes(Consts.SERVER_IDENTITY);
        _socket.Options.RouterHandover = true;

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
        NetMQMessage? responseMessageMQ = null;
        switch (clientMessage!.Type)
        {
            case MessageType.Registration:
                RegisterClient(clientMessage);
                break;
            default: //просто отправлю сообщение получателю
                responseMessageMQ = MessagingHelper.ComposeMessage(clientMessage.Receiver, MessagingHelper.SerializeMessage(clientMessage));
                MessagingHelper.TrySendMessage(_socket, responseMessageMQ);
                break;
        }
    }
    public void RegisterClient(ValuedMessage message)
    {
        message.AddFrame(FramesNames.STATUS, "Registration completed");
        MessagingHelper.Response(_socket, message, responseBy: Consts.SERVER_IDENTITY);
    }
}
