using LSB.Classes.DataProviders;
using LSB.Interfaces;
using Microsoft.Win32.SafeHandles;

namespace LSB.Classes;
public static class KeyCreator
{
    public static bool Verify(IDataProvider originalDataProvider, IDataProvider dataToHideProvider)
    {
        return originalDataProvider.Data.Count() >= dataToHideProvider.Data.Count() * (8 / Consts.ModificatedBitsInByte);
    }
    /// <summary>
    /// Возвращает ключ для шифрования - ключ может содержать extension, если обрабатывается локально хранящийся файл
    /// </summary>
    /// <param name="originalDataProvider">провайдер данных, в которых будут прятаться другие данные</param>
    /// <param name="dataToHideProvider">провайдер данных, которые прячем</param>
    /// <returns></returns>
    public static LSBKey CreateKey(IDataProvider originalDataProvider, IDataProvider dataToHideProvider)
    {
        if (!Verify(originalDataProvider, dataToHideProvider))
        {
            return null;
        }

        var bytesPositions = GenerateBytesPositions(originalDataProvider, dataToHideProvider);

        if (originalDataProvider is LocalFileProvider)
        {
            return new LSBKey((originalDataProvider as LocalFileProvider)!.Extension, bytesPositions);
        }
        else
        {
            return new LSBKey(bytesPositions);
        }
    }

    private static IEnumerable<UInt32> GenerateBytesPositions(IDataProvider originalDataProvider, IDataProvider dataToHideProvider)
    {
        var bytesPositions = new List<UInt32>();


        for (int i = 0; i < dataToHideProvider.Data.Count() * Consts.SplittedPartsAmount; ++i)
        {
            UInt32 pos = (UInt32)(new Random().Next(15, originalDataProvider.Data.Count() - 1));
            while (bytesPositions.Contains(pos))
            {
                pos = (UInt32)(new Random().Next(15, originalDataProvider.Data.Count() - 1));
            }
            bytesPositions.Add(pos);
        }
        return bytesPositions;
    }
}
