using System;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace kuznechik
{
	public class MainClass
	{
		private const string KEY = "kYVM7EytcVuDbjdKtdL3pjjg4zc5kqRy"; //32 байта

		public static void Main(string[] args)
		{
			var keyBytes = Encoding.UTF8.GetBytes(KEY);

			var normal_kuz = new Kuznechik(keyBytes);
			var corrupted_kuz = new KuznechikCorrupted(keyBytes);

			var key0 = new byte[16];
			var key1 = new byte[16];

			var timers = new Stopwatch[2];

            for (int key_i = 0; key_i < 2; ++key_i)
			{
				timers[key_i] = new Stopwatch();
				timers[key_i].Start();

                for (int injectingByteIndex = 0; injectingByteIndex < 16;)
				{
					var foundKeyByte = false;
                    for (int byteVal = 0; byteVal < 256; ++byteVal)
					{
						if (foundKeyByte || injectingByteIndex >= 16)
						{
							break;
						}
                        var generatedData = GenerateRandomInputMessage(16);
                        generatedData[injectingByteIndex] = (byte)byteVal;
						
						var normal_Encrypted = normal_kuz.Encrypt(generatedData, key_i);
						var corrupted_Encrypted = corrupted_kuz.Encrypt(generatedData, injectingByteIndex, key_i);
						var areOutputsEqual = AreArraysEqual(corrupted_Encrypted, normal_Encrypted);

						if (!areOutputsEqual)
						{
							continue;
						}

						if (key_i == 0)
						{
							key0[injectingByteIndex] = (byte)(byteVal ^ TransformationTables.ReversedPi[0]);
						}

						if (key_i == 1)
						{
							var inputDataCopy = new byte[generatedData.Length];
							generatedData.CopyTo(inputDataCopy, 0);
							normal_kuz.LSX(ref inputDataCopy, key0);
							key1[injectingByteIndex] = (byte)(inputDataCopy[injectingByteIndex] ^ TransformationTables.ReversedPi[0]);
						}
						foundKeyByte =
							true;
						injectingByteIndex++;

                    }
				}
				timers[key_i].Stop();
			}
            Console.WriteLine($"Мастер-ключ: {KEY}");
            Console.WriteLine($"1 итер. ключ: {Encoding.UTF8.GetString(key0)} | время нахождения = {timers[0].Elapsed.ToString()}");
            Console.WriteLine($"2 итер. ключ: {Encoding.UTF8.GetString(key1)} | время нахождения = {timers[1].Elapsed.ToString()}");
            Console.Read();
		}

		private static byte[] GenerateRandomInputMessage(int size)
		{
            byte[] randomBytes = new byte[size];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
			return randomBytes;
        }

		private static bool AreArraysEqual(byte[] corruptedEncrypted, byte[] encryptedData)
		{
			if (corruptedEncrypted.Length != encryptedData.Length)
			{
				return false;
			}

			for(int i = 0; i < corruptedEncrypted.Length; ++i)
			{
				if (corruptedEncrypted[i] != encryptedData[i])
				{
					return false;
				}
			}
			return true;
		}
	}
}


