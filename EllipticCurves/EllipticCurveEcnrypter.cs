namespace EllipticCurves;
public class EllipticCurveEcnrypter
{
    public string Crypt(EnryptParameters param)
    {
        var cryptedStr = "";
        foreach (var symbol in param.StringToEncrypt)
        {
            var fromAlphabet = param.Alphabet[symbol];
            var firstPoint = EllipticPointsOperations.Multiply(param.BobG, param.AliceK);
            var secondPoint = EllipticPointsOperations.Sum(fromAlphabet, EllipticPointsOperations.Multiply(param.BobOpennedKey, param.AliceK));
            cryptedStr += param.Alphabet.First(item => item.Value.Equals(firstPoint)).Key;
            cryptedStr += param.Alphabet.First(item => item.Value.Equals(secondPoint)).Key;
        }
        return cryptedStr;
    }
}