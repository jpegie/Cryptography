namespace EllipticCurves;


public static class EllipticPointsOperations
{
    const int O_NUM_EQUIVALENT = -999;
    static int _a;
    static int _module;

    public static int Module
    {
        get => _module;
        set => _module = value; 
    }

    public static int A
    {
        get => _a;
        set => _a = value;
    }
    public static int GetPointOrder(EllipticPoint point)
    {
        for (int i = 1; i < _module; ++i)
        {
            if (Multiply(point, i).IsO)
            {
                return i;
            }
        }
        return -1;
    }
    public static EllipticPoint Sum(EllipticPoint point1, EllipticPoint point2)
    {
        if (point1.IsO)
        {
            return point2;
        }

        if (point2.IsO)
        {
            return point1;
        }

        if ((point1.X == point2.X) && (point2.Y == -point2.Y))
        {
            return new EllipticPoint(0, 0)
            {
                IsO = true
            };
        }

        var alpha = GetAlphaForSumming(point1, point2);
        if (alpha == O_NUM_EQUIVALENT)
        {
            return new EllipticPoint(0, 0) { IsO = true };
        }
        var newX = GetModuledNumber((int)Math.Pow(alpha, 2) - point1.X - point2.X);
        var newY = GetModuledNumber(alpha * (point1.X - newX) - point1.Y);

        return new EllipticPoint(newX, newY);
    }

    public static int GetModuledNumber(int num)
    {
        while (num < 0)
        {
            num += _module;
        }
        return (num % _module);
    }
    public static EllipticPoint Subtract(EllipticPoint point1, EllipticPoint point2)
    {
        point2.Y = GetModuledNumber(-point2.Y);
        return Sum(point1, point2);
    }
    public static EllipticPoint Multiply(EllipticPoint point, int factor)
    {
        EllipticPoint result = point;
        for (int i = 0; i < factor - 1; ++i)
        {
            result = Sum(result, point);
        }
        return result;
    }

    public static int GetAlphaForSumming(EllipticPoint point1, EllipticPoint point2)
    {
        int reversed;
        if (!point1.Equals(point2))
        {
            if (point1.X == point2.X)
            {
                return O_NUM_EQUIVALENT;
            }
            TryModInverse(GetModuledNumber(point2.X - point1.X), out reversed);
            return GetModuledNumber((point2.Y - point1.Y) * reversed);
        }
        else
        {
            TryModInverse(GetModuledNumber(2 * point1.Y), out reversed);
            return GetModuledNumber((3 * (int)Math.Pow(point1.X, 2) + _a) * reversed);
        }

    }
    public static bool TryModInverse(int number, out int result)
    {
        int n = number;
        int m = _module, v = 0, d = 1;
        while (n > 0)
        {
            int t = m / n, x = n;
            n = m % x;
            m = x;
            x = d;
            d = checked(v - t * x); // Just in case
            v = x;
        }
        result = v % _module;
        if (result < 0) result += _module;
        if ((long)number * result % _module == 1L) return true;
        result = default;
        return false;
    }
}
