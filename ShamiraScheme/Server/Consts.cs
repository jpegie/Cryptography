using Newtonsoft.Json.Serialization;
using System.Dynamic;

namespace Server;
public static class Consts
{
    public const string BANK_IDENTITY = "Bank";
    public const string SERVER_IDENTITY = "Server";
    public const int VERIFICATION_ROUNDS_AMOUNT = 40;
    public const int PORT = 12346;
    public const string LOCALHOST = "tcp://localhost";
    public const string ALL_HOST = "tcp://*";
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
    public const string BALANCE = "Balance";
    public const string MAX_BANKNOTE = "MaxBanknote";
    public const string BANKNOTE = "Banknote";
    public const string BANKNOTE_VALUE = "BanknoteValue";
    public const string UNSIGNED_BANKNOTE = "BanknoteToSign";
    public const string BANKNOTE_TO_VERIFY = "BanknoteToVerify";
    public const string SIGNED_BANKNOTE = "SignedBanknote";
    public const string COST_SIGNED = "Cost";
    public const string COST_VALUE = "CostValue";
    public const string BANKNOTE_TO_SIGN = "Change";
    public const string BANKNOTE_TO_SIGN_VALUE = "ChangeValue";
    public const string VERIFICATION_STATUS = "VerificationStatus";
}
