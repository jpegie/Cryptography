using System.Collections;
using System.Drawing.Drawing2D;
using System.Net;
using System.Numerics;
using ScottPlot;

namespace LinearCongruentialGenerator;
public class LCG_RSA : ILCG
{
    private const int BITS_IN_GEN_NUM = 8;
    private const int NUMBERS_TO_GEN = 10_000;
    private Dictionary<int, int> _setDict;
    private BigInteger _p, _q, _n;
    private BigInteger _b;
    private BigInteger _x0;
    private List<bool> _totalBits;
    private List<int> _totalSet;
    public LCG_RSA()
    {
        _p = new BigInteger(129172331);
        _q = new BigInteger(360488599);
        _n = _p * _q;
        _x0 = new RandomBigInteger(4).NextBigInteger(BITS_IN_GEN_NUM);
        _b = new RandomBigInteger(5).NextBigInteger(64);
        _totalBits = new List<bool>();
        _totalSet = new List<int>();
    }

    public Dictionary<int, int> SetDictionary => _setDict;
    public List<int> TotalSet => _totalSet;
    public List<bool> TotalBits => _totalBits;
    public void GenerateSet(object []? args)
    {
        _setDict = new Dictionary<int, int>();
        var xPrev = _x0;
        var lowerBound = int.MinValue;
        var upperBound = int.MaxValue;

        if (args != null && args.Length == 3)
        {
            lowerBound = (int)args![1];
            upperBound = (int)args![2];

        }

        for (int i = 0; i < NUMBERS_TO_GEN; ++i)
        {
            var bitsInNewNum = new List<bool>();

            for (int j = 0; j < BITS_IN_GEN_NUM; ++j)
            {
                var xNew = BigInteger.ModPow(xPrev, _b, _n);
                var leastBit = (xNew & 1) == 1 ? true : false;
                bitsInNewNum.Add(leastBit);
                xPrev = xNew;
            }

            var numberByte = ConvertBoolArrayToByte(bitsInNewNum.ToArray());
            var number = (int)numberByte;

            if (number < lowerBound || number > upperBound)
            {
                continue;
            }

            if (_setDict.ContainsKey(number))
            {
                _setDict[number]++;
            }
            else
            {
                _setDict.Add(number, 1);
            }
            _totalSet.Add(number);
            _totalBits.AddRange(bitsInNewNum);
        }
    }

    public void Print()
    { 
        var count = 0;
        var avgFaced = 0;
        var minFaced = int.MaxValue;
        var maxFaced = 0;

        foreach (var pair in _setDict)
        {
            Console.WriteLine($"{pair.Key.ToString()}\t{pair.Value.ToString()}");
            avgFaced += pair.Value;

            if (pair.Value > maxFaced)
            {
                maxFaced = pair.Value;
            }

            if (pair.Value < minFaced)
            {
                minFaced = pair.Value;
            }

            count++;
        }

        avgFaced /= count;

        Console.WriteLine($"Кол-во уникальных чисел: {count}");
        Console.WriteLine($"AVG faced: {avgFaced}, MIN faced: {minFaced}, MAX faced: {maxFaced}");
    }

    private static byte ConvertBoolArrayToByte(bool[] source)
    {
        byte result = 0;
        // This assumes the array never contains more than 8 elements!
        int index = 8 - source.Length;

        // Loop through the array
        foreach (bool b in source)
        {
            // if the element is 'true' set the bit at that position
            if (b)
                result |= (byte)(1 << (7 - index));

            index++;
        }
        return result;
    }

    private bool GetLeastBit(BigInteger number)
    {
        var bytes = number.ToByteArray();
        var numBits = new BitArray(bytes);
        return numBits[0]; //0 потому что BitArray реверснут
    }
}
