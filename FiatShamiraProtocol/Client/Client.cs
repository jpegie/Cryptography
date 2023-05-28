using NetMQ.Sockets;
using NetMQ;
using Server.Extensions;
using Server;
using System.Numerics;
using System.Text;
using Newtonsoft.Json;

namespace Client;
internal class Client
{
    DealerSocket _socket;
    NetMQPoller _poller;
    ClientProtocolData _protocolData;
    VerificationParams _curVerifParams;
    Task _requestingMessageTask;
    bool _isRegistered = false;

    public Client(string name, int port = 12346)
    {
        _curVerifParams = new VerificationParams();
        _protocolData = new ClientProtocolData(name);
        _protocolData.GenerateKeys(29 * 17);
        _socket = new DealerSocket();
        _socket.Options.Identity = Encoding.UTF8.GetBytes(name);
        _socket.Connect($"tcp://127.0.0.1:{port}");
        _socket.ReceiveReady += HandleReceivingMessage!;
        //событие ReceiveReady отрабатывает только через пуллер,
        //поэтому нужно создать его с прослушкой одного сокета - текущего
        _poller = new NetMQPoller { _socket };
    }
    public string Name => _protocolData.Name;
    private void Register()
    {
        var registerMessage = new ValuedMessage(_protocolData.Name, "Server", MessageType.Registration);
        registerMessage.AddFrame("PublicKey", _protocolData.PublicKey);

        var registerMessageSerialized = JsonConvert.SerializeObject(registerMessage);
        var messageForServer = MessagingHelper.ComposeMessageToServer(registerMessageSerialized);
        MessagingHelper.TrySendMessage(_socket, messageForServer);
    }

    public void Start()
    {
        _requestingMessageTask = new Task(() =>
        {
            Register();
            while (true)
            {
                if (_isRegistered)
                {
                    var newMessage = ParseNewMessageFromConsole();
                    var newMessageSerialized = JsonConvert.SerializeObject(newMessage);
                    var message = MessagingHelper.ComposeMessageToServer(newMessageSerialized);
                    MessagingHelper.TrySendMessage(_socket, message);
                    Thread.Sleep(2000);
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
        switch (message!.Type)
        {
            case MessageType.Verification:
                Print($"[VRound #{-1}] Request for <{-1}>...", true);
                NetMQMessage responseMessage = ComposeResponseValuedMessage(message);
                MessagingHelper.TrySendMessage(e.Socket, responseMessage);
                break;
            case MessageType.Registration:
                if (message.Frames["Status"].ToString() == "Registration completed")
                {
                    _isRegistered = true;
                }
                PrintMessage(message);
                break;
            case MessageType.Default:
                PrintMessage(message);
                break;
        }
    }
    private ValuedMessage ParseNewMessageFromConsole()
    {
        var msg = "";
        Print("Message: ", false);
        var text = Console.ReadLine()!;
        Print("To: ", false);
        var receiver = Console.ReadLine()!;
        var message = new ValuedMessage (_protocolData.Name, receiver, MessageType.Default);
        return message;
    }
    private NetMQMessage ComposeResponseValuedMessage(ValuedMessage message)
    {
        var responseValue = new BigInteger(0);
        var frameName = message.Frames.First().Key;
        switch (frameName)
        {
            case "x":
                _curVerifParams.R = new Random().NextBigInteger(1, _protocolData.Modulo);
                _curVerifParams.X = BigInteger.ModPow(_curVerifParams.R, 2, _protocolData.Modulo);
                responseValue = _curVerifParams.X;
                break;
            case "y":
                _curVerifParams.E = (BigInteger)message.Frames["e"];
                _curVerifParams.Y = _curVerifParams.R * BigInteger.ModPow(_protocolData.PrivateKey, _curVerifParams.E, _protocolData.Modulo);
                responseValue = _curVerifParams.Y;
                break;
            default:
                break;
        }
        var reponseMessage = new ValuedMessage(_protocolData.Name, "Server", MessageType.Default);
        reponseMessage.Frames.Add(frameName, responseValue);

        var responseMessageSerialized = JsonConvert.SerializeObject(reponseMessage);
        return MessagingHelper.ComposeMessageToServer(responseMessageSerialized);
    }
    object printLock = new object();
    void PrintMessage(ValuedMessage message)
    {
        foreach(var frame in message.Frames)
        {
            Print($"{message.Sender}: {frame.Key} = {frame.Value}", true);
        }
        
    }
    void Print(string message, bool addNewLine)
    {
        lock (printLock)
        {
            //"очистка" текущей строки
            Console.SetCursorPosition(0, Console.CursorTop);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, Console.CursorTop);
            if (addNewLine)
            {
                Console.WriteLine(message);
            }
            else
            {
                Console.Write(message);
            }
        }
    }
}
