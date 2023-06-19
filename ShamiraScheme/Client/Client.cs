using NetMQ.Sockets;
using NetMQ;
using Server.Extensions;
using Server;
using System.Numerics;
using System.Text;
using Newtonsoft.Json;
using Server.Helpers;

namespace Client;
public class Client
{
    protected DealerSocket _socket;
    protected NetMQPoller _poller;
    protected ClientProtocolData _protocolData;
    protected Task _requestingMessageTask;
    protected bool _isRegistered = false;
    protected bool _gotMoodulo = false;
    protected long _maxBanknoteValue = 21;
    protected List<Banknote> _banknotes;
    private bool _isRegisteredInBank = false;

    public Client(string name, string serverIP)
    {
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
    protected void RequestForModulo()
    {
        var requestMessage = new ValuedMessage(
            _protocolData.Name,
            Consts.SERVER_IDENTITY,
            MessageType.Modulo);
        var requestMessageSerialized = MessagingHelper.SerializeMessage(requestMessage);
        var messageForServer = MessagingHelper.ComposeMessageToServer(requestMessageSerialized);
        MessagingHelper.TrySendMessage(_socket, messageForServer);
    }
    protected void Register()
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

    public virtual void Start()
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
                        "\n0 - register in bank" + 
                        "\n1 - buy banknote" +
                        "\n2 - buy somenting" +
                        "\n3 - verify banknote" + 
                        "\nInput: ");
                    var inputOption = Console.ReadLine(); PrintHelper.AddNewLine();
                    Thread.Sleep(1000);
                    switch (inputOption)
                    {
                        case "0":
                            RegisterInBank();
                            break;
                        case "1":
                            BuyBanknote();
                            break;
                        case "2":
                            BuyProduct();
                            break;
                        case "3":
                            VerifyBanknote();
                            break;
                    }
                    Thread.Sleep(1000);
                }
            }
        }, TaskCreationOptions.LongRunning);
        _requestingMessageTask.Start();
        _poller.RunAsync();
    }
    private void RegisterInBank()
    {
        if (_isRegisteredInBank)
        {
            PrintHelper.Print("You are already registered in bank!", true);
            return;
        }
        var registrationMessage = new ValuedMessage(_protocolData.Name, Consts.BANK_IDENTITY, MessageType.RegistrationInBank);
        MessagingHelper.SerializeThenSendMessageToServer(_socket, registrationMessage);
    }
    private void BuyBanknote()
    {
        var banknote = BankHelper.ComposeBanknote(
                                _protocolData.Modulo,
                                ExponentHelper.ComposeExponentFromValue(_maxBanknoteValue),
                                _maxBanknoteValue);

        _banknotes.Add(banknote); //TODO: добавить какой-нибудь BanknotesProvider

        var requestMessage = new ValuedMessage(_protocolData.Name, Consts.BANK_IDENTITY, MessageType.BanknoteRequest);
        requestMessage.AddFrame(FramesNames.BANKNOTE_TO_SIGN, banknote.SMultR);
        requestMessage.AddFrame(FramesNames.BANKNOTE_TO_SIGN_VALUE, _maxBanknoteValue);
        MessagingHelper.SerializeThenSendMessageToServer(_socket, requestMessage);
    }
    private void BuyProduct()
    {
        PrintHelper.Print("Seller: ", false);
        var seller = Console.ReadLine()!;
        PrintHelper.Print("Using banknote index: ", false);
        var banknoteIndex = int.Parse(Console.ReadLine()!);
        PrintHelper.Print("Cost: ", false);
        var cost = long.Parse(Console.ReadLine()!);
        var change = _maxBanknoteValue - cost;
        var requestMessage = new ValuedMessage(_protocolData.Name, seller, MessageType.Payment);

        var signedCost = BigInteger.Pow
        (
            BigInteger.Parse(_banknotes.ElementAt(banknoteIndex).Sign), //s_1 ^ (1/h)
            ExponentHelper.ComposeExponentFromValue(change) //s_1 ^ ((1/h) * change)
        ); //первая часть составного сообщения (ее забирает себе продавец)

        _banknotes.RemoveAt(banknoteIndex); //использованную банкноту удаляю

        var changeUnsigned = BankHelper.ComposeBanknote
        (
            _protocolData.Modulo,
            ExponentHelper.ComposeExponentFromValue(change),
            change
         ); //вторая часть составного сообщения (ее продавец оправляет банку на подпись и потом отправляет покупателю в качестве сдачи)

        _banknotes.Add(changeUnsigned); //сразу надо добавить в свою коллекцию банкнот

        requestMessage.AddFrame(FramesNames.COST_SIGNED, signedCost.ToString());
        requestMessage.AddFrame(FramesNames.COST_VALUE, cost);
        requestMessage.AddFrame(FramesNames.BANKNOTE_TO_SIGN, changeUnsigned.SMultR);
        requestMessage.AddFrame(FramesNames.BANKNOTE_TO_SIGN_VALUE, change);

        MessagingHelper.SerializeThenSendMessageToServer(_socket, requestMessage);
    }
    private void VerifyBanknote()
    {
        PrintHelper.Print("Banknote index to verify: ", false);
        var index = int.Parse(Console.ReadLine()!);
        var banknoteToVerify = _banknotes[index];
        if (!banknoteToVerify.IsVerified)
        {
            var requestMessage = new ValuedMessage(_protocolData.Name, Consts.BANK_IDENTITY, MessageType.BanknoteVerification);
            requestMessage.AddFrame(FramesNames.BANKNOTE_TO_VERIFY, banknoteToVerify.SMultR);
            requestMessage.AddFrame(FramesNames.SIGNED_BANKNOTE, ((BigInteger.Parse(banknoteToVerify.Sign) * BigInteger.Parse(banknoteToVerify.R)) % _protocolData.Modulo).ToString()); //верифицировать нужно с затемняющим множителем, т.к. банк сначала избавляется от него и потом подписывает S
            requestMessage.AddFrame(FramesNames.BANKNOTE_VALUE, banknoteToVerify.Value);
            MessagingHelper.SerializeThenSendMessageToServer(_socket, requestMessage);
        }
        else
        {
            PrintHelper.Print("Banknote already verified!", true);
        }
    }
    protected virtual void HandleReceivingMessage(object sender, NetMQSocketEventArgs e)
    {
        var receivedMessage = e.Socket.ReceiveMultipartMessage();
        var message = MessagingHelper.ParseValuedMessage(receivedMessage);
        PrintHelper.PrintMessage(message!);
        NetMQMessage responseMessage;
        switch (message!.Type)
        {
            case MessageType.RegistrationInBank:
                _maxBanknoteValue = long.Parse(message.Frames[FramesNames.MAX_BANKNOTE].ToString()!);
                _isRegistered = true;
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
                break;
            case MessageType.BanknoteResponse:
                if (!message.Frames.ContainsKey(FramesNames.SIGNED_BANKNOTE))
                {
                    PrintHelper.Print("MoneyResponse message doesn't include new banknote", true);
                }
                else
                {
                    var unsignedBanknote = message.Frames[FramesNames.UNSIGNED_BANKNOTE].ToString()!;
                    var rawBanknote = message.Frames[FramesNames.SIGNED_BANKNOTE].ToString()!; //все еще с затемняющим множителем
                    var activeBanknote = _banknotes.Find(b => b.SMultR == unsignedBanknote)!;
                    activeBanknote.Sign = (ModMath.ModDivision(BigInteger.Parse(rawBanknote), BigInteger.Parse(activeBanknote.R), _protocolData.Modulo) % _protocolData.Modulo).ToString(); //избавляюсь от затемняющего множителя
                    PrintHelper.PrintBanknotes(_banknotes);
                }
                break;
            case MessageType.Payment:
                if (!message.Frames.ContainsKey(FramesNames.COST_SIGNED))
                {
                    PrintHelper.Print("Payment message doesn't include signed cost", true);
                }
                else
                {
                    var banknoteSerialNumber = message.Frames[FramesNames.COST_SIGNED].ToString()!; //полностью валидный серийный номер купюры
                    var banknoteValue = long.Parse(message.Frames[FramesNames.COST_VALUE].ToString()!);
                    var banknote = new Banknote
                    {
                        Sign = banknoteSerialNumber,
                        Value = banknoteValue,
                        IsVerified = true
                    };
                    _banknotes.Add(banknote);
                    PrintHelper.PrintBanknotes(_banknotes);

                    //покупатель
                    var buyer = message.Sender;
                    //формирование запроса к банку на подпись сдачи
                    var changeUnsigned = message.Frames[FramesNames.BANKNOTE_TO_SIGN];
                    var changeValue = message.Frames[FramesNames.BANKNOTE_TO_SIGN_VALUE];
                    var requestToSign = new ValuedMessage(_protocolData.Name, Consts.BANK_IDENTITY, MessageType.BanknoteSigning);
                    requestToSign.AddFrame(FramesNames.BANKNOTE_TO_SIGN, changeUnsigned);
                    requestToSign.AddFrame(FramesNames.BANKNOTE_TO_SIGN_VALUE, changeValue);
                    MessagingHelper.SerializeThenSendMessageToServer(_socket, requestToSign);
                    //на время подписи банкноты забираю полностью обработку сообщений
                    var signedBanknoteMessage = MessagingHelper.ParseValuedMessage(_socket.ReceiveMultipartMessage())!;
                    signedBanknoteMessage.Type = MessageType.BanknoteResponse;
                    signedBanknoteMessage.Sender = _protocolData.Name;
                    signedBanknoteMessage.Receiver = buyer;
                    //полученную подписанную сдачу отправляю покупателю
                    MessagingHelper.SerializeThenSendMessageToServer(_socket, signedBanknoteMessage);
                }
                break;
            case MessageType.BanknoteVerification:
                var unsignedValueString = (string)message.Frames[FramesNames.BANKNOTE_TO_VERIFY];
                var verifBanknote = _banknotes.Find(b => b.SMultR == unsignedValueString)!;
                var status = bool.Parse((string)message.Frames[FramesNames.VERIFICATION_STATUS]);
                verifBanknote.IsVerified = status;
                break;
            case MessageType.Default:
                break;
        }  
    }
}
