namespace Magma;

public class Step
{
    ulong _data;
    IEnumerable<uint> _keys;
    public Step(ulong dataFragment, IEnumerable<uint>keys)
    {
        _data = dataFragment;
        _keys = keys;
    }
    public IEnumerable<byte> Crypt()
    {
        var bytes = new List<byte>();
        var cryptedFragments = BitConverter.GetBytes(CryptFragment()).ToArray();
        foreach (var fragment in cryptedFragments)
        {
            bytes.Add(fragment);
        }
        return bytes;
    }

    private ulong CryptFragment()
    {
        ulong encryptedResult = 0u;

        var totalRounds = 32;

        var right = (uint)(_data >> 32);
        var left = (uint)((_data << 32) >> 32);

        var prevRight = right;

        for (int round = 0; round < totalRounds; ++round)
        {
            prevRight = right;

            right = (UInt32)((right + _keys.ElementAt(round)) % (Convert.ToUInt64(Math.Pow(2, 32))));
            right = GetNewRight(right, left);

            if (round == totalRounds - 1)
            {
                left = right;
                right = prevRight;
            }
            else
            {
                left = prevRight;
            }
        }
        encryptedResult = ((ulong)right) << 32 | (ulong)left;

        return encryptedResult;
    }

    private uint GetNewRight(uint right, uint left)
    {
        var tRight = 0u;
        var totalParts = 8;
        var bitsInPart = 4; 
        var baseShift = 28; 

        for (int i = 0; i < totalParts; ++i)
        {
            var part = (right << bitsInPart * i) >> baseShift; //0000000000000000000000000000XXXX 
            var tPart = ReplacementTable.T[i, part];
            tRight |= (UInt32)(tPart << (baseShift - i * bitsInPart));
        }

        tRight = (tRight << 11) | (tRight >> 21);
        return tRight ^ left;
    }



}