using System.Text;
namespace Magma;
public class MagmaCrypt
{
    private static int _blockLenghtBytes = 8;
    private static byte[] _key;
    private List<byte> _data;

    public MagmaCrypt(IEnumerable<byte> data, string key)
    {
        if (key.Length != 32)
        {
            throw new Exception("KEY MUST BE 32 CHARS LENGTH");
        }

        _data = data.ToList<byte>();
        _key = Encoding.Default.GetBytes(key).ToArray();
    }
    public List<byte> Crypt(bool decrypt = false)
    {
        FillGapInData();
        var crypted = new List<byte>();
        var keys = decrypt ? GetKeys().Reverse() : GetKeys();
        var fragments = SplitDataToFragments();

        foreach (var fragment in fragments)
        {
            var step = new Step(fragment, keys);
            var cryptedFragmentBytes = step.Crypt();
            crypted.AddRange(cryptedFragmentBytes);
        }
        return crypted;
    }

    private void FillGapInData()
    {
        if (_data.Count % 8 != 0)
        {
            var amountBytesToAdd = 8 * (_data.Count / 8 + 1) - _data.Count;
            _data.InsertRange(0, Enumerable.Repeat<byte>(0x00, amountBytesToAdd));
        }
    }
    
    private IEnumerable<uint> GetKeys()
    {
        int totalParts = 4,
            lastPart = 3,
            keyBytesCapacity = 4,
            keysInPart = 8;

        var setOfKeys = new List<uint>();
        var keys = new List<uint>();

        for (int i = 0; i < keysInPart; ++i)
        {
            setOfKeys.Add(
                BitConverter.ToUInt32(
                _key
                .Skip(i * keyBytesCapacity)
                .Take(keyBytesCapacity)
                .ToArray()));
        }
        for (int i = 0; i < totalParts; ++i)
        {
            if (i == lastPart)
            {
                setOfKeys.Reverse();
            }
            keys.AddRange(setOfKeys);
        }
        return keys;
    }

    private IEnumerable<ulong> SplitDataToFragments()
    {
        int blocksAmount = _data.Count / _blockLenghtBytes;
        if (_data.Count % _blockLenghtBytes != 0)
        {
            blocksAmount++;
        }
        List<ulong> blocks = new List<ulong>();
        for (int i = 0; i < blocksAmount; ++i)
        {
            var takeAmount = 8;

            if (i + 1 == blocksAmount)
            {
                if (_data.Count % _blockLenghtBytes != 0)
                {
                    takeAmount = _data.Count % _blockLenghtBytes;
                }
            }
            takeAmount = 8;
            var blockBytes = _data.Skip(i * 8).Take(takeAmount).ToList();

            if (takeAmount != 8)
            {
                blockBytes.InsertRange(0, Enumerable.Repeat<byte>(0x00, 8 - takeAmount).ToArray());
            }

            blocks.Add(BitConverter.ToUInt64(blockBytes.ToArray()));
        }
        return blocks;
    }
}

