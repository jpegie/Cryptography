using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace kuznechik
{
	public class BlockSplitter
	{
		public static List<byte[]> ChunkInputText(string inputText, int chunkSize = 16)
		{
			var res = new List<byte[]>();
			var bytes = Encoding.UTF8.GetBytes(inputText);
			var blocksAmnt = (int)Math.Ceiling((double)bytes.Length / chunkSize);

			for (int i = 0; i < blocksAmnt; ++i)
			{
				var chunk = bytes.Skip(i*chunkSize).Take(chunkSize).ToList();

				if (chunk.Count != chunkSize)
				{
					chunk.AddRange(Enumerable.Repeat((byte)0x00, chunkSize - chunk.Count));
				}

				res.Add(chunk.ToArray());
			}
			return res;
		}
		public static List<byte[]> ChunkBytesArray(byte[] data, int chunkSize = 16)
		{
			var res = new List<byte[]>();
			var blocksAmnt = (int)Math.Ceiling((double)data.Length / chunkSize);
		
			for(int i=0; i<blocksAmnt; ++i)
			{
				var chunk = data.Skip(i * chunkSize).Take(chunkSize).ToList();
				if (chunk.Count != chunkSize)
				{
                    chunk.AddRange(Enumerable.Repeat((byte)0x00, chunkSize - chunk.Count));
                }
				res.Add(chunk.ToArray());
			}

			return res;
		}
	}
}
