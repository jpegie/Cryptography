namespace Client;
class ClientEntryPoint
{
    static void Main(string[] args)
    {
        Console.Write("Username: ");
        var user = new Client(Console.ReadLine()!);
        user.Start();
    }
}