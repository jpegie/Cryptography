using NetMQ;
using Newtonsoft.Json;
using System.Text;
using System.Security.Cryptography;

namespace Server.Helpers;
public static class MessagingHelper
{
    public static void Response(NetMQSocket socket, SignedMessage messageToResponseTo, string responseBy = "")
    {
        var sendingMessage = messageToResponseTo.Clone();
        var receiver = sendingMessage.Value.Sender;
        var sender = responseBy == "" ? sendingMessage.Value.Sender : responseBy;
        sendingMessage.Value.Sender = sender;
        var responseMessage = ComposeMessage(receiver, SerializeMessage(sendingMessage));
        TrySendMessage(socket, responseMessage);
    }

    public static NetMQMessage ComposeMessage(params string[] list)
    {
        var message = new NetMQMessage();
        foreach (var param in list)
        {
            message.Append(param);
        }
        return message;
    }
    public static NetMQMessage ComposeMessageToServer(params string[] list)
    {
        var serverParam = new string[] { Consts.SERVER_IDENTITY };
        list = serverParam.Concat(list).ToArray();
        return ComposeMessage(list);
    }
    public static bool TrySendMessage(NetMQSocket socket, NetMQMessage message)
    {
        return socket.TrySendMultipartMessage(message);
    }
  /*  public static ValuedMessage? ParseValuedMessage(NetMQMessage message)
    {
        var messageFrameIndex = message.FrameCount == 1 ? 0 : 2;
        var deserializedMessage = message[messageFrameIndex].ConvertToString();
        return JsonConvert.DeserializeObject<ValuedMessage>(deserializedMessage);
    }*/
    public static SignedMessage? ParseSignedMessage(NetMQMessage message)
    {
        var messageFrameIndex = message.FrameCount == 1 ? 0 : 2;
        var deserializedMessage = message[messageFrameIndex].ConvertToString();
        return JsonConvert.DeserializeObject<SignedMessage>(deserializedMessage);
    }
    public static string SerializeMessage(object message)
    {
        return JsonConvert.SerializeObject(message);
    }
    public static SignedMessage SignMessage(ValuedMessage message)
    {
        var hash = "";
        using (var sha256 = SHA256.Create())
        {
            var serializedMessage = SerializeMessage(message);
            var inputBytes = Encoding.UTF8.GetBytes(serializedMessage);
            var hashBytes = sha256.ComputeHash(inputBytes);
            hash = Convert.ToHexString(hashBytes);
        }
        var signed = new SignedMessage
        {
            Value = message,
            Hash = hash
        };
        return signed;
    }
}

