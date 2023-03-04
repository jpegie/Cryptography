using System.Collections;

namespace Стрибог;
public class StribogHash
{
    private const int BYTES_LEN_COMMON = 64;
    private const int BYTES_LEN_IN_L = 8;

    private byte[] _n = new byte[BYTES_LEN_COMMON];
    private byte[] _sigma = new byte[BYTES_LEN_COMMON];
    private byte[] _initVec = new byte[BYTES_LEN_COMMON];
    private int _hashLen = 256;

    public StribogHash(int len = 256)
    {
        for (int i = 0; i < BYTES_LEN_COMMON; ++i)
        {
            _n[i] = 0x00;
            _sigma[i] = 0x00;
            _initVec[i] = (byte)(len == 512 ? 0x00 : 0x01);
        }
        _hashLen = len;
    }


    public IEnumerable<byte> GetHash(IEnumerable<byte> data)
    {
        var dataArr = data.ToArray();

        var h = new byte[BYTES_LEN_COMMON]; Array.Copy(_initVec, h, BYTES_LEN_COMMON);

        var n0 = Enumerable.Repeat((byte)0x00, 64).ToArray();

        if (dataArr.Length >= 64)
        {
            dataArr = SqueezeTo512(ref h, dataArr);
        }

        var dataArrBitsAmount = BitConverter.GetBytes(dataArr.Length * 8).Reverse().ToArray();
        if (dataArr.Length < 64)
        {
            dataArr = FillUpTo512(dataArr);
        }

        h = G(_n, h, dataArr);
        _n = SumModulo512(_n, dataArrBitsAmount);
        _sigma = SumModulo512(_sigma, dataArr);
        h = G(n0, h, _n);
        h = G(n0, h, _sigma);

        if (_hashLen == 512)
        {
            return h;
        }
        else
        {
            byte[] h256 = new byte[32];
            Array.Copy(h, 0, h256, 0, 32);
            return h256;
        }
    }

    private byte[] FillUpTo512(byte[] dataArr)
    {
        //в начало добить 0, потом одну 1 и сам dataArr
        var zeroesAmount = BYTES_LEN_COMMON - dataArr.Length - 1;
        var res = Enumerable
            .Repeat((byte)0x00, zeroesAmount)
            .Append((byte)0x01).ToList();
        res.AddRange(dataArr);
        return res.ToArray();
    }

    private byte[] SqueezeTo512(ref byte[] h, byte[] dataArr)
    {
        var bytes512 = BitConverter.GetBytes(512).Reverse().ToArray();
        var itersAmount = dataArr.Length / BYTES_LEN_COMMON;
        for (int i = 0; i < itersAmount; ++i)
        {
            var last512 = dataArr
                .SkipLast(i*BYTES_LEN_COMMON)
                .TakeLast(BYTES_LEN_COMMON)
                .ToArray();

            h = G(_n, h, last512);
            _n = SumModulo512(_n, bytes512);
            _sigma = SumModulo512(_sigma, last512);
        }

        dataArr = dataArr.SkipLast(itersAmount * BYTES_LEN_COMMON).ToArray();
        return dataArr;
    }

    private byte[] Sum512(byte[] a, byte[] b)
    {
        byte[] res = new byte[BYTES_LEN_COMMON];
        for(int i = 0; i < BYTES_LEN_COMMON; ++i)
        {
            res[i] = (byte)(a[i] ^ b[i]);
        }
        return res;
    }

    private byte[] E(byte[] k, byte[] m)
    {
        var res = Sum512(k, m);
        for (int i = 0; i < 12; ++i)
        {
            res = S(res);
            res = P(res);
            res = L(res);
            k = KeySchedule(k, i);
            res = Sum512(res, k);
        }
        return res;
    }
    
    private byte[] SumModulo512(byte[] a, byte[] b)
    {
        //var modulo = BigInteger.Pow(2, 512);
        //var aInt = new BigInteger(a);
        //var bInt = new BigInteger(b);
        //var sum = (aInt + bInt) % modulo;

        //var res = sum
        //    .ToByteArray()
        //    .ToList();

        //res.InsertRange(0, Enumerable.Repeat((byte)0x00, BYTES_LEN_COMMON - res.Count));
       
        //return res.ToArray();

        byte[] temp = new byte[64];
        int i = 0, t = 0;
        byte[] tempA = new byte[64];
        byte[] tempB = new byte[64];
        Array.Copy(a, 0, tempA, 64 - a.Length, a.Length);
        Array.Copy(b, 0, tempB, 64 - b.Length, b.Length);
        for (i = 63; i >= 0; i--)
        {
            t = tempA[i] + tempB[i] + (t >> 8);
            temp[i] = (byte)(t & 0xFF);
        }
        return temp;
    }

    private byte[] KeySchedule(byte[] k, int i)
    {
        k = Sum512(k, Data.C[i]);
        k = S(k);
        k = P(k);
        k = L(k);
        return k;
    }

    private byte[] G(byte[] n, byte[] h, byte[] m)
    {
        var k = Sum512(h, n);
        k = S(k);
        k = P(k);
        k = L(k);
       
        var t = E(k, m);
        t = Sum512(t, h);

        var newh = Sum512(t, m);
        return newh;
    }

    private byte[] S(byte[] data)
    {
        byte[] res = new byte[BYTES_LEN_COMMON];
        for (int i = 0; i < BYTES_LEN_COMMON; i++)
        {
            res[i] = Data.Sbox[data[i]];
        } 
        return res;
    }

    private byte[] P(byte[] data)
    {
        byte[] res = new byte[BYTES_LEN_COMMON];
        for (int i = 0; i < BYTES_LEN_COMMON; i++)
        {
            res[i] = data[Data.Tau[i]];
        }
        return res;
    }
    
    private byte[] L(byte[] data)
    {
        const int BYTES_IN_BLOCK = 8;
        const int BITS_IN_BLOCK = 64;

        var res = new List<byte>();

        var blocks = data.Chunk(BYTES_IN_BLOCK).ToList();

        foreach (var block in blocks)
        {
            var bl = block.Reverse().ToArray();
            var bits = GetBoolArrayFromBytes(bl);
            var multiplied = 0ul;

            for (int i = 0; i < BITS_IN_BLOCK; ++i)
            {
                if (bits[i])
                {
                    multiplied ^= Data.A[i];
                }
            }

            var resBlock = GetBytesArrayFromUlong(multiplied);
            res.AddRange(resBlock);
        }
        return res.ToArray();
    }

    private bool[] GetBoolArrayFromBytes(byte[] data)
    {
        var res = new bool[data.Length * 8];
        new BitArray(data).CopyTo(res, 0);
        res = res.Reverse().ToArray();
        return res;
    }

    private byte[] GetBytesArrayFromUlong(ulong num)
    {
        var res = BitConverter.GetBytes(num).Reverse().ToArray();
        return res;
    }
}
