using System.Numerics;

/*
 * !!!Алиса хочет отправить сообщение Бобу!!!
 * 1) Алиса генерирует приватный и публичный ключи RSA
 * 2) Боб генерирует приватный и публичный ключи RSA
 * 3) Алиса отправляет свой публичный ключ Бобу
 * 4) Боб отправляет свой публичный ключ Алисе
 * 5) Алиса создает сообщение и прикрепляет к нему подпись
 * 6) Алиса шифрует сообщение с подписью с помощью публичного ключа Боба
 * 7) Алиса отправляет свой сообщение Бобу
 * 8) Боб принимает сообщение Алисы
 * 9) Боб расшифровывает принятое сообщение с помощью своего закрытого ключа
 * 10) Боб находит прообраз подписи и сравнивает его с полученным сообщение, 
 * если все совпадает, то сообщение подлинное, иначе сообщение было подменено
 */


class Program
{
    //https://ilovecalc.com/calcs/maths/prime-number-generator/894/
    static void Main(string[] args)
    {
        Console.Write("Логин: ");
        var username = Console.ReadLine();
        Console.Write("Введите значение p: ");
        var p = BigInteger.Parse(Console.ReadLine()!);
        Console.Write("Введите значение q: ");
        var q = BigInteger.Parse(Console.ReadLine()!);
        Console.WriteLine();

        var protocol = new RSAprotocol(p, q, username!);

        while (true)
        {
            Console.WriteLine("1. Отправить сообщение"
                            + "\n2. Получить сообщение"
                            + "\n3. Выход");
            Console.Write("Ввод: ");
            var input = int.Parse(Console.ReadLine()!);
            Console.WriteLine();
            switch (input)
            {
                case 1: protocol.SendMessage(); break;
                case 2: protocol.ReceiveMessage(); break;
                default: return;
            }
        }
    }
}