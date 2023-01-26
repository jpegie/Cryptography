using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinearCongruentialGenerator;

public class GammaFunctions
{
    public static double GammaUpperCont(double a, double x)
    {
        // Incomplete GammaUpper computed by continuing fraction
        if (x < 0.0)
            throw new Exception("x param less than 0.0 in GammaUpperCont");
        double gln = LogGamma(a);
        double b = x + 1.0 - a;
        double c = 1.0 / 1.0E-30; // Div by close to double.MinValue
        double d = 1.0 / b;
        double h = d;
        for (int i = 1; i <= 100; ++i)
        {
            double an = -i * (i - a);
            b += 2.0;
            d = an * d + b;
            if (Math.Abs(d) < 1.0E-30) d = 1.0E-30; // Got too small?
            c = b + an / c;
            if (Math.Abs(c) < 1.0E-30) c = 1.0E-30;
            d = 1.0 / d;
            double del = d * c;
            h *= del;
            if (Math.Abs(del - 1.0) < 3.0E-7)
                return Math.Exp(-x + a * Math.Log(x) - gln) * h;  // Close enough?
        }
        throw new Exception("Unable to compute GammaUpperCont " +
          "to desired accuracy");
    }
    public static double LogGamma(double x)
    {
        double[] coef = new double[6] { 76.18009172947146, -86.50532032941677,
    24.01409824083091, -1.231739572450155,
    0.1208650973866179E-2, -0.5395239384953E-5 };
        double LogSqrtTwoPi = 0.91893853320467274178;
        double denom = x + 1;
        double y = x + 5.5;
        double series = 1.000000000190015;
        for (int i = 0; i < 6; ++i)
        {
            series += coef[i] / denom;
            denom += 1.0;
        }
        return (LogSqrtTwoPi + (x + 0.5) * Math.Log(y) -
          y + Math.Log(series / x));
    }
}
