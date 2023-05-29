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
    VerificationParams _curVerifParams;
    Task _requestingMessageTask;
    bool _isRegistered = false;
    bool _gotMoodulo = false;
    List<BigInteger> _xVerifHistory;
    string _lastSentMessageHash = "";

    public Client(string name)
    {
        _xVerifHistory = new List<BigInteger>();
        _curVerifParams = new VerificationParams();
        _protocolData = new ClientProtocolData(name);
        _socket = new DealerSocket();
        _socket.Options.Identity = Encoding.UTF8.GetBytes(name);
        _socket.Connect($"{Consts.SERVER_HOST}:{Consts.PORT}");
        _socket.ReceiveReady += HandleReceivingMessage!;
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
        var signedMessage = MessagingHelper.SignMessage(requestMessage);
        var requestMessageSerialized = MessagingHelper.SerializeMessage(signedMessage);
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

        var signedMessage = MessagingHelper.SignMessage(registerMessage);
        var registerMessageSerialized = MessagingHelper.SerializeMessage(signedMessage);
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
                    Thread.Sleep(1000);
                    _xVerifHistory.Clear(); //очистка истории X, т.к. для верификации нужны уникальные и каждая верификация уникальна на каждое сообщение
                    var newMessage = ParseNewMessageFromConsole();
                    var signedMessage = MessagingHelper.SignMessage(newMessage);
                    var newMessageSerialized = MessagingHelper.SerializeMessage(signedMessage);
                    var message = MessagingHelper.ComposeMessageToServer(newMessageSerialized);
                    var isSendingSucceed = MessagingHelper.TrySendMessage(_socket, message);
                    if (isSendingSucceed)
                    {
                        _lastSentMessageHash = signedMessage.Hash;
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
        var message = MessagingHelper.ParseSignedMessage(receivedMessage);
        PrintHelper.PrintMessage(message);
        switch (message!.Value.Type)
        {
            case MessageType.Verification:
                if (message.Hash == _lastSentMessageHash)
                {
                    var responseMessage = ComposeResponseValuedMessage(message);
                    MessagingHelper.TrySendMessage(e.Socket, responseMessage);
                }
                else
                {
                    Console.WriteLine("Got verification for message that I haven't sent!!!");
                }
                break;
            case MessageType.Modulo:
                var modulo = BigInteger.Parse(message.Value.Frames[FramesNames.MODULO].ToString()!);
                _protocolData.Modulo = modulo;
                _protocolData.GenerateKeys();
                _gotMoodulo = true;
                break;
            case MessageType.Registration:
                if (message.Value.Frames[FramesNames.STATUS].ToString() == "Registration completed")
                {
                    _isRegistered = true;
                }
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
    private NetMQMessage ComposeResponseValuedMessage(SignedMessage message)
    {
        var responseValue = new BigInteger(0);
        var responseMessage = new SignedMessage
        {
            Value = new ValuedMessage(
                _protocolData.Name,
                Consts.SERVER_IDENTITY,
                MessageType.Default),
            Hash = message.Hash,
        };
        foreach (var frame in message.Value.Frames)
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
                    _curVerifParams.E = BigInteger.Parse(message.Value.Frames[FramesNames.E].ToString()!);
                    _curVerifParams.Y = _curVerifParams.R * BigInteger.ModPow(_protocolData.PrivateKey, _curVerifParams.E, _protocolData.Modulo);
                    responseValue = _curVerifParams.Y;
                    break;
                default:
                    break;
            }
            responseMessage.Value.Frames[frameName] = responseValue;
        }
        var responseMessageSerialized = MessagingHelper.SerializeMessage(responseMessage);
        return MessagingHelper.ComposeMessageToServer(responseMessageSerialized);
    }
}
