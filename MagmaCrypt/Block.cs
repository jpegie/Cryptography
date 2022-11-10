namespace Magma;

public class Block
{
    ulong _data;
    int _index;
    List<uint> _keys;
    List<byte> _cryptedData;
    public Block(ulong data, List<uint>keys, int index)
    {
        _data = data;
        _keys = keys;
        _index = index; 
    }
    public int Index => _index;
    public List<byte> CryptedData => _cryptedData;
    public void Crypt()
    {
        var bytes = new List<byte>();
        var cryptedFragments = BitConverter.GetBytes(CryptBlock()).ToArray();
        foreach (var fragment in cryptedFragments)
        {
            bytes.Add(fragment);
        }
        _cryptedData = bytes;
    }

    private ulong CryptBlock()
    {
        const int totalIterations = 32;
        ulong encryptedResult = 0u;

        var right = (uint)(_data >> 32);
        var left = (uint)((_data << 32) >> 32);

        for (int round = 0; round < totalIterations; ++round)
        {
            DoIteration(round, round == totalIterations - 1, ref left, ref right);
        }
        encryptedResult = ((ulong)right) << 32 | (ulong)left;

        return encryptedResult;
    }
    private void DoIteration(int round_i, bool isLastRound, ref uint left, ref uint right)
    {
        var prevRight = right;
        right = (UInt32)((right + _keys.ElementAt(round_i)));// % (Convert.ToUInt64(Math.Pow(2, 32))));
        right = GetNewRight(right, left);

        if (isLastRound)
        {
            left = right;
            right = prevRight;
        }
        else
        {
            left = prevRight;
        }
    }

    private uint GetNewRight(uint right, uint left)
    {
        var newRight = 0u;
        const int totalParts = 8,
                  bitsInPart = 4,
                  baseShift = 28; 

        for (int i = 0; i < totalParts; ++i)
        {
            var part = (right << bitsInPart * i) >> baseShift; //0000000000000000000000000000XXXX 
            var replacesPart = ReplacementTable.T[i, part];
            newRight |= (UInt32)(replacesPart << (baseShift - i * bitsInPart));
        }

        return ((newRight << 11) | (newRight >> 21)) ^ left;
    }
}