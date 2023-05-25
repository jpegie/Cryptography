using FiatShamiraProtocol.Модель.Дополнения;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace FiatShamiraProtocol.Модель;
public class VerificationCenter
{
    BigInteger _modulo;
    Random _rnd;
    public VerificationCenter(BigInteger modulo)
    {
        _rnd = new Random();
        _modulo = modulo;
    }

    public BigInteger GenerateR()
    {
        return _rnd.NextBigInteger(1, _modulo);
    }
    public int GenerateE()
    {
        return _rnd.Next(0, 1);
    }
}
