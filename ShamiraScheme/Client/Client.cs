using NetMQ.Sockets;
using NetMQ;
using Server.Extensions;
using Server;
using System.Numerics;
using System.Text;
using Newtonsoft.Json;
using Server.Helpers;

namespace Client;
internal class Client
{
    const string EXTENSION = ".data";
    const string ENCRYPTED_FILENAME = "encrypted" + EXTENSION;
    const string DECRYPTED_FILENAME = "decrypted" + EXTENSION;

    string _filesDirectory = "";

    private object _mySavedKey;

    DealerSocket _socket;
    NetMQPoller _poller;
    ClientProtocolData _protocolData;
    VerificationParams _curVerifParams;
    Task _requestingMessageTask;
    bool _isRegistered = false;
    bool _gotMoodulo = false;
    long _maxBanknoteValue = 21;

    List<BigInteger> _xVerifHistory;
    List<Banknote> _banknotes; //s, r: value
    public Client(string name, string filesDirectory, string serverIP)
    {
        if (filesDirectory == "")
        {
            _filesDirectory = Directory.GetCurrentDirectory();
        }
        else
        {
            _filesDirectory = filesDirectory;
        }
        _xVerifHistory = new List<BigInteger>();
        _curVerifParams = new VerificationParams();
        _protocolData = new ClientProtocolData(name);
        _socket = new DealerSocket();
        _socket.Options.Identity = Encoding.UTF8.GetBytes(name);
        _socket.Connect($"{serverIP}:{Consts.PORT}");
        _socket.ReceiveReady += HandleReceivingMessage!;
        _banknotes = new List<Banknote>();  
        //событие ReceiveReady отрабатывает только через пуллер,
        //поэтому нужно создать его с прослушкой одного сокета - текущего
        _poller = new NetMQPoller { _socket };
    }
    public string Name => _protocolData.Name;
    private void RequestForModulo()
    {
        var requestMessage = new ValuedMessage(
            _protocolData.Name,
            Consts.SERVER_IDENTITY,
            MessageType.Modulo);
        var requestMessageSerialized = MessagingHelper.SerializeMessage(requestMessage);
        var messageForServer = MessagingHelper.ComposeMessageToServer(requestMessageSerialized);
        MessagingHelper.TrySendMessage(_socket, messageForServer);
    }
    private void Register()
    {
        var registerMessage = new ValuedMessage(
            _protocolData.Name, 
            Consts.SERVER_IDENTITY, //кому сообщение
            MessageType.Registration);
        registerMessage.AddFrame(FramesNames.PUBLIC_KEY, _protocolData.PublicKey);

        var registerMessageSerialized = MessagingHelper.SerializeMessage(registerMessage);
        var messageForServer = MessagingHelper.ComposeMessageToServer(registerMessageSerialized);
        MessagingHelper.TrySendMessage(_socket, messageForServer);
    }

    public void Start()
    {
        _requestingMessageTask = new Task(() =>
        {
            RequestForModulo();
            while (!_gotMoodulo) { } //кручусь пока не получу модуль от сервера для дальнейшей регистрации
            Register();
            while (true)
            {
                if (_isRegistered)
                {
                    Console.Write(
                        "\nOptions: " +
                        "\n1 - send message" +
                        "\n2 - send message to encrypt" +
                        "\n3 - send message to decrypt" +
                        "\n4 - buy banknote" +
                        "\n5 - buy a product" +
                        "\n6 - verify banknote" + 
                        "\nInput: ");
                    var inputOption = Console.ReadLine(); PrintHelper.AddNewLine();
                    var newMessageSerialized = "";
                    var filePath = "";
                    ValuedMessage newMessage;
                    NetMQMessage message;

                    Thread.Sleep(1000);
                    switch (inputOption)
                    {
                        case "1":
                            newMessage = ParseNewMessageFromConsole();
                            newMessageSerialized = MessagingHelper.SerializeMessage(newMessage);
                            message = MessagingHelper.ComposeMessageToServer(newMessageSerialized);
                            MessagingHelper.TrySendMessage(_socket, message);
                            break;
                        case "2":
                            PrintHelper.Print("File path to encrypt: ", false);
                            filePath = Console.ReadLine()!;
                            newMessage = ParseMessageForEncryptFromFile(filePath);
                            newMessageSerialized = MessagingHelper.SerializeMessage(newMessage);
                            message = MessagingHelper.ComposeMessageToServer(newMessageSerialized);
                            MessagingHelper.TrySendMessage(_socket, message);
                            break;
                        case "3":
                            PrintHelper.Print("File path to decrypt: ", false);
                            filePath = Console.ReadLine()!;
                            newMessage = ParseMessageForDecryptFromFile(filePath);
                            newMessageSerialized = MessagingHelper.SerializeMessage(newMessage);
                            message = MessagingHelper.ComposeMessageToServer(newMessageSerialized);
                            MessagingHelper.TrySendMessage(_socket, message);
                            break;
                        case "4":
                            var banknote = BankHelper.ComposeBanknote(
                                _protocolData.Modulo, 
                                ExponentHelper.ComposeExponentFromValue(_maxBanknoteValue),
                                _maxBanknoteValue);

                            _banknotes.Add(banknote); //TODO: добавить какой-нибудь BanknotesProvider

                            newMessage = new ValuedMessage(_protocolData.Name, Consts.SERVER_IDENTITY, MessageType.BanknoteRequest);
                            newMessage.AddFrame(FramesNames.UNSIGNED_BANKNOTE, banknote.SMultR);
                            newMessageSerialized = MessagingHelper.SerializeMessage(newMessage);
                            message = MessagingHelper.ComposeMessageToServer(newMessageSerialized);
                            MessagingHelper.TrySendMessage(_socket, message);
                            break;
                        case "5":
                            PrintHelper.Print("Seller: ", false);
                            var seller = Console.ReadLine()!;
                            PrintHelper.Print("Using banknote index: ", false);
                            var banknoteIndex = int.Parse(Console.ReadLine()!);
                            PrintHelper.Print("Cost: ", false);
                            var cost = BigInteger.Parse(Console.ReadLine()!);
                            var change = _maxBanknoteValue - cost;
                            newMessage = new ValuedMessage(_protocolData.Name, seller, MessageType.Payment);

                            var realCost = BigInteger
                            .Pow
                            (
                                BigInteger.Parse(_banknotes.ElementAt(banknoteIndex).Sign), //s_1 ^ (1/h)
                                ExponentHelper.ComposeExponentFromValue((long)(change)) //s_1 ^ ((1/h) * change)
                            ); //первая часть составного сообщения
                            _banknotes.RemoveAt(banknoteIndex); //использованную банкноту удаляю

                            var changeBanknote = BankHelper.ComposeBanknote(
                                _protocolData.Modulo,
                                ExponentHelper.ComposeExponentFromValue((long)change),
                                (long)change); //вторая часть составного сообщения
                            _banknotes.Add(changeBanknote); //сразу надо добавить в свою коллекцию банкнот

                            newMessage.AddFrame(FramesNames.COST, realCost.ToString());
                            newMessage.AddFrame(FramesNames.COST_VALUE, cost);
                            newMessage.AddFrame(FramesNames.CHANGE_TO_SIGN, changeBanknote.SMultR);
                            newMessage.AddFrame(FramesNames.CHANGE_VALUE, change);
                            newMessageSerialized = MessagingHelper.SerializeMessage(newMessage);
                            message = MessagingHelper.ComposeMessageToServer(newMessageSerialized);
                            MessagingHelper.TrySendMessage(_socket, message);
                            break;
                        case "6":
                            PrintHelper.Print("Banknote index to verify: ", false);
                            var index = int.Parse(Console.ReadLine()!);
                            var banknoteToVerify = _banknotes[index];
                            if (!banknoteToVerify.IsVerified)
                            {
                                newMessage = new ValuedMessage(_protocolData.Name, Consts.SERVER_IDENTITY, MessageType.BanknoteVerification);
                                newMessage.AddFrame(FramesNames.BANKNOTE_TO_VERIFY, banknoteToVerify.SMultR);
                                newMessage.AddFrame(FramesNames.SIGNED_BANKNOTE, ((BigInteger.Parse(banknoteToVerify.Sign) * BigInteger.Parse(banknoteToVerify.R)) % _protocolData.Modulo).ToString()); //верифицировать нужно с затемняющим множителем, т.к. банк сначала избавляется от него и потом подписывает S
                                newMessage.AddFrame(FramesNames.BANKNOTE_VALUE, banknoteToVerify.Value);
                                newMessageSerialized = MessagingHelper.SerializeMessage(newMessage);
                                message = MessagingHelper.ComposeMessageToServer(newMessageSerialized);
                                MessagingHelper.TrySendMessage(_socket, message);
                            }
                            else
                            {
                                PrintHelper.Print("Banknote already verified!", true);
                            }
                            break;
                    }
                    Thread.Sleep(1000);
                }
            }
        }, TaskCreationOptions.LongRunning);
        _requestingMessageTask.Start();
        _poller.RunAsync();
    }

    void HandleReceivingMessage(object sender, NetMQSocketEventArgs e)
    {
        var receivedMessage = e.Socket.ReceiveMultipartMessage();
        var message = MessagingHelper.ParseValuedMessage(receivedMessage);
        PrintHelper.PrintMessage(message!);
        NetMQMessage responseMessage;
        switch (message!.Type)
        {
            case MessageType.KeyRequest:
                responseMessage = ComposeKeyMessage(message);
                MessagingHelper.TrySendMessage(e.Socket, responseMessage);
                break;
            case MessageType.Verification:
                responseMessage = ComposeResponseValuedMessage(message);
                MessagingHelper.TrySendMessage(e.Socket, responseMessage);
                break;
            case MessageType.Modulo:
                var modulo = BigInteger.Parse(message.Frames[FramesNames.MODULO].ToString()!);
                _protocolData.Modulo = modulo;
                _protocolData.GenerateKeys();
                _gotMoodulo = true;
                break;
            case MessageType.Registration:
                if (message.Frames[FramesNames.STATUS].ToString() == "Registration completed")
                {
                    _isRegistered = true;
                }
                _maxBanknoteValue = (long)message.Frames[FramesNames.MAX_BANKNOTE];
                break;
            case MessageType.KeyDelivery:
                _mySavedKey = message.Frames[FramesNames.Key].ToString()!;
                break;
            case MessageType.Encrypt:
                var encryptedDataValue = Convert.FromHexString(message.Frames[FramesNames.Data].ToString()!);
                FilesHelper.WriteData(ENCRYPTED_FILENAME, _filesDirectory, encryptedDataValue);
                break;
            case MessageType.Decrypt:
                var decryptedDataValue = Convert.FromHexString(message.Frames[FramesNames.Data].ToString()!);
                FilesHelper.WriteData(DECRYPTED_FILENAME, _filesDirectory, decryptedDataValue, asHex: false);
                ExecutionHelper.Execute(Path.Combine(_filesDirectory, DECRYPTED_FILENAME));
                break;
            case MessageType.BanknoteResponse:
                if (!message.Frames.ContainsKey(FramesNames.SIGNED_BANKNOTE))
                {
                    PrintHelper.Print("MoneyResponse message doesn't include new banknote", true);
                }
                else
                {
                    var unsignedBanknote = (string)message.Frames[FramesNames.UNSIGNED_BANKNOTE];
                    var rawBanknote = (string)message.Frames[FramesNames.SIGNED_BANKNOTE]; //все еще с затемняющим множителем
                    var activeBanknote = _banknotes.Find(b => b.SMultR == unsignedBanknote)!;
                    activeBanknote.Sign = (ModMath.ModDivision(BigInteger.Parse(rawBanknote), BigInteger.Parse(activeBanknote.R), _protocolData.Modulo) % _protocolData.Modulo).ToString(); //избавляюсь от затемняющего множителя
                    PrintHelper.PrintBanknotes(_banknotes);
                }
                break;
            case MessageType.Payment:
                if (!message.Frames.ContainsKey(FramesNames.SIGNED_BANKNOTE))
                {
                    PrintHelper.Print("MoneyResponse message doesn't include new banknote", true);
                }
                else
                {
                    var banknoteSerialNumber = (string)message.Frames[FramesNames.SIGNED_BANKNOTE]; //полностью валидный серийный номер купюры
                    var banknoteValue = (long)message.Frames[FramesNames.BANKNOTE_VALUE];
                    var banknote = new Banknote
                    {
                        Sign = banknoteSerialNumber,
                        Value = banknoteValue,
                        IsVerified = true
                    };
                    _banknotes.Add(banknote);
                    PrintHelper.PrintBanknotes(_banknotes);
                }
                break;
            case MessageType.BanknoteVerification:
                var unsignedValueString = (string)message.Frames[FramesNames.BANKNOTE_TO_VERIFY];
                var verifBanknote = _banknotes.Find(b => b.SMultR == unsignedValueString);
                var status = bool.Parse((string)message.Frames[FramesNames.VERIFICATION_STATUS]);
                verifBanknote.IsVerified = status;
                break;
            case MessageType.Default:
                break;
        }  
    }
    private ValuedMessage ParseNewMessageFromConsole()
    {
        PrintHelper.Print("Message: ", false);
        var text = Console.ReadLine()!;
        PrintHelper.Print("To: ", false);
        var receiver = Console.ReadLine()!;
        var message = new ValuedMessage (_protocolData.Name, receiver, MessageType.Default);
        message.UpdateMessageFrame(text);
        return message;
    }
    private ValuedMessage ParseMessageForEncryptFromFile(string filePath)
    {
        var totalUsers = "";
        var requiredUsers = "";
        PrintHelper.Print("Enter encryption params -> ", true);
        PrintHelper.Print("Total users: ", false); 
        totalUsers = Console.ReadLine()!;
        PrintHelper.Print("Required users to decrypt: ", false); 
        requiredUsers = Console.ReadLine()!;

        int total = int.Parse(totalUsers);
        int required = int.Parse(requiredUsers);

        var data = Convert.ToHexString(FilesHelper.ReadFile(filePath));
        var message = new ValuedMessage(_protocolData.Name, Consts.SERVER_IDENTITY, MessageType.Encrypt);
        message.AddFrame(FramesNames.Data, data);
        message.AddFrame(FramesNames.Players, total);
        message.AddFrame(FramesNames.Required, required);
        return message;
    }
    private ValuedMessage ParseMessageForDecryptFromFile(string filePath)
    {
        var data = Convert.ToHexString(FilesHelper.ReadFile(filePath, asHex: true));
        var message = new ValuedMessage(_protocolData.Name, Consts.SERVER_IDENTITY, MessageType.Decrypt);
        message.AddFrame(FramesNames.Data, data);
        return message;
    }

    private NetMQMessage ComposeKeyMessage(ValuedMessage message)
    {
        var reponseMessage = new ValuedMessage(
            _protocolData.Name,
            Consts.SERVER_IDENTITY,
            MessageType.KeyResponse);

        reponseMessage.Frames[FramesNames.Key] = _mySavedKey;

        var responseMessageSerialized = MessagingHelper.SerializeMessage(reponseMessage);
        return MessagingHelper.ComposeMessageToServer(responseMessageSerialized);
    }

    private NetMQMessage ComposeResponseValuedMessage(ValuedMessage message)
    {
        var responseValue = new BigInteger(0);
        var reponseMessage = new ValuedMessage(
            _protocolData.Name, 
            Consts.SERVER_IDENTITY, 
            MessageType.Default);
        foreach (var frame in message.Frames)
        {
            var frameName = frame.Key;
            var xFound = false;
            switch (frameName)
            {
                case FramesNames.X:
                    while (!xFound)
                    {
                        _curVerifParams.R = new Random().NextBigInteger(1, _protocolData.Modulo);
                        _curVerifParams.X = BigInteger.ModPow(_curVerifParams.R, 2, _protocolData.Modulo);
                        if (!_xVerifHistory.Contains(_curVerifParams.X))
                        {
                            xFound = true;
                            _xVerifHistory.Add(_curVerifParams.X);
                        }
                    }
                    responseValue = _curVerifParams.X;
                    break;
                case FramesNames.Y:
                    _curVerifParams.E = BigInteger.Parse(message.Frames[FramesNames.E].ToString()!);
                    _curVerifParams.Y = _curVerifParams.R * BigInteger.ModPow(_protocolData.PrivateKey, _curVerifParams.E, _protocolData.Modulo);
                    responseValue = _curVerifParams.Y;
                    break;
                default:
                    break;
            }
            reponseMessage.Frames[frameName] = responseValue;
        }
        var responseMessageSerialized = MessagingHelper.SerializeMessage(reponseMessage);
        return MessagingHelper.ComposeMessageToServer(responseMessageSerialized);
    }
}
