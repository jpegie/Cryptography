using Server.Helpers;
using System.Diagnostics;

namespace Client;
class ClientEntryPoint
{
    static void Main(string[] args)
    {
        ExecutionHelper.Execute("C:\\User1\\decrypted.data");
        return;


        Console.Write("Username: ");
        var userName = Console.ReadLine()!;
        Console.Write("Directory for saving files: ");
        var filesDir = Console.ReadLine()!;
        var user = new Client(userName, filesDir);

        user.Start();
    }
}