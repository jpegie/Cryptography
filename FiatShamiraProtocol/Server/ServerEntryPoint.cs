namespace Server;
class ServerEntryPoint
{
    public static void Main(string[] args)
    {
        var server = new TrustedServer();
        server.StartReceiving();
    }  
}