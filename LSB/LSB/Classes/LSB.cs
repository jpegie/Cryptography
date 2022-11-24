using LSB.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace LSB.Classes;
public class LSB
{
    IDataProvider ?_originalProvider;
    IDataProvider ?_hiddenProvider;
    IDataProvider? _encryptedDataProvider;

    LSBKey _key;
    
    /// <summary>
    /// </summary>
    /// <param name="originalDataProvider">провайдер данных, в которых будем прятать данные</param>
    /// <param name="dataToHideProvider">то, что будем прятать</param>
    public LSB(IDataProvider originalDataProvider, IDataProvider dataToHideProvider, LSBKey key)
    {
        _originalProvider = originalDataProvider;
        _hiddenProvider = dataToHideProvider;
        _key = key;
    }
    /// <summary>
    /// Конструктор для дешифрования
    /// </summary>
    /// <param name="dataProvider"></param>
    /// <param name="key"></param>
    public LSB(IDataProvider dataProvider, LSBKey key)
    {
        _encryptedDataProvider = dataProvider;
        _key = key;
    }

    public IEnumerable<byte> HideData()
    {
        var originalData = _originalProvider!.Data.ToArray();
        IEnumerable<byte> splittedHiddingData = new List<byte>();

        _hiddenProvider!.Data.ToList()
            .ForEach(b =>
            {
                splittedHiddingData = splittedHiddingData.Concat(SplitHiddingByte(b));
            }
            );

        for(int byte_i = 0; byte_i < _key.ModificatedBytesPositions.Count(); ++byte_i)
        {
            var originalBits = new BitArray(new byte[] { originalData[byte_i] });
            Reverse(originalBits);
            originalBits = originalBits
                .RightShift(Consts.ModificatedBitsInByte)
                .LeftShift(Consts.ModificatedBitsInByte);
            var modificatedBits = new BitArray(new byte[] { splittedHiddingData.ElementAt(byte_i) });
            Reverse(modificatedBits);

            originalData[byte_i] = ConvertBitsToByte(originalBits.Or(modificatedBits));
        }
        return originalData;
    }
    private byte ConvertBitsToByte(BitArray bits)
    {
        if (bits.Count != 8)
        {
            throw new ArgumentException("bits");
        }
        byte[] bytes = new byte[1];
        bits.CopyTo(bytes, 0);
        return bytes[0];
    }

    private IEnumerable<byte> SplitHiddingByte(byte b)
    {
        var splittedByte = new List<byte>();
        var bits = new BitArray(new byte[] { b }); 
        Reverse(bits);

        //01110100
        for (int i = 0; i < 8 / Consts.ModificatedBitsInByte; ++i)
        {
            var bPart = bits
                .LeftShift(8 + i * Consts.ModificatedBitsInByte)
                .RightShift(16 - Consts.ModificatedBitsInByte);
            splittedByte.Add(ConvertBitsToByte(bPart));
        }
        return splittedByte;
    }

    public void Reverse(BitArray array)
    {
        int length = array.Length;
        int mid = (length / 2);

        for (int i = 0; i < mid; i++)
        {
            bool bit = array[i];
            array[i] = array[length - i - 1];
            array[length - i - 1] = bit;
        }
    }

    public IEnumerable<byte> UnhideData()
    {
        var encryptedData = _encryptedDataProvider!.Data.ToArray();
        var hiddenData = new List<byte>();
        var hiddenByteParts = new List<BitArray>();

        foreach (var cryptedBytePos in _key.ModificatedBytesPositions)
        {
            if (hiddenByteParts.Count == Consts.SplittedPartsAmount)
            {
                BitArray hiddenByte = new BitArray(0x00);
                foreach(var part in hiddenByteParts)
                {
                    hiddenByte = hiddenByte.Or(part);
                }
                hiddenData.Add(ConvertBitsToByte(hiddenByte));
                hiddenByteParts.Clear();
            }

            var bytePart = new BitArray(new byte[] { encryptedData[cryptedBytePos] });
            Reverse(bytePart);
            bytePart = bytePart
                .LeftShift(8 - Consts.ModificatedBitsInByte)
                .RightShift(8 - Consts.ModificatedBitsInByte)
                .RightShift(Consts.ModificatedBitsInByte * hiddenByteParts.Count);

            hiddenByteParts.Add(bytePart);
        }
        return hiddenData;
    }
}
