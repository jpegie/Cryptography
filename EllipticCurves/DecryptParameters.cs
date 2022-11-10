namespace EllipticCurves;
public class DecryptParameters
{
    public int AliceK { get; set; }
    public int BobHiddenKey { get; set; }
    public string EncryptedString { get; set; }
    public Dictionary<char, EllipticPoint> Alphabet { get; set; }

}
