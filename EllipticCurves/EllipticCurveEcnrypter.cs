namespace EllipticCurves;
public class EllipticCurveEcnrypter
{
    public string Crypt(EnryptParameters param)
    {
        var cryptedChars = new List<(EllipticPoint, EllipticPoint)>();
        var cryptedStr = "";
        foreach (var symbol in param.InputStr)
        {
            var fromAlphabet = param.Alphabet[symbol];
            var x = EllipticPointsOperations.Multiply(param.BobG, param.AliceK);
            var y = EllipticPointsOperations.Sum(fromAlphabet, EllipticPointsOperations.Multiply(param.BobOpennedKey, param.AliceK));
            cryptedChars.Add((x, y));
            cryptedStr += param.Alphabet.First(item => item.Value.Equals(x)).Key;
            cryptedStr += param.Alphabet.First(item => item.Value.Equals(y)).Key;
        }
        return cryptedStr;
    }
}