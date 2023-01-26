using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinearCongruentialGenerator;
public class LCG : ILCG
{
    private int _module, _a, _b, _x0;
    private Dictionary<int, int> _setDict;
    private List<Dictionary<int, int>> _sets;
    private List<int> _totalSet;
    private List<bool> _totalBits;
    public LCG(int module, int a, int b, int x0)
    {
        _module = module;
        _a = a;
        _b = b;
        _x0 = x0;
        _totalBits = new List<bool>();
        _totalSet = new List<int>();
    }

    public List<int> TotalSet => _totalSet;
    public List<bool> TotalBits => _totalBits;
    public Dictionary<int, int> SetDictionary => _setDict;
    public void GenerateSets(int length)
    {
        _sets = new List<Dictionary<int, int>>();
        var setsGenerator = new LCG_SetsGenerator(_module);
        setsGenerator.Generate();

        foreach (var a in setsGenerator.SetA)
        {
            foreach(var b in setsGenerator.SetB)
            {
                _a = a;
                _b = b;
                GenerateSet(new object[] { length });
                _sets.Add(_setDict);
            }
        }
        return;
    }

    public void GenerateSet(object [] ? args)
    {
        var length = (int)args![0];

        _setDict = new Dictionary<int, int>();
        _setDict.Add(_x0, 1);
        _totalSet.Add(_x0);
        var lastX = _x0;

        for(int i=0; i< length - 1; ++i)
        {
            var newX = (_a * lastX + _b) % _module;
            if (_setDict.ContainsKey(newX))
            {
                _setDict[newX]++;
            }
            else
            {
                _setDict.Add(newX, 1);
            }
            _totalSet.Add(newX);
            lastX = newX;
        }
    }

    
    public void Print()
    {
        foreach(var pair in _setDict)
        {
            Console.WriteLine($"{pair.Key.ToString()}\t{pair.Value.ToString()}");
        }
    }
}


class LCG_SetsGenerator
{
    private int _module;

    public LCG_SetsGenerator(int module)
    {
        _module = module;
    }

    public List<int> SetA { get; set; }
    public List<int> SetB { get; set; }

    public void Generate()
    {
        var simpleDeviders = GetModuleSimpleDeviders();
        SetA = GenerateValidSet_A(simpleDeviders);
        SetB = GenerateValidSet_C();
    }

    private List<int> GenerateValidSet_C()
    {
        var validSet = new List<int>();

        for (int possibleC = 0; possibleC < 1_000; ++possibleC)
        {
            if (GcdSlim(possibleC, _module) == 1 || possibleC == _module)
            {
                validSet.Add(possibleC);
            }
        }

        return validSet;
    }

    private List<int> GetModuleSimpleDeviders()
    {
        var simpleDeviders = new List<int>();
        for (int devider = 0; devider < _module; ++devider)
        {
            if (GcdSlim(devider, _module) == 1 || devider == _module)
            {
                simpleDeviders.Add(devider);
            }
        }
        return simpleDeviders;
    }
    private List<int> GenerateValidSet_A(List<int> simpleDeviders)
    {
        var validSetA = new List<int>();
        for (int possibleA = 0; possibleA < _module; ++possibleA)
        {
            var valid = true;
            foreach (var devider in simpleDeviders)
            {
                if (possibleA % devider != 0
                   || _module % 4 == 0 && possibleA % 4 != 0)
                {
                    valid = false;
                    break;
                }
            }
            if (valid)
            {
                validSetA.Add(possibleA + 1);
            }

        }
        return validSetA;
    }


    private int GcdSlim(int a, int b)
    {
        int x, y;
        return Gcd(a, b, out x, out y);
    }

    private int Gcd(int a, int b, out int x, out int y)
    {

        if (a == 0)
        {
            x = 0;
            y = 1;
            return b;
        }

        int gcd = Gcd(b % a, a, out x, out y);

        int newY = x;
        int newX = y - (b / a) * x;

        x = newX;
        y = newY;
        return gcd;
    }
}