using BrendanGrant.Helpers.FileAssociation;
using Server.Extensions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Net.Sockets;
using System.Numerics;
using System.Reflection;
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

    public static BigInteger SignBanknote(string banknoteToSignString, BigInteger exponent, BigInteger modulo)
    {
        var banknoteToSign = BigInteger.Parse(banknoteToSignString);
        var signedBanknote = BankHelper.SignBanknote(banknoteToSign, exponent, modulo);
        return signedBanknote;
    }

    public static bool VerifyBanknote(ValuedMessage clientMessage, BigInteger p, BigInteger q, BigInteger modulo)
    {
        var sign = BigInteger.Parse(clientMessage.Frames[FramesNames.SIGNED_BANKNOTE].ToString()!);
        var banknoteValue = long.Parse(clientMessage.Frames[FramesNames.BANKNOTE_VALUE].ToString()!);
        var unsignedBanknote = clientMessage.Frames[FramesNames.BANKNOTE_TO_VERIFY].ToString()!;
        var exponent = ExponentHelper.GetExponentWithModulo(q, p, banknoteValue);
        var repeatedSign = BankHelper.SignBanknote(unsignedBanknote, exponent, modulo);
        return sign == repeatedSign;
    }
}
