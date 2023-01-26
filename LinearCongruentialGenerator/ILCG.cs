using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LinearCongruentialGenerator;
public interface ILCG
{
    public void Print();
    public Dictionary<int, int> SetDictionary { get; }
    public void GenerateSet(object []? args);
    public List<bool> TotalBits { get; }
    public List<int> TotalSet { get; }
}
