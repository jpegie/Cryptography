namespace Server;
public class SignedMessage
{
    public ValuedMessage Value { get; set; }
    public string Hash { get; set; }

    public SignedMessage Clone(bool withFrames = true)
    {
        var clonedValued = Value.Clone(withFrames);
        return new SignedMessage
        {
            Value = clonedValued,
            Hash = Hash.Clone().ToString()!
        };
    }
}
