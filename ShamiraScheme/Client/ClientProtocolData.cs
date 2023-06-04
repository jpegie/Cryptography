using Server.Extensions;
using System.Numerics;

namespace Client;
public class ClientProtocolData
{
    BigInteger _privateKey;
    BigInteger _publicKey;
    BigInteger _modulo;
    string _name;

    public ClientProtocolData(string name)
    {
        _name = name;
    }
    public string Name => _name;
    public BigInteger Modulo
    {
        get => _modulo;
        set => _modulo = value; 
    }   
    public BigInteger PublicKey => _publicKey;
    public BigInteger PrivateKey => _privateKey;    

    public void GenerateKeys()
    {
        GeneratePrivateKey();
        GeneratePublicKey();
    }
    private void GeneratePrivateKey()
    {
        var isPrivateKeyFound = false;
        var buffPrivateKey = new Random().NextBigInteger(1, _modulo/4); //генерация начальной точки

        while (!isPrivateKeyFound && buffPrivateKey < _modulo)
        {
            if (BigInteger.GreatestCommonDivisor(_modulo, buffPrivateKey) == 1)
            {
                isPrivateKeyFound = true;
                break;
            }
            buffPrivateKey++;
        }
        //если не получилось взаимно простое число с @n, то секретный ключ будет равен 1
        if (buffPrivateKey >= _modulo)
        {
            _privateKey = 1;
        }
        else
        {
            _privateKey = buffPrivateKey;
        }
    }
    private void GeneratePublicKey()
    {
        _publicKey = BigInteger.ModPow(_privateKey, 2, _modulo);
    }
}
