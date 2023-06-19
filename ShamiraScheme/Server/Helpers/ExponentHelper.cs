using System.Numerics;

namespace Server.Helpers;
public static class ExponentHelper
{
    private static int[] _firstPrimeNumbers = new int[]
    {
        3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73, 79, 83, 89, 97, 101, 103, 107, 109, 113, 127, 131, 137, 139
    };
    public static int ComposeExponentFromValue(long banknoteValue)
    {
        var result = 1;
        var bitsString = Convert.ToString(banknoteValue, 2).Reverse();
        var lastPrimeNumberIndex =  0;
        foreach(var bitChar in bitsString)
        {
            if (bitChar == '1')
            {
                result *= _firstPrimeNumbers[lastPrimeNumberIndex];
            }
            lastPrimeNumberIndex++;
        }
        return result;
    }
    public static BigInteger GetExponentWithModulo(BigInteger q, BigInteger p, long banknoteValue)
    {
        var valueExponent = ComposeExponentFromValue(banknoteValue);
        var modulo = (p - 1) * (q - 1);
        var exponent = ModMath.ModInverse(valueExponent, modulo);
        return exponent;
    }
}
