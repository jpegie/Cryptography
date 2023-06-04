using System.Dynamic;

namespace Server;
public static class Consts
{
    public const string SERVER_IDENTITY = "Server";
    public const int VERIFICATION_ROUNDS_AMOUNT = 40;
    public const int PORT = 12346;
    public const string LOCALHOST = "tcp://localhost";
}
public static class FramesNames
{
    public const string MESSAGE = "Message";
    public const string PUBLIC_KEY = "PublicKey";
    public const string STATUS = "Status";
    public const string ROUND = "Round";
    public const string MODULO = "Modulo";
    public const string X = "x";
    public const string Y = "y";
    public const string E = "e";
    public const string Data = "Data";
    public const string Key = "Key";
    public const string Players = "Players";
    public const string Required = "Required";
}
