using EllipticCurves;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Стрибог;

namespace DigitalSignature;
public class DigitalSignature
{
    int _hashLen = 256;
    BigInteger _d, _q;

    public DigitalSignature(BigInteger d, BigInteger q, int hashLen = 256)
    {
        _hashLen = hashLen;
        _d = d;
        _q = q;
    }

    public byte[] GetSignature(byte[] dataToSign, EllipticPoint point)
    {
        var stribog = new StribogHash();
        var hash = stribog.GetHash(dataToSign);
        var alpha = new BigInteger(hash.ToArray());
        var e =
            alpha % _q == 0
            ? alpha % _q
            : 1;
        var r = new BigInteger(0);
        var k = 0;
        var s = new BigInteger(0);
        while (s == 0)
        {
            while (r == 0)
            {
                k = new Random().Next(0, 10000000);
                var c = EllipticPointsOperations.Multiply(point, k);
                r = c.X;
            }
            s = (r * _d + k * e) % _q;
        }


        byte[] signature = new byte[_hashLen / 4];

        Array.Copy(r.ToByteArray(), 0, signature, 0, 256 / 8);
        Array.Copy(s.ToByteArray(), 0, signature, 256 / 8, 256 / 8);

        return signature;

    }
}

