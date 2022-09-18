const UInt16 KEY = 42098; //1010010001110010

UInt16 Crypt(UInt16 number, UInt16 key, int rounds)
{
    var keys = PrepareKeys(key, rounds);

    for(int i=0; i<rounds; ++i)
    {
        DoFeistelRound(ref number, keys[i]);
        Console.WriteLine($"Round {i + 1}: {number}");
    }
    return number;
}

void DoFeistelRound(ref UInt16 num, byte key)
{
    var numBytes = GetBytes(num);
    UInt16 r = GetRight(numBytes[1]);
    UInt16 l = GetLeft(numBytes[0], key);
    num = (UInt16)(l + r);
}

UInt16 GetLeft(byte l, byte key)
{
    byte[] bytes = new byte[2];
    bytes[0] = (byte)(l ^ key);
    bytes[1] = 0;
    return BitConverter.ToUInt16(bytes);
}

UInt16 GetRight(byte num)
{
    var bytes = new byte[2];
    byte l = (byte)(num << 4);
    byte r = (byte)(num >> 4);
    bytes[0] = 0;
    bytes[1] = (byte)(l + r);
    return BitConverter.ToUInt16(bytes);
}

byte[] GetBytes(UInt16 num)
{
    return BitConverter.GetBytes(num).Reverse<byte>().ToArray();
} 

List<byte> PrepareKeys(UInt16 key, int rounds)
{
    List<byte> keys = new List<byte>();
    for (int i = 0; i < rounds; ++i)
    {
        int power = 8 - i * 3;
        power = power < 0 ? 0 : power;
        UInt16 key_i = (UInt16)((key / Math.Pow(2, power)) % 256);
        keys.Add(GetBytes(key_i)[1]);
    }
    return keys;
}

Console.Write("Number to crypt (<65535): ");
try
{
    var number = UInt16.Parse(Console.ReadLine()!);
    var result = Crypt(number, KEY, 4);
    Console.Write($"Crypted: {result}");
}
catch (Exception e) 
{
    Console.WriteLine(e);
}
finally 
{
    Console.Read();
}
/*
 * Number to crypt (<65535): 21345
Round 1: 5879
Round 2: 32565
Round 3: 21347
Round 4: 13857
Round 5: 4676
Round 6: 17504
Round 7: 1590
Round 8: 25460
 */


