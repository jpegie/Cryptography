using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinearCongruentialGenerator;


public class NIST_BlockTest_Lite
{
    private List<bool> _bitArr;
    private int _blockLen = 21;
    public NIST_BlockTest_Lite(List<bool> bitArr, int blockLength)
    {
        _bitArr = bitArr;
        _blockLen = blockLength;
    }
    public double GetAvgFreq()
    {
        var freqs = new List<double>();

        for (int i = 0; i * _blockLen < _bitArr.Count; i += _blockLen)
        {
            var block = _bitArr.Skip(_blockLen * i).Take(_blockLen);
            var freq = (double)block.Count(a => a == true) / (double)_blockLen;
            freqs.Add(freq);
        }

        return freqs.Average();
    }
}



public class NIST_BlockTest
{
    static public double BlockTest(BitArray bitArray, int blockLength)
    {
        int numBlocks = bitArray.Length / blockLength; // 'N'
        double[] proportions = new double[numBlocks];
        int k = 0; // ptr into bitArray
        for (int block = 0; block < numBlocks; ++block)
        {
            int countOnes = 0;
            for (int i = 0; i < blockLength; ++i)
            {
                if (bitArray[k++] == true)
                    ++countOnes;
            }
            proportions[block] = (countOnes * 1.0) / blockLength;
        }
        double summ = 0.0;
        for (int block = 0; block < numBlocks; ++block)
            summ = summ + (proportions[block] - 0.5) *
             (proportions[block] - 0.5);
        double chiSquared = 4 * blockLength * summ; // magic
        double a = numBlocks / 2.0;
        double x = chiSquared / 2.0;
        double pValue = GammaFunctions.GammaUpperCont(a, x);
        return pValue;
    }

}
