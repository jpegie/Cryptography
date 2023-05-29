using System.ComponentModel.DataAnnotations;

namespace Server;
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
        _frames[name] = value;   
    }
    public void ClearFrames(string excludeFrame = "")
    {
        foreach(var frame in _frames)
        {
            if (frame.Key == excludeFrame)
            {
                continue;
            }
            _frames.Remove(frame.Key);
        }
    }
    public void AddFrameAsRequestingValue(string name)
    {
        AddFrame(name, -1);
    }
    public void UpdateMessageFrame(string message) 
    {
        _frames[FramesNames.MESSAGE] = message;
    }
    public void SwapSenderWithReceiver()
    {
        var buff = _sender;
        _sender = _receiver;
        _receiver = buff;
    }
    public ValuedMessage Clone(bool withFrames = true)
    {
        var clone = new ValuedMessage(_sender, _receiver, _messageType);
        if (withFrames)
        {
            foreach (var frame in _frames)
            {
                clone.AddFrame(frame.Key, frame.Value);
            }
        }
        return clone;
    }
}


