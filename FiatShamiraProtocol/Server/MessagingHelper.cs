using NetMQ;
using Newtonsoft.Json;
using System.ServiceModel.Channels;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server;
public static class MessagingHelper
{
    public static void Response(NetMQSocket socket, ValuedMessage messageToResponseTo)
    {
        var responseMessage = ComposeMessage(messageToResponseTo.Sender, JsonConvert.SerializeObject(messageToResponseTo));
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
        var serverParam = new string[] { "Server" };
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

    /*
     * public const int
        SERVER_PUBLICKEY = -1,
        SENDER_INDEX = 0,
        RECEIVER_INDEX = 1,
        MESSAGE_INDEX = 2,
        PUBLICKEY_INDEX = 3;
    public const string
        SERVER_NAME = "Server",
        REGISTRATION = "Registration";
    public ReceivedMessage(NetMQMessage message)
    {
        SenderFrame = message[SENDER_INDEX];
        ReceiverFrame = message[RECEIVER_INDEX];
        MessageFrame = message[MESSAGE_INDEX];
     */
}

