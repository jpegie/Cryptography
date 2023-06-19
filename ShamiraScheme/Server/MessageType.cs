namespace Server;
public enum MessageType
{
    Unknown = 0,
    Registration = 1,
    Verification = 2,
    Default = 3,
    Modulo = 4,
    KeyRequest = 5,
    KeyDelivery = 6,
    KeyResponse = 7,
    Encrypt = 8,
    Decrypt = 9,
    BanknoteRequest = 10,
    BanknoteResponse = 11,
    Payment = 12,
    BanknoteVerification = 13
}