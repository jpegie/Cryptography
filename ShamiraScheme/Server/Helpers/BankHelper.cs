using BrendanGrant.Helpers.FileAssociation;
using Server.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Numerics;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;


namespace Server.Helpers;
public static class BankHelper
{
    public static Banknote ComposeBanknote(BigInteger n, int exponent, long value)
    {
        var rand = new Random();
        var s = rand.NextBigInteger(1, n - 1);
        var r = rand.NextBigInteger(1, n - 1);
        var rFound = false;
        while (!rFound)
        {
            if (BigInteger.GreatestCommonDivisor(r, n) == 1)
            {
                rFound = true;
            }
            else
            {
                r = rand.NextBigInteger(1, n - 1);
            }
        }
        
        var mult = (s * BigInteger.ModPow(r, new BigInteger(exponent), n)) % n;
        var banknote = new Banknote
        {
            S = s.ToString(),
            R = r.ToString(),
            Value = value,
            SMultR = mult.ToString()
        };
        return banknote;
    }

    public static BigInteger SignBanknote(BigInteger banknote, BigInteger secretExponent, BigInteger modulo)
    {
        return BigInteger.ModPow(banknote, secretExponent, modulo) % modulo;
    }

}
