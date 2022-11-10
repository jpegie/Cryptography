namespace EllipticCurves;
public class EllipticCurveDecrypter
{
    public string Decrypt(DecryptParameters param)
    {
        var decryptedStr = "";
        for (int i = 0; i < param.EncryptedString.Length; i += 2)
        {
            var encryptedFirstPoint = param.Alphabet[param.EncryptedString[i]];
            var ecnryptedSecondPoint = param.Alphabet[param.EncryptedString[i + 1]];
            var result = EllipticPointsOperations.Subtract(ecnryptedSecondPoint, EllipticPointsOperations.Multiply(encryptedFirstPoint, param.BobHiddenKey));
            var decryptedChar = param.Alphabet.FirstOrDefault(item => item.Value.Equals(result), param.Alphabet.First(item => item.Key == '@')).Key; ;
            decryptedStr += decryptedChar;
        }
        return decryptedStr;
    }
}
