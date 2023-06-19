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
    DealerSocket _socket;
    NetMQPoller _poller;
    ClientProtocolData _protocolData;
    Task _requestingMessageTask;
    bool _isRegistered = false;
    bool _gotMoodulo = false;
    long _maxBanknoteValue = 21;
    List<Banknote> _banknotes;

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
                        "\n1 - buy banknote" +
                        "\n2 - buy somenting" +
                        "\n3 - verify banknote" + 
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
                        case "2":
                            PrintHelper.Print("Seller: ", false);
                            var seller = Console.ReadLine()!;
                            PrintHelper.Print("Using banknote index: ", false);
                            var banknoteIndex = int.Parse(Console.ReadLine()!);
                            PrintHelper.Print("Cost: ", false);
                            var cost = long.Parse(Console.ReadLine()!);
                            var change = _maxBanknoteValue - cost;
                            newMessage = new ValuedMessage(_protocolData.Name, seller, MessageType.Payment);

                            var realCost = BigInteger.Pow
                            (
                                BigInteger.Parse(_banknotes.ElementAt(banknoteIndex).Sign), //s_1 ^ (1/h)
                                ExponentHelper.ComposeExponentFromValue(change) //s_1 ^ ((1/h) * change)
                            ); //первая часть составного сообщения
                           
                            _banknotes.RemoveAt(banknoteIndex); //использованную банкноту удаляю

                            var changeBanknote = BankHelper.ComposeBanknote
                            (
                                _protocolData.Modulo,
                                ExponentHelper.ComposeExponentFromValue(change),
                                change
                             ); //вторая часть составного сообщения
                           
                            _banknotes.Add(changeBanknote); //сразу надо добавить в свою коллекцию банкнот

                            newMessage.AddFrame(FramesNames.COST, realCost.ToString());
                            newMessage.AddFrame(FramesNames.COST_VALUE, cost);
                            newMessage.AddFrame(FramesNames.CHANGE_TO_SIGN, changeBanknote.SMultR);
                            newMessage.AddFrame(FramesNames.CHANGE_VALUE, change);
                            newMessageSerialized = MessagingHelper.SerializeMessage(newMessage);
                            message = MessagingHelper.ComposeMessageToServer(newMessageSerialized);
                            MessagingHelper.TrySendMessage(_socket, message);
                            break;
                        case "3":
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
                    PrintHelper.Print("Payment message doesn't include new banknote", true);
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
                var verifBanknote = _banknotes.Find(b => b.SMultR == unsignedValueString)!;
                var status = bool.Parse((string)message.Frames[FramesNames.VERIFICATION_STATUS]);
                verifBanknote.IsVerified = status;
                break;
            case MessageType.Default:
                break;
        }  
    }
}
