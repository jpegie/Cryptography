using NetMQ;
using Server;
using System.Numerics;
using Server.Helpers;
using System.ServiceModel.Channels;

namespace Bank;

internal class Bank: Client.Client
{
    const long CLIENT_STARTER_BALANCE = 100;
    const long MAX_BANKNOTE_VALUE = 21; //10101 -> 13*7*3
    /*
     * Возможная сдача:
     * 1) 1
     * 2) 4
     * 3) 16
     * 4) 17
     * 4) 20
     */
    Dictionary<string, BigInteger> _registratedUsers;
    Dictionary<string, long> _usersBalances;
    BigInteger _secretExponent;
    BigInteger _p, _q, _modulo;
    public Bank(string serverIP, BigInteger p, BigInteger q): base(Consts.BANK_IDENTITY, serverIP)
    {
        _p = p;
        _q = q;
        _modulo = _p * _q;
        _registratedUsers = new Dictionary<string, BigInteger>();
        _usersBalances = new Dictionary<string, long>();
        _secretExponent = ExponentHelper.GetExponentWithModulo(_q, _p, MAX_BANKNOTE_VALUE);
    }

    public override void Start()
    {
        _requestingMessageTask = new Task(() =>
        {
            RegisterOnServer();
        }, TaskCreationOptions.LongRunning);
        _requestingMessageTask.Start();
        _poller.RunAsync();
    }

    protected override void HandleReceivingMessage(object sender, NetMQSocketEventArgs e)
    {
        var receivedMessage = e.Socket.ReceiveMultipartMessage();
        var message = MessagingHelper.ParseValuedMessage(receivedMessage);
        PrintHelper.PrintMessage(message!);
        switch (message!.Type)
        {
            case MessageType.Registration:
                if (message.Frames[FramesNames.STATUS].ToString() == "Registration completed")
                {
                    _isRegistered = true;
                }
                break;
            case MessageType.RegistrationInBank:
                RegisterUser(message);
                break;
            case MessageType.BanknoteVerification:
                VerifyBanknote(message);
                break;
            case MessageType.BanknoteRequest:
                SignBanknote(message);
                break;
            case MessageType.BanknoteSigning:
                SignBanknote(message);
                break;
            case MessageType.Modulo:
                ResponseWithModulo(message);
                break;
        }
    }

    public void ResponseWithModulo(ValuedMessage message)
    {
        var responseMessage = message.Clone(withFrames: false);
        responseMessage.SwapSenderWithReceiver();
        responseMessage.AddFrame(FramesNames.MODULO, _modulo);
        MessagingHelper.SerializeThenSendMessageToServer(_socket, responseMessage);
    }

    public void RegisterUser(ValuedMessage message)
    {
        if (!_registratedUsers.ContainsKey(message.Sender))
        {
            _usersBalances.Add(message.Sender, CLIENT_STARTER_BALANCE);
        }
        var responseMessage = message.Clone(withFrames: false);
        responseMessage.SwapSenderWithReceiver();
        responseMessage.AddFrame(FramesNames.STATUS, "Registration completed");
        responseMessage.AddFrame(FramesNames.BALANCE, _usersBalances[message.Sender]);
        responseMessage.AddFrame(FramesNames.MAX_BANKNOTE, MAX_BANKNOTE_VALUE);
        MessagingHelper.SerializeThenSendMessageToServer(_socket, responseMessage);
    }
    /// <summary>
    /// Проверяет оригинальность банкноты
    /// </summary>
    /// <param name="message"></param>
    public void VerifyBanknote(ValuedMessage message)
    {
        var unsignedBanknote = message.Frames[FramesNames.BANKNOTE_TO_VERIFY].ToString()!;
        var isVerified = BankHelper.VerifyBanknote(message, _p, _q, _modulo);
        var responseMessage = message.Clone(withFrames: false);
        responseMessage.SwapSenderWithReceiver();
        responseMessage.AddFrame(FramesNames.BANKNOTE_TO_VERIFY, unsignedBanknote);
        responseMessage.AddFrame(FramesNames.VERIFICATION_STATUS, isVerified.ToString());
        MessagingHelper.SerializeThenSendMessageToServer(_socket, responseMessage);
    }
    /// <summary>
    /// Подписывает банкноту
    /// </summary>
    /// <param name="message"></param>
    public void SignBanknote(ValuedMessage message)
    {
        var banknoteToSign = BigInteger.Parse(message.Frames[FramesNames.BANKNOTE_TO_SIGN].ToString()!);
        var banknoteToSignValue = long.Parse(message.Frames[FramesNames.BANKNOTE_TO_SIGN_VALUE].ToString()!);
        var exponent = ExponentHelper.GetExponentWithModulo(_q, _p, banknoteToSignValue);
        var signed = BankHelper.SignBanknote(banknoteToSign, exponent, _modulo);
        var responseMessage = message.Clone(withFrames: false);
        responseMessage.SwapSenderWithReceiver();
        responseMessage.Type = MessageType.BanknoteResponse;
        responseMessage.AddFrame(FramesNames.UNSIGNED_BANKNOTE, banknoteToSign.ToString());
        responseMessage.AddFrame(FramesNames.SIGNED_BANKNOTE, signed.ToString());
        MessagingHelper.SerializeThenSendMessageToServer(_socket, responseMessage);
    }
}
