//log_a(y) (pmod n)

long FastPowAlg(long num, long pow, long mod)
{
    long result = 1;
    long next_num = num;

    while (pow > 0)
    {
        if (pow % 2 == 1)
        {
            result *= next_num;
            result %= mod;
        }
        next_num *= next_num;
        next_num %= mod;
        pow /= 2;
    }

    return result;
}

var a = int.Parse(Console.ReadLine()!);
var y = int.Parse(Console.ReadLine()!);
var n = int.Parse(Console.ReadLine()!);
var s = (int)Math.Floor(Math.Sqrt(n));

var firstSeq = new List<(long, long)>();
for(int r=0; r < s; ++r)
{
    firstSeq.Add((r, y * FastPowAlg(a, r, n) % n));
}
firstSeq.Sort((a, b) => a.Item2.CompareTo(b.Item2));

var seconsSeq = new List<(long, long)>();
for (int t = 1; t <= s; ++t)
{
    seconsSeq.Add((t*s, FastPowAlg(a, t * s, n)));
}
seconsSeq.Sort((a, b) => a.Item2.CompareTo(b.Item2));


var firstMappingToSecond = new Dictionary<(long, long), List<(long, long)>>();
firstSeq.ForEach(first =>
{
    if (!firstMappingToSecond.ContainsKey(first))
    {
        firstMappingToSecond.Add(first, new List<(long, long)>());
    }
    firstMappingToSecond[first].AddRange(seconsSeq.Where(second => second.Item2 == first.Item2));
});


for(int i = 0; i < firstMappingToSecond.Keys.Count; ++i)
{
    var key = firstMappingToSecond.Keys.ElementAt(i);
    for(int j = 0; j < firstMappingToSecond[key].Count; ++j)
    {
        var possibleX = firstMappingToSecond[key][j].Item1 - key.Item1;
        if (FastPowAlg(a, possibleX, n) == y)
        {
            var x = possibleX;
            Console.WriteLine($"x = {x}");
        }
    }   
}