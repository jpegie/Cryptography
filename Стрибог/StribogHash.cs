using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Стрибог;
public class StribogHash
{
    private const int BYTES_LEN_COMMON = 64;

    private byte[] _n = new byte[BYTES_LEN_COMMON]; //длина данных
    private byte[] _sigma = new byte[BYTES_LEN_COMMON]; //сумма всех блоков (по 64 байта) данных по по модулю 512
    private byte[] _initVec = new byte[BYTES_LEN_COMMON]; //вектор инициализации
    private int _hashLen = 256;

    public StribogHash(int hashLen = 256)
    {
        _hashLen = hashLen;
        InitInitVec();
    }

    private void InitInitVec()
    {
        for (int i = 0; i < BYTES_LEN_COMMON; ++i)
        {
            _initVec[i] = (byte)(_hashLen == 512 ? 0x00 : 0x01);
        }
    }

    public IEnumerable<byte> GetHash(IEnumerable<byte> data)
    {
        var dataArr = data.ToArray();
       
        var vec0 = new byte[BYTES_LEN_COMMON];
        var hash = new byte[BYTES_LEN_COMMON];
        _initVec.CopyTo(hash, 0);

        if (dataArr.Length >= BYTES_LEN_COMMON) //если длина данных > 64 байтов, то надо обрезать до <= 64
        {
            dataArr = CutTo64(ref hash, dataArr);
        }

        var dataBitsLenInBytes = BitConverter //кол-во бит в данных после сокращения
            .GetBytes(dataArr.Length * 8)
            .Reverse()
            .ToArray();

        if (dataArr.Length < BYTES_LEN_COMMON) //если после сокращения получилось не 64 байта, то дополним до 64
        {
            dataArr = FillUpTo64(dataArr);
        }

        hash = G(_n, hash, dataArr); //применяем функцию сжатия к последнего неполному блоку (уже дополненному)
        _n = SumMod512(_n, dataBitsLenInBytes); //находим длину данных 
        _sigma = SumMod512(_sigma, dataArr); //сигма здесь это сумма по блокам (можно поглядеть в CutTo64()), кроме последнего, который был неполны -> прибавим этот неполный блок
        hash = G(vec0, hash, _n); //сжимаем длину сообщения
        hash = G(vec0, hash, _sigma); //добавляем в хэш еще и контрольную суммы по блокам

        if (_hashLen == 512)
        {
            return hash;
        }
        else //если выходной хэш 256, то вернем просто первые 32 байта, т.е. 256 бита
        {
            byte[] h256 = new byte[32];
            Array.Copy(hash, 0, h256, 0, 32);
            return h256;
        }
    }

    //Дополняет массив байтов до 512 битов, т.е. 64 байтов
    private byte[] FillUpTo64(byte[] data)
    {
        //в начало добить 0, потом одну 1 и сам dataArr
        var zeroesAmount = BYTES_LEN_COMMON - data.Length - 1;
        var res = Enumerable
            .Repeat((byte)0x00, zeroesAmount)
            .Append((byte)0x01).ToList();
        res.AddRange(data);
        return res.ToArray();
    }

    /*
     * Исходные данные делит на блоки по 64 байта, применяет к ним функцию сжатию
     * и затем две операции суммы по модулю 2^512
     */
    private byte[] CutTo64(ref byte[] hash, byte[] data)
    {
        var blocksAmount = data.Length / BYTES_LEN_COMMON;
        
        for (int i = 0; i < blocksAmount; ++i)
        {
            var last512bits = data
                .SkipLast(i*BYTES_LEN_COMMON)
                .TakeLast(BYTES_LEN_COMMON)
                .ToArray();

            hash = G(_n, hash, last512bits);
            _n = SumMod512(_n, Data.Vect512);
            _sigma = SumMod512(_sigma, last512bits);
        }
        
        var leftover = data
                       .SkipLast(blocksAmount * BYTES_LEN_COMMON)
                       .ToArray();

        return leftover;
    }

    //XOR двух векторов, в частности операция X
    private byte[] SumXor512(byte[] a, byte[] b)
    {
        byte[] res = new byte[BYTES_LEN_COMMON];
        for(int i = 0; i < BYTES_LEN_COMMON; ++i)
        {
            res[i] = (byte)(a[i] ^ b[i]);
        }
        return res;
    }
    
    //Преобразование E
    private byte[] E(byte[] key, byte[] m)
    {
        var res = SumXor512(key, m);
        for (int i = 0; i < 12; ++i)
        {
            res = S(res);
            res = P(res);
            res = L(res);
            key = GetKeyForRound(key, i);
            res = SumXor512(res, key);
        }
        return res;
    }
    //Сложение двух векторов в кольце (просто столбиком складываем два вектора, будто это числа)
    private byte[] SumMod512(byte[] a, byte[] b)
    {
        var aTemp = new byte[BYTES_LEN_COMMON]; Array.Copy(a, 0, aTemp, BYTES_LEN_COMMON - a.Length, a.Length);
        var bTemp = new byte[BYTES_LEN_COMMON]; Array.Copy(b, 0, bTemp, BYTES_LEN_COMMON - b.Length, b.Length);

        var sum = 0;
        var sumBytes = new byte[BYTES_LEN_COMMON];
        for(int i = BYTES_LEN_COMMON - 1; i >= 0; --i)
        {
            sum = aTemp[i] + bTemp[i] + (sum >> 8); //складываем каждый байт и из предыдущего сложения переносим 1
            sumBytes[i] = (byte)(sum & 0xFF); //операция И с 255, чтобы убрать 1 в 9 разряде, если она есть (число не должно быть больше 255)
        }
        return sumBytes;
    }

    //Возращает ключ для раунда
    private byte[] GetKeyForRound(byte[] key, int round_i)
    {
        key = SumXor512(key, Data.C[round_i]);
        key = S(key);
        key = P(key);
        key = L(key);
        return key;
    }

    //Функция сжатия
    private byte[] G(byte[] n, byte[] hash, byte[] data)
    {
        var k = SumXor512(hash, n);
        k = S(k);
        k = P(k);
        k = L(k);
       
        var t = E(k, data);
        t = SumXor512(t, hash);

        var newHash = SumXor512(t, data);
        return newHash;
    }

    
    //Подставновка s-блоков
    private byte[] S(byte[] data)
    {
        byte[] res = new byte[BYTES_LEN_COMMON];
        for (int i = 0; i < BYTES_LEN_COMMON; ++i)
        {
            res[i] = Data.Sbox[data[i]];
        } 
        return res;
    }

    //Переставляет байты использую таблицу Tau
    private byte[] P(byte[] data)
    {
        byte[] res = new byte[BYTES_LEN_COMMON];
        for (int i = 0; i < BYTES_LEN_COMMON; ++i)
        {
            res[i] = data[Data.Tau[i]];
        }
        return res;
    }

    //Линейное преобразование
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
            var multiplied = 0ul; //произведение

            for (int i = 0; i < BITS_IN_BLOCK; ++i)
            {
                if (bits[i]) //перемножаем константы из массив A только тогда, когда встречаем бит равный 1
                {
                    multiplied ^= Data.A[i];
                }
            }

            var resBlock = GetBytesArrayFromUlong(multiplied);
            res.AddRange(resBlock);
        }
        return res.ToArray();
    }

    //Переводит массив байт в массив бит
    private bool[] GetBoolArrayFromBytes(byte[] data)
    {
        var res = new bool[data.Length * 8];
        new BitArray(data).CopyTo(res, 0);
        res = res.Reverse().ToArray();
        return res;
    }

    //Переводит ulong в массив байт
    private byte[] GetBytesArrayFromUlong(ulong num)
    {
        var res = BitConverter.GetBytes(num).Reverse().ToArray();
        return res;
    }
}
