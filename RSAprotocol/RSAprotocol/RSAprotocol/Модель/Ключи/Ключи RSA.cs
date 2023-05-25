using System.Numerics;
public class RSAkeys
{
    PrivateKey _privateKey;
    PublicKey _publicKey;
    BigInteger _p, _q, _n, _phi, _e, _d;
    public RSAkeys(BigInteger p, BigInteger q)
    {
        _p = p;
        _q = q;
        _n = _p * _q; //вычисление модуля
        _phi = (_p - 1) * (_q - 1); //вычисляется значение функции Эйлера от числа n

        GenerateE();
        GenerateD();

        _publicKey = new PublicKey
        {
            E = _e,
            N = _n
        };
        _privateKey = new PrivateKey
        {
            D = _d,
            N = _n
        };
    }

    public PublicKey PublicKey => _publicKey;
    public PrivateKey PrivateKey => _privateKey;
    //генерация открытой экспоненты
    private void GenerateE()
    {
        //написано в википедии, что обвчно берут числа 17, 257 или 65537 (числа Ферма)
        _e = new BigInteger(17);

        //проверка на взаимную простоту
        while (BigInteger.GreatestCommonDivisor(_e, _phi) != 1)
        {
            _e += 1;
        }
    }
    //генерация секретной экспоненты
    private void GenerateD()
    {
        _d = ModInverse(_e, _phi);
    }

    //поиск обратного по модулю
    static BigInteger ModInverse(BigInteger a, BigInteger n)
    {
        BigInteger t = 0, newt = 1;
        BigInteger r = n, newr = a;
        while (newr != 0)
        {
            BigInteger quotient = r / newr;
            BigInteger tmp = t;
            t = newt;
            newt = tmp - quotient * newt;
            tmp = r;
            r = newr;
            newr = tmp - quotient * newr;
        }
        if (r > 1)
        {
            throw new Exception("Обратный элемент не существует");
        }
        if (t < 0)
        {
            t = t + n;
        }
        return t;
    }
}
