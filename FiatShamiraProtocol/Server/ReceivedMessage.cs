using NetMQ;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Server;
/*public class ReceivedMessage
{
    public const int
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
        PublicKeyFrame = message.FrameCount >= 4
            ? message[PUBLICKEY_INDEX]
            : new NetMQFrame(SERVER_PUBLICKEY.ToString()); //у сервера нет публичного ключа, он его не отправляет, поэтому пусть будет -1
        Sender = SenderFrame.ConvertToString();
        Receiver = ReceiverFrame.ConvertToString();
        Message = MessageFrame.ConvertToString();
        PublicKeyString = PublicKeyFrame.ConvertToString();
    }
    public bool IsRegistrationMessage
    {
        get
        {
            return Receiver == SERVER_NAME
                && Message == REGISTRATION
                ? true
                : false;
        }
    }
    public NetMQFrame SenderFrame { get; private set; }
    public NetMQFrame ReceiverFrame { get; private set; }
    public NetMQFrame MessageFrame { get; private set; }
    public NetMQFrame PublicKeyFrame { get; private set; }
    public string Sender { get; private set; }
    public string Receiver { get; private set; }
    public string Message { get; private set; }
    public string PublicKeyString { get; private set; }
    public BigInteger PublicKeyBigInteger
    {
        get
        {
            return BigInteger.Parse(PublicKeyString);
        }
    }
}

public class VerificationMessage : ReceivedMessage
{
    public const int
        REQUESTING_VALUE_INDEX = 3,
        VERIFICATION_ROUND_INDEX = 4,
        REQUIRED_FRAMES_AMOUNT = 5;
    public const string
        VERIFICATION = "Verification";
    public VerificationMessage(NetMQMessage message) : base(message)
    {
        if (!IsVerificationMessage(message))
        {
            throw new Exception("Невозможно создать объект типа VerificationMessage - параметр не явлется VerificationMessage");
        }
        RequestingValueFrame = message[REQUESTING_VALUE_INDEX];
        VerificationRoundFrame = message[VERIFICATION_ROUND_INDEX];
        RequestedValue = RequestingValueFrame.ConvertToString();
        VerificationRoundInt = int.Parse(VerificationRoundFrame.ConvertToString());
    }
    public NetMQFrame RequestingValueFrame { get; private set; }
    public NetMQFrame VerificationRoundFrame { get; private set; }
    public string RequestedValue { get; private set; }
    public int VerificationRoundInt { get; private set; }

    public static bool IsVerificationMessage(NetMQMessage message)
    {
        return
            message.FrameCount == REQUIRED_FRAMES_AMOUNT
            && message[SENDER_INDEX].ConvertToString() == SERVER_NAME
            && message[MESSAGE_INDEX].ConvertToString() == VERIFICATION
            ? true
            : false;
    }
}

public class ValuedMessage : ReceivedMessage
{
    public const int
        VALUE_NAME_INDEX = 3,
        VALUE_INDEX = 4,
        REQUIRED_FRAMES_AMOUNT = 5;
    public ValuedMessage(NetMQMessage message) : base(message)
    {
        if (!IsValuedMessage(message))
        {
            throw new Exception("Невозможно создать объект типа ValuedMessage - параметр не явлется ValuedMessage");
        }

        ValueNameFrame = message[VALUE_NAME_INDEX];
        ValueFrame = message[VALUE_INDEX];
        ValueNameString = ValueNameFrame.ConvertToString();
        ValueString = ValueFrame.ConvertToString();
    }
    public NetMQFrame ValueNameFrame { get; private set; }
    public NetMQFrame ValueFrame { get; private set; }
    public string ValueNameString { get; private set; }
    public string ValueString { get; private set; }
    public BigInteger ValueBigInteger
    {
        get
        {
            return BigInteger.Parse(ValueString);
        }
    }
    public static bool IsValuedMessage(NetMQMessage message)
    {
        return
            message.FrameCount == REQUIRED_FRAMES_AMOUNT
            && message[RECEIVER_INDEX].ConvertToString() == SERVER_NAME
            && message[MESSAGE_INDEX].ConvertToString() == "Value"
            ? true
            : false;
    }
}

public class Message
{
    public MessageType Type { get; set; } = 0;
    public string Text { get; set; } = ""; //сериализованное сообщение
}
*/

/*public class ReceivedMessage
{
    public const int
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
        PublicKeyFrame = message.FrameCount >= 4
            ? message[PUBLICKEY_INDEX]
            : new NetMQFrame(SERVER_PUBLICKEY.ToString()); //у сервера нет публичного ключа, он его не отправляет, поэтому пусть будет -1
        Sender = SenderFrame.ConvertToString();
        Receiver = ReceiverFrame.ConvertToString();
        Message = MessageFrame.ConvertToString();
        PublicKeyString = PublicKeyFrame.ConvertToString();
    }
    public bool IsRegistrationMessage
    {
        get
        {
            return Receiver == SERVER_NAME
                && Message == REGISTRATION
                ? true
                : false;
        }
    }
    public NetMQFrame SenderFrame { get; private set; }
    public NetMQFrame ReceiverFrame { get; private set; }
    public NetMQFrame MessageFrame { get; private set; }
    public NetMQFrame PublicKeyFrame { get; private set; }
    public string Sender { get; private set; }
    public string Receiver { get; private set; }
    public string Message { get; private set; }
    public string PublicKeyString { get; private set; }
    public BigInteger PublicKeyBigInteger
    {
        get
        {
            return BigInteger.Parse(PublicKeyString);
        }
    }
}*/


public interface IMessage
{
    public MessageType Type { get; set; }
    public string Sender { get; set;  }
    public string Receiver { get; set;  }
    public Dictionary<string, object> Frames { get; }
}

[Serializable]
public class ValuedMessage : IMessage
{
    string _sender, _receiver;
    Dictionary<string, object> _frames;
    MessageType _messageType;
    public ValuedMessage(string sender, string receiver, MessageType type)
    {
        _sender = sender;
        _receiver = receiver;
        _messageType = type;
        _frames = new Dictionary<string, object>();    
    }
    virtual public MessageType Type
    {
        get => _messageType;
        set => _messageType = value;    
    }
    public string Sender
    {
        get => _sender;
        set => _sender = value;
    }
    public string Receiver
    {
        get => _receiver;
        set => _receiver = value;  
    }
    public Dictionary<string, object> Frames => _frames;
    public void AddFrame(string name, object value)
    {
        _frames.Add(name, value);   
    }
    public void AddFrameAsRequestingValue(string name)
    {
        AddFrame(name, -1);
    }
    public void AddMessageFrame(string message) 
    {
        if (_frames.ContainsKey("Message"))
        {
            _frames["Message"] = message;
        }
        else
        {
            _frames.Add("Message", message);
        }
    }
    public void SwapSenderWithReceiver()
    {
        var buff = _sender;
        _sender = _receiver;
        _receiver = _sender;
    }
}

/*public class MessageFrame
{
    public string Name { get; set; } = "";
    public object? ValueProperty { get; set; } = null;
    public object? Parameter { get; set; } = null;  
}*/


