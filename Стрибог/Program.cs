using System.Text;
using Стрибог;

byte[] dataToHash;

Console.Write("Введите 0 для ввода своего текста, 1 для данных из ГОСТа: ");

if (Console.ReadLine() == "0")
{
    Console.Write("Текст: ");
    dataToHash = Encoding.UTF8.GetBytes(Console.ReadLine()!);
}
else
{
    dataToHash = Data.InputDataFromGOST;
}

Console.WriteLine($"HEX: {Convert.ToHexString(dataToHash)}");

Console.Write("Стрибог 256 или 512? ");
var hashType = int.Parse(Console.ReadLine()!);

var hash = new StribogHash(hashType).GetHash(dataToHash);
Console.WriteLine($"Хэш: {Convert.ToHexString(hash.ToArray())}");

//Хэш-кодом сообщения M_1, является значение:
//H(M_1) = 00557be5e5841d52a449M6b0251d05d27f94ab76cbaa6da890b59d8e11e159d.
