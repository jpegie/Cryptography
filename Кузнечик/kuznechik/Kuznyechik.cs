using System;
using static System.Net.Mime.MediaTypeNames;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

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
    
        public byte[] Encrypt (byte[] data) 
        {

            var inputBlocks = BlockSplitter.ChunkBytesArray(data); //разбитие на блоки + дополнение последнего нулями
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
                    //10-ая итерция не полная и включает только X()
                    if (i == 9) 
                    {
                        X(encryptedBlock, _iterKeys[9]);
                        break;
                    }

                    /* 
                     * тут нужно скопировать ключ в temp, 
                     * потому что все функции работают с входным массивом (temp) по ссылке и ничего результирующего не возвращают
                    */
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

        private void GenIterationsKeys () 
        {
            _iterKeys = new byte[10][]; //10 итерационных ключей

            for (int i = 0; i < 10; i++) 
            {
                _iterKeys[i] = new byte[ITER_KEY_LEN];
            }

            var key0 = new byte[ITER_KEY_LEN];
            var key1 = new byte[ITER_KEY_LEN];
            var c_const = new byte[ITER_KEY_LEN];

            //заполнение первых двух итерационных ключей, 0 ключ - левая половина мастер-ключа, 1 ключ - правая половина мастер-ключа
            for (int i = 0; i < ITER_KEY_LEN; i++) 
            {
                key0[i] = _mainKey[i];
                key1[i] = _mainKey[i + 16];
            }

            //вставка только что найденнных 0 и 1 итерационных ключей в массив с ключами
            Array.Copy(key0, _iterKeys[0], ITER_KEY_LEN); 
            Array.Copy(key1, _iterKeys[1], ITER_KEY_LEN);

            var iter_i = 1;
            for (int i = 0; i < 4; i++) 
            {
                for (int j = 0; j < 8; j++)
                {
                    c_const[15] = (byte)iter_i; L(c_const); //нахождение константы C для генерации ключа
                    F(c_const, key0, key1);
                    iter_i++;
                }

                Array.Copy(key0, _iterKeys[2 * i], ITER_KEY_LEN); //четный ключ
                Array.Copy(key1, _iterKeys[2 * i + 1], ITER_KEY_LEN); //нечетный ключ
            }
        }

        //https://ru.stackoverflow.com/questions/443209/
        // умножение в поле Галуа
        private byte MultiplyInGF256 (byte a, byte b) 
        {
            /*
             * a и b нужно понимать как многочлены - 1 соответствует имеющейся степени в многочлене
             * т.е. если a === 27 === 11011 === 1 + x + 0*x^2 + x^3 + x^4 + 0*x^5 + 0*x^6 + 0*x^7 + x^8 
             * x^8 === 100000000 === 256 -> после Галуа из 256 элементов === GF(256)
             * 
             * В итоге функция выполняет перемножение многочленов в столбик
             */


            byte sum = 0; //накопительная сумма (то же, что и при умножении столбиком)
            byte firstBit;
            for (int i = 0; i < 8 ; i++) //многочлен представляется как 8 членов, некоторые === 1, некоторые === 0
            {
                //здесь смотрим, если последний бит === 1, тогда умножим на него и запишем результат в sum
                if ((b & 1) == 1) 
                {
                    sum ^= a; //умножение эквивалентно сумме по модулю 2, т.е. XOR
                }
                firstBit = (byte)(a & 0x80); //a & 10000000 (128) <- запомним старший член многочлена a
                a <<= 1; //сдвигаем a на бит влево как бы говоря, что на старший бит мы умножили и просто убираем его
                if (firstBit == 1) //если старший многочлен === 1, то вычитаем (что то же самое, что и сложение) из a многочлен 11000011
                {
                    a ^= 0xc3; // x^8 + x^7 + x^6 + x + 1 <<< 11000011
                }
                b >>= 1; //умножали на младший член многочлена b, поэтому уберем его и перейдем к следующему коэффициенту
            }
            return sum;
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
