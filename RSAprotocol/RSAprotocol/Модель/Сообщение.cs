using System.Numerics;

public class Message
{
    public BigInteger ID { get; set; }
    public string Sender { get; set; }
    public Byte[] Data { get; set; }
    public BigInteger[] SignedData { get; set; }
}

public class EncryptedSignedMessage
{
    public BigInteger[] Data { get; set; }
}
