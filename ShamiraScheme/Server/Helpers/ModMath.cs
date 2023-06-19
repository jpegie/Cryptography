using System.Numerics;

namespace Server.Helpers;
public static class ModMath
{
    public static BigInteger ModDivision(BigInteger a, BigInteger b, BigInteger N)
    {
        // Проверка деления на ноль
        if (b == 0)
        {
            throw new DivideByZeroException("Деление на ноль невозможно");
        }

        // Находим обратный элемент b в кольце по модулю N
        BigInteger bInverse = ModInverse(b, N);

        // Выполняем деление a на b с учетом обратного элемента
        BigInteger result = (a * bInverse) % N;

        return result;
    }

    // Функция для нахождения обратного элемента в кольце по модулю N
    public static BigInteger ModInverse(BigInteger a, BigInteger N)
    {
        BigInteger m0 = N;
        BigInteger y = 0, x = 1;

        while (a > 1)
        {
            BigInteger q = a / N;
            BigInteger t = N;

            N = a % N;
            a = t;
            t = y;

            y = x - q * y;
            x = t;
        }

        if (x < 0)
        {
            x += m0;
        }

        return x;
    }
}
