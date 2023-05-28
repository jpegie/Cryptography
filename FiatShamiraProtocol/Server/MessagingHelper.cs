using NetMQ;

namespace Server;
public static class MessagingHelper
{
    public static void Response(NetMQSocket socket, ReceivedMessage messageToResponseTo, string message, params string[] list)
    {
        var responseMessage = ComposeMessage(
            messageToResponseTo.Sender,   //кому отправить
            messageToResponseTo.Receiver,                             //от кого отправление
            messageToResponseTo.Sender,   //первая строка получателю не придет, но хочу сохранить в мете 
            message);                           //само сообщение
        foreach(var param in list)
        {
            responseMessage.Append(param);
        }
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
        var serverParam = new string[] { ReceivedMessage.SERVER_NAME };
        list = serverParam.Concat(list).ToArray();
        return ComposeMessage(list);
    }
    public static bool TrySendMessage(NetMQSocket socket, NetMQMessage message)
    {
        return socket.TrySendMultipartMessage(message);
    }
}

