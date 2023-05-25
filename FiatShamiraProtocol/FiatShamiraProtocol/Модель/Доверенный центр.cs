using FiatShamiraProtocol.Модель.Дополнения;
using Open.Numeric.Primes;
using System.Numerics;

namespace FiatShamiraProtocol.Модель;

public class TrustCenter
{
    const int MIN_PARAM_VAL = 10_000_000;
    const int MAX_PARAM_VAL = int.MaxValue;
    BigInteger _modulo;
    long _p, _q;

    public TrustCenter()
    {
        GenerateParams();
    }

    public BigInteger Modulo => _modulo;
    private void GenerateParams()
    {
        var genStartPoint = 0; 
        genStartPoint = RandomExtensions.ThisThreadsRandom.Next(MIN_PARAM_VAL, MAX_PARAM_VAL);
        _p = Prime.Numbers.StartingAt(genStartPoint).Take(1).ElementAt(1);
        genStartPoint = RandomExtensions.ThisThreadsRandom.Next(MIN_PARAM_VAL, MAX_PARAM_VAL);
        _q = Prime.Numbers.StartingAt(genStartPoint).Take(1).ElementAt(1);
        _modulo = _p * _q;
    }
}

