namespace Client;
class ClientEntryPoint
{
    static void Main(string[] args)
    {
        Console.Write("Username: ");
        var userName = Console.ReadLine()!;
        Console.Write("Directory for saving files: ");
        var filesDir = Console.ReadLine()!;
        var user = new Client(userName, filesDir);

        user.Start();
    }
}