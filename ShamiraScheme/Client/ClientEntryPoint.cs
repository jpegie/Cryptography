using Server;
using Server.Helpers;
using System.Diagnostics;

namespace Client;
class ClientEntryPoint
{
    static void Main(string[] args)
    {
        Console.Write("Username: ");
        var userName = Console.ReadLine()!;
        Console.Write("Directory for saving files: ");
        var filesDir = Console.ReadLine()!;
        Console.Write("Server's IP (nothing for localhost): ");
        var serverIp = Console.ReadLine()!;
        if (serverIp == "")
        {
            serverIp = Consts.LOCALHOST;
        }
        else if (!serverIp.StartsWith("tcp://"))
        {
            serverIp = "tcp://" + serverIp;
        }
        var user = new Client(userName, filesDir, serverIp);

        user.Start();
    }
}