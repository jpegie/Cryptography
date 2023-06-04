using System.Numerics;
namespace Server.Extensions;

public static class RandomExtensions
{
    [ThreadStatic] private static Random Local;

    public static Random ThisThreadsRandom
    {
        get { return Local ?? (Local = new Random(unchecked(Environment.TickCount * 31 + Thread.CurrentThread.ManagedThreadId))); }
    }
    /// <summary>
    /// Returns a random BigInteger that is within a specified range.
    /// The lower bound is inclusive, and the upper bound is exclusive.
    /// </summary>
    public static BigInteger NextBigInteger(this Random random,
        BigInteger minValue, BigInteger maxValue)
    {
        if (minValue > maxValue) throw new ArgumentException();
        if (minValue == maxValue) return minValue;
        BigInteger zeroBasedUpperBound = maxValue - 1 - minValue; // Inclusive
        byte[] bytes = zeroBasedUpperBound.ToByteArray();

        // Search for the most significant non-zero bit
        byte lastByteMask = 0b11111111;
        for (byte mask = 0b10000000; mask > 0; mask >>= 1, lastByteMask >>= 1)
        {
            if ((bytes[bytes.Length - 1] & mask) == mask) break; // We found it
        }

        while (true)
        {
            random.NextBytes(bytes);
            bytes[bytes.Length - 1] &= lastByteMask;
            var result = new BigInteger(bytes);
            if (result <= zeroBasedUpperBound) return result + minValue;
        }
    }
}


