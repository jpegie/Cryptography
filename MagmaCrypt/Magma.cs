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
        var splittedData = SplitDataToBlocks(); //делим файл на блоки по 64 бита
        var cryptingTasks = new List<Task>(); 
        var blocks = new List<Block>();

        for (int i = 0; i < splittedData.Count(); i++)
        {
            blocks.Add(new Block(splittedData[i], keys, i));
        }

        blocks.ForEach(b => cryptingTasks.Add(new Task(b.Crypt)));
        cryptingTasks.ForEach(task => task.Start());

        Task.WaitAll(cryptingTasks.ToArray());
        blocks = blocks.OrderBy(b => b.Index).ToList();

        if (decrypt)
        {
            RemoveGapBytes(blocks);
        }
        _stopwatch.Stop();
        Console.WriteLine((decrypt ? "Decrypting" : "Encrypting") + $"finished in {_stopwatch.Elapsed.ToString()}");
        return blocks.SelectMany(b => b.CryptedData).ToList();
        
    }
    private void RemoveGapBytes(List<Block> blocks)
    {
        blocks[0].CryptedData.RemoveRange(0, blocks[0].CryptedData.FindIndex(b => b != 0x00));
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

    private ulong [] SplitDataToBlocks()
    {
        const int bytesInBlock = 8;
        var blocksAmount = _data.Count / bytesInBlock;
        var blocks = new ulong[blocksAmount];
        for (int i = 0; i < blocksAmount; ++i)
        {
            var block = _data.GetRange(i * bytesInBlock, bytesInBlock);
            blocks[i] = BitConverter.ToUInt64(block.ToArray());
        }
        return blocks;
    }
}

