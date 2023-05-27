using NetMQ;
using System.Numerics;

namespace Server;
public class ReceivedMessage
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
        SenderFrame = message[SENDER_INDEX];
        ReceiverFrame = message[RECEIVER_INDEX];
        MessageFrame = message[MESSAGE_INDEX];
        PublicKeyFrame = message.FrameCount >= 4
            ? message[PUBLICKEY_INDEX]
            : new NetMQFrame(SERVER_PUBLICKEY.ToString()); //у сервера нет публичного ключа, он его не отправляет, поэтому пусть будет -1
        SenderString = SenderFrame.ConvertToString();
        ReceiverString = ReceiverFrame.ConvertToString();
        MessageString = MessageFrame.ConvertToString();
        PublicKeyString = PublicKeyFrame.ConvertToString();
    }
    public bool IsRegistrationMessage
    {
        get
        {
            return ReceiverString == SERVER_NAME
                && MessageString == REGISTRATION
                ? true
                : false;
        }
    }
    public NetMQFrame SenderFrame { get; private set; }
    public NetMQFrame ReceiverFrame { get; private set; }
    public NetMQFrame MessageFrame { get; private set; }
    public NetMQFrame PublicKeyFrame { get; private set; }
    public string SenderString { get; private set; }
    public string ReceiverString { get; private set; }
    public string MessageString { get; private set; }
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