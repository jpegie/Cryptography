using System.Drawing;

namespace LSB_;
public static class LSBcrypter
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Interoperability", "CA1416:Проверка совместимости платформы", Justification = "<Ожидание>")]
    public static Bitmap HideData(Bitmap image, IEnumerable<byte> data)
    {
        var bytes = data.ToList();
        var currByte = 0;
        var bitsWritten = 0; //8 -> Байт записали
        var isWrittingOnlyRG = false;

        for (int x = 0; x < image.Width; ++x)
        {
            for (int y = 0; y < image.Height; ++y)
            {
                Color imagePixel = image.GetPixel(x, y);

                var r = imagePixel.R - imagePixel.R % 2;
                var g = imagePixel.G - imagePixel.G % 2;
                var b = imagePixel.B - imagePixel.B % 2;

                if (currByte == bytes.Count)
                {
                    return image;
                }
                else
                {
                    if (bitsWritten == 6)
                    {
                        isWrittingOnlyRG = true;
                    }

                    r += CutByte(bytes, currByte, ref bitsWritten);
                    g += CutByte(bytes, currByte, ref bitsWritten);

                    if (!isWrittingOnlyRG)
                    {
                        b += CutByte(bytes, currByte, ref bitsWritten);
                    }
                }

                image.SetPixel(x, y, Color.FromArgb(r, g, b));

                if (bitsWritten == 8)
                {
                    bitsWritten = 0;
                    isWrittingOnlyRG = false;
                    currByte++;
                }
            }
        }
        return image;
    }

    private static int CutByte(List<byte> bytes, int i, ref int bitsWritten)
    {
        var cutten = bytes[i] % 2;
        bytes[i] /= 2;
        bitsWritten++;
        return cutten;

    }
    public static IEnumerable<byte> RevealData(Bitmap image, int bytesHidden)
    {
        List<bool> bits = new List<bool>();
        List<byte> hiddenBytes = new List<byte>();

        for(int x = 0; x < image.Width ; ++x)
        {
            for(int y = 0; y < image.Height; ++y)
            {
                if (hiddenBytes.Count == bytesHidden)
                {
                    return hiddenBytes;
                }

                Color imagePixel = image.GetPixel(x, y);

                bits.Add(imagePixel.R % 2 == 1);
                bits.Add(imagePixel.G % 2 == 1);

                if (bits.Count == 8)
                {
                    bits.Reverse();
                    hiddenBytes.Add(ConvertBoolArrayToByte(bits.ToArray()));
                    bits.Clear();    
                }
                else
                {
                    bits.Add(imagePixel.B % 2 == 1);
                }
            }
        }

        return hiddenBytes;
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
}
