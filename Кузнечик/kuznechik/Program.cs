using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Text;

namespace kuznechik
{
	public class MainClass
	{
		private const string KEY = "kYVM7EytcVuDbjdKtdL3pjjg4zc5kqRy"; //32 байта

		public static void Main(string[] args)
		{
			var keyBytes = Encoding.UTF8.GetBytes(KEY);

			Console.Write("Текст для шифрования: ");
			var textToEncrypt = Console.ReadLine();
			var textToEncryptBytes = Encoding.UTF8.GetBytes(textToEncrypt);
			
			var kuz = new Kuznechik(keyBytes);
			var encryptedData = kuz.Encrypt(textToEncryptBytes);
			var decryptedData = kuz.Decrypt(encryptedData);

			Console.WriteLine("E: " + Encoding.UTF8.GetString(encryptedData));
			Console.WriteLine("D: " + Encoding.UTF8.GetString(decryptedData));
		
			Console.Read();
		}
	}
}


