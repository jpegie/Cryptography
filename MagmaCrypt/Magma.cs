using System.Diagnostics;
using System.Text;
namespace Magma;

public class MagmaCrypt
{
    Stopwatch _stopwatch;
    private List<byte> _key;
    private List<byte> _data;

    public MagmaCrypt(string key)
    {
        if (key.Length != 32)
        {
            throw new Exception("KEY MUST BE 32 CHARS LENGTH");
        }
        _stopwatch = new Stopwatch();   
        _key = Encoding.Default.GetBytes(key).ToList();
    }
    public IEnumerable<byte> Crypt(IEnumerable<byte> data, bool decrypt = false)
    {
        _stopwatch.Start();
        Console.WriteLine((decrypt ? "Decrypting" : "Encrypting") + " started");
        
        FillGapInData(data);

        var keys = GetRoundsKeys(decrypt);
        var splittedData = SplitDataToFragments();
        var cryptingTasks = new List<Task>();
        var fragments = new List<Fragment>();

        for (int i = 0; i < splittedData.Count(); i++)
        {
            fragments.Add(new Fragment(splittedData[i], keys, i));
        }

        fragments.ForEach(f => cryptingTasks.Add(new Task(f.Crypt)));
        cryptingTasks.ForEach(task => task.Start());

        Task.WaitAll(cryptingTasks.ToArray());
        fragments = fragments.OrderBy(f => f.Index).ToList();

        if (decrypt)
        {
            RemoveGapBytes(fragments);
        }
        _stopwatch.Stop();
        Console.WriteLine((decrypt ? "Decrypting" : "Encrypting" + $"finished in {_stopwatch.Elapsed.ToString()}"));
        return fragments.SelectMany(f => f.CryptedData).ToList();
        
    }
    private void RemoveGapBytes(List<Fragment> fragments)
    {
        fragments[0].CryptedData.RemoveRange(0, fragments[0].CryptedData.FindIndex(b => b != 0x00));
    }
    private void FillGapInData(IEnumerable<byte> data)
    {
        _data = data.ToList();
        if (_data.Count % 8 != 0)
        {
            var gapCount = 8 * (_data.Count / 8 + 1) - _data.Count;
            _data.InsertRange(0, Enumerable.Repeat<byte>(0x00, gapCount));
        }
    }
    
    private List<uint> GetRoundsKeys(bool decrypt)
    {
        const int setsInFinalKeys = 4,
            lastSet = 3,
            bytesInKey = 4,
            keysInSet = 8;

        var keysSet = new List<uint>();
        var finalKeys = new List<uint>();

        for (int i = 0; i < keysInSet; ++i)
        {
            keysSet.Add(BitConverter.ToUInt32(
                _key.GetRange(i * bytesInKey, bytesInKey).ToArray()));
        }
        for (int i = 0; i < setsInFinalKeys; ++i)
        {
            if (i == lastSet)
            {
                keysSet.Reverse();
            }
            finalKeys.AddRange(keysSet);
        }
        if (decrypt)
        {
            finalKeys.Reverse();
        }
        return finalKeys;
    }

    private ulong [] SplitDataToFragments()
    {
        const int bytesInFragment = 8;
        var fragmentsAmount = _data.Count / bytesInFragment;
        var fragments = new ulong[fragmentsAmount];
        for (int i = 0; i < fragmentsAmount; ++i)
        {
            var fragment = _data.GetRange(i * bytesInFragment, bytesInFragment);
            fragments[i] = BitConverter.ToUInt64(fragment.ToArray());
        }
        return fragments;
    }
}

