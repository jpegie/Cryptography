using NetMQ;
using Newtonsoft.Json;

namespace Server.Helpers;
public static class MessagingHelper
{
    public static void Response(NetMQSocket socket, ValuedMessage messageToResponseTo)
    {
        var responseMessage = ComposeMessage(messageToResponseTo.Sender, SerializeMessage(messageToResponseTo));
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
}

