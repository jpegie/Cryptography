using LSB.Classes.DataProviders;
using LSB.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSB.Classes;
public class LSBKey
{
    /* #### Ключ ####
        2 байта     : [0:2] - кол-во зашифрованных бит в байте 
        10 байтов   : [3:13] - расширение зашифрованного файла 
        n-12 байтов : [14:n] - позиции байтов, в которых зашифрована информация (между такими позициями 0x00 байт означает границу такого числа)
    */
    private string _extension = "";
    private IEnumerable<UInt32> _modificatedBytesPositions;

    public LSBKey(string extension, IEnumerable<UInt32> modificatedBytesPositions)
    {
        _extension = extension;
        _modificatedBytesPositions = modificatedBytesPositions;
    }
    public LSBKey(IEnumerable<UInt32> modificatedBytesPositions)
    {
        _modificatedBytesPositions = modificatedBytesPositions;
    }

    public IEnumerable<UInt32> ModificatedBytesPositions => _modificatedBytesPositions;
    public IEnumerable<byte> GetFullKey()
    {
        var extensionBytes = Encoding.UTF8.GetBytes(_extension).ToList();
        extensionBytes.InsertRange(0, Enumerable.Repeat<byte>(0x00, Consts.BytesCapacityInExtension - extensionBytes.Count));

        return BitConverter.GetBytes(Consts.ModificatedBitsInByte)
                .Concat(extensionBytes)
                .Concat(_modificatedBytesPositions.Cast<byte>());
    }
}
