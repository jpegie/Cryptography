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
using System.ComponentModel.Design;

namespace Server;
internal class TrustedServer
{
    RouterSocket _socket;
    BigInteger _modulo, _p, _q;
    Dictionary<string, BigInteger> _registratedUsers;

    public TrustedServer(BigInteger p, BigInteger q, string host)
    {
        _p = p; 
        _q = q;
        _modulo = _p * _q;
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
        ValuedMessage? responseMessage = null;
        NetMQMessage? responseMessageMQ = null;
        switch (clientMessage!.Type)
        {
            case MessageType.Registration:
                RegisterClient(clientMessage);
                break;
            case MessageType.Modulo:
                responseMessage = clientMessage.Clone(withFrames: false);
                responseMessage.AddFrame(FramesNames.MODULO, _modulo);
                MessagingHelper.Response(_socket, responseMessage, responseBy: Consts.SERVER_IDENTITY);
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
