using NetMQ;

namespace Server;
public static class MessagingHelper
{
    public static void Response(NetMQSocket socket, ReceivedMessage messageToResponseTo, string message, params string[] list)
    {
        //var sender = responseBy == "" ? "Server" : messageToResponseTo.ReceiverString;
        var sender = "Server";
        var responseMessage = ComposeMessage(
            messageToResponseTo.SenderString,   //кому отправить
            sender,                             //от кого отправление
            messageToResponseTo.SenderString,   //первая строка получателю не придет, но хочу сохранить в мете 
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
    public static bool TrySendMessage(NetMQSocket socket, NetMQMessage message)
    {
        return socket.TrySendMultipartMessage(message);
    }
}

