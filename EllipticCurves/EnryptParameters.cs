namespace EllipticCurves;
public class EnryptParameters
{
    public int AliceK { get; set; }
    public EllipticPoint BobOpennedKey { get; set; }
    public EllipticPoint BobG { get; set; }
    public Dictionary<char, EllipticPoint> Alphabet { get; set; }
    public string StringToEncrypt { get; set; }
}
