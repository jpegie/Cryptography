using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FiatShamiraProtocol.Модель;

public class User
{
    BigInteger _privateKey;
    BigInteger _publicKey;
    BigInteger _modulo;
    VerificationCenter _verificationCenter;
    string _name;
   
    public User(BigInteger modulo, string name)
    {
        _verificationCenter = new VerificationCenter(modulo); 
        _name = name;
        _modulo = modulo;
        GeneratePrivateKey();
        GeneratePublicKey();
    }
    public string Name => _name;
    public BigInteger PublicKey => _publicKey;
    public void ReceiveMessage()
    {
        throw new NotImplementedException();
    }
    public void SendMessage() 
    {
        throw new NotImplementedException();
    }
    private void GeneratePrivateKey()
    {
        var isPrivateKeyFound = false;
        var buffPrivateKey = 1;

        while(!isPrivateKeyFound && buffPrivateKey < _modulo)
        {
            //тут всегда сразу выходить будем, потому что с 1 всегда НОД = 1
            //TODO: мб сделать какой-то старт поинт, т.е. число, с которого начинается генерация вместо 1
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
