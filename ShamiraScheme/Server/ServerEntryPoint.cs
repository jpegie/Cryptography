using Server.Helpers;
using System.Numerics;

namespace Server;
class ServerEntryPoint
{
    public static void Main(string[] args)
    {
        Console.Write("Host (nothing for localhost, * for all): ");
        var host = Console.ReadLine()!;
        if (host == "")
        {
            host = Consts.LOCALHOST;
        }
        else if (host == "*")
        {
            host = Consts.ALL_HOST;
        }
        Console.Write("p: ");
        var pStr = Console.ReadLine();
        var p = pStr == "" ? BigInteger.Parse("8846778806957649834997871053200051444979523428026682443142099987129564087752101788125535577616106496045221480272165491344042775186947347532309520582967477") : BigInteger.Parse(pStr!);
       // p = BigInteger.Parse("497485402448183");
        Console.Write("q: ");
        var qStr = Console.ReadLine();
        var q = qStr == "" ? BigInteger.Parse("11328952343021082769345119262888614658459187014050612680004022585385352282751653486624042341733768919829382948725920483205216436528108236412642889996910961") : BigInteger.Parse(qStr!);
       // q = BigInteger.Parse("505400128081019");
        Console.WriteLine($"Params: p = {p}, q = {q}");
        var server = new TrustedServer(p, q, host);
        server.StartReceiving();

        
    }  
}