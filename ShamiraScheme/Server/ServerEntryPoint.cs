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
        var server = new TrustedServer(host);
        server.StartReceiving();
    }  
}