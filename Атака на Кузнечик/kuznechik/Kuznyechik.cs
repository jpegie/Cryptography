using System;
using System.Collections.Generic;

namespace kuznechik
{
    public class Kuznechik
    {
        private const int BLOCK_LEN = 16;
        private const int MAIN_KEY_LEN = 32;
        private const int ITER_KEY_LEN = MAIN_KEY_LEN / 2;

        private byte[] _mainKey = null;
        private byte[][] _iterKeys = null;

        public Kuznechik(byte[] mainKey)
        {
            _mainKey = mainKey;
            GenIterationsKeys();
        }
    
        public byte[] Encrypt (byte[] data, int returnRound) 
        {

            var inputBlocks = BlockSplitter.ChunkBytesArray(data);
            var res = new List<byte>();

            foreach(var bl in inputBlocks)
            {
                var encryptedBlock = new byte[BLOCK_LEN];
                var temp = new byte[BLOCK_LEN];
            
                if (bl.Length != BLOCK_LEN)
                {
                    throw new Exception($"Размер блока должен быть === {BLOCK_LEN}");
                }

                Array.Copy(bl, encryptedBlock, BLOCK_LEN);

                for (int i = 0; i < 10; i++)
                {
                    if (i == 9) 
                    {
                        X(encryptedBlock, _iterKeys[9]);
                        break;
                    }

                    Array.Copy(_iterKeys[i], temp, BLOCK_LEN); 
                    
                    X(temp, encryptedBlock);
                    S(temp);
                    L(temp);

                    Array.Copy(temp, encryptedBlock, BLOCK_LEN);
                }
                

                res.AddRange(encryptedBlock);
            }

            return res.ToArray();
        }

		public byte[] Decrypt(byte[] data) 
        {
            var inputBlocks = BlockSplitter.ChunkBytesArray(data);
            var res = new List<byte>();

            foreach(var bl in inputBlocks)
            {
                var block = new byte[BLOCK_LEN];
                Array.Copy(bl, block, BLOCK_LEN);
                for (int i = 9; i >= 0; i--)
                {
                    X(block, _iterKeys[i]);

                    if (i != 0) //выполняем только не в последней итерации
                    {
                        RevL(block);
                        RevS(block);
                    }
                }
                
                res.AddRange(block);
            }
            return res.ToArray();
		}

        public void GenIterationsKeys()
        {
            _iterKeys = new byte[10][]; //10 итерационных ключей

            for (int i = 0; i < 10; i++)
            {
                _iterKeys[i] = new byte[ITER_KEY_LEN];
            }

            byte[][] iterC = new byte[32][]; // массив итерационных констант
            byte[][] iterNum = new byte[32][];
            for (int i = 0; i < 32; i++)
            {
                iterNum[i] = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, Convert.ToByte(i + 1) };
                L(iterNum[i]);
                iterC[i] = iterNum[i];
            }

            byte[] A = new byte[16];
            byte[] B = new byte[16];
            for (int i = 0; i < 16; i++)
            {
                A[i] = _mainKey[i];
                B[i] = _mainKey[16 + i];
            }

            _iterKeys[0] = new byte[16];
            _iterKeys[1] = new byte[16];
            Array.Copy(A, _iterKeys[0], 16);
            Array.Copy(B, _iterKeys[1], 16);


            byte[] C = new byte[16];
            byte[] D = new byte[16];

            for (int i = 0; i < 4; i++)
            {
                KuzF(A, B, ref C, ref D, iterC[0 + 8 * i]);
                KuzF(C, D, ref A, ref B, iterC[1 + 8 * i]);
                KuzF(A, B, ref C, ref D, iterC[2 + 8 * i]);
                KuzF(C, D, ref A, ref B, iterC[3 + 8 * i]);
                KuzF(A, B, ref C, ref D, iterC[4 + 8 * i]);
                KuzF(C, D, ref A, ref B, iterC[5 + 8 * i]);
                KuzF(A, B, ref C, ref D, iterC[6 + 8 * i]);
                KuzF(C, D, ref A, ref B, iterC[7 + 8 * i]);
                _iterKeys[2 * i + 2] = new byte[16];
                _iterKeys[2 * i + 3] = new byte[16];
                Array.Copy(A, _iterKeys[2 * i + 2], 16);
                Array.Copy(B, _iterKeys[2 * i + 3], 16);
            }
        }
        private void KuzF(byte[] input1, byte[] input2, ref byte[] output1, ref byte[] output2, byte[] round_C)
        {
            Array.Copy(input1, output2, input1.Length);

            X(input1, round_C);
            S(input1);
            L(input1);
            X(input1, input2);
            output1 = input1;
        }

        //https://ru.stackoverflow.com/questions/443209/
        // умножение в поле Галуа
        private byte MultiplyInGF256 (byte a, byte b) 
        {

            byte sum = 0; //накопительная сумма 
            byte firstBit;
            for (int i = 0; i < 8 ; i++) 
            {
                if ((b & 1) == 1) 
                {
                    sum ^= a; 
                }
                firstBit = (byte)(a & 0x80); //a & 10000000 
                a <<= 1; 
                if (firstBit == 1) 
                {
                    a ^= 0xc3; // x^8 + x^7 + x^6 + x + 1 <<< 11000011
                }
                b >>= 1;
            }
            return sum;
        }

        public void LSX(ref byte[] result, byte[] data)
        {
            X(result, data);
            S(result);
            L(result);
        }


        //нелинейное преобразование
        private void S (byte[] data) 
        {
            for (int i = 0; i < data.Length; i++) 
            {
                data[i] = TransformationTables.Pi[data[i]];
            }
        }

        //сложение двух двоичных векторов по модулю 2
        private void X (byte[] a, byte[] b) 
        {
            for (int i = 0; i < a.Length; i++) 
            {
                a[i] = (byte) (a[i] ^ b[i]);
            }
        }

        //функция R сдвигает данные и реализует уравнение, представленное для расчета L-функции
        private void R (byte[] data) 
        {
			byte z = data[15];
			for (int i = 14; i >= 0; i--)
			{
				z ^= MultiplyInGF256(data[i], TransformationTables.LFactors[i]);
			}
            for (int i = 15; i > 0; i--)
            {
                data[i] = data[i - 1];
            }
            data[0] = z;
        }

        //представляет одну итерацию развертывания ключа
        private void F(byte [] c_const, byte[] a1, byte[] a0)
        {
            var temp = new byte[ITER_KEY_LEN];
            Array.Copy(c_const, temp, BLOCK_LEN);

            X(temp, a0);
            S(temp);
            L(temp);

            X(temp, a0);

            Array.Copy(a1, a0, ITER_KEY_LEN);
            Array.Copy(temp, a1, ITER_KEY_LEN);

        }


        //линейное преобразование
        private void L(byte[] data)
        {
            for (int i = 0; i < 16; i++)
            {
                R(data);
            }
        }

        //обратное нелинейное биективное преобразование 
        private void RevS(byte[] data)
        {
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = TransformationTables.ReversedPi[data[i]];
            }
        }
        private void RevR(byte[] data)
		{
            
			var z = data[0];

			for (int i = 0; i < 15; i++) 
            {
				data[i] = data[i + 1];
				z ^= MultiplyInGF256(data[i], TransformationTables.LFactors[i]);
			}

			data[15] = z;
        }
        private void RevL(byte[] data)
        {
            for (int i = 0; i < 16; i++)
            {
                RevR(data);
            }
        }
    }
}
