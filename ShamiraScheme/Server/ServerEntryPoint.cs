using System.Numerics;

namespace Server;
class ServerEntryPoint
{
    public static void Main(string[] args)
    {
        Console.Write("p: ");
        var pStr = Console.ReadLine();
        var p = pStr == "" ? 1823 : BigInteger.Parse(pStr!);
        Console.Write("q: ");
        var qStr = Console.ReadLine();
        var q = qStr == "" ? 4813 : BigInteger.Parse(qStr!);
        Console.WriteLine($"Params: p = {p}, q = {q}");

        var server = new TrustedServer(p, q);
        server.StartReceiving();

        
    }  
}