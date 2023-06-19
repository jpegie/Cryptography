using NetMQ;
using Newtonsoft.Json;
using System.Net.Sockets;

namespace Server.Helpers;
public static class MessagingHelper
{
    public static void Response(NetMQSocket socket, ValuedMessage messageToResponseTo, string responseBy = "")
    {
        var sendingMessage = messageToResponseTo.Clone();
        var receiver = sendingMessage.Sender;
        var sender = responseBy == "" ? sendingMessage.Sender : responseBy;
        sendingMessage.Sender = sender;
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
    public static ValuedMessage? ParseValuedMessage(NetMQMessage message)
    {
        var messageFrameIndex = message.FrameCount == 1 ? 0 : 2;
        var deserializedMessage = message[messageFrameIndex].ConvertToString();
        return JsonConvert.DeserializeObject<ValuedMessage>(deserializedMessage);
    }
    public static string SerializeMessage(ValuedMessage message)
    {
        return JsonConvert.SerializeObject(message);
    }
    public static void SerializeThenSendMessageToServer(NetMQSocket socket, ValuedMessage message)
    {
        var serialized = MessagingHelper.SerializeMessage(message);
        var msgToServer = MessagingHelper.ComposeMessageToServer(serialized);
        MessagingHelper.TrySendMessage(socket, msgToServer);
    }
}

