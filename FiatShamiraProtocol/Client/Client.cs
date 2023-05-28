using NetMQ.Sockets;
using NetMQ;
using Server.Extensions;
using Server;
using System.Numerics;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Security.Cryptography.Xml;

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
        var registerMessage = MessagingHelper.ComposeMessage(
           ReceivedMessage.SERVER_NAME,   //получатель
           ReceivedMessage.REGISTRATION,  //сообщение
           _protocolData.PublicKey.ToString());
        MessagingHelper.TrySendMessage(_socket, registerMessage);
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
                    var message = MessagingHelper.ComposeMessageToServer(newMessage);
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
        var msg = e.Socket.ReceiveMultipartMessage();
        if (VerificationMessage.IsVerificationMessage(msg))
        {
            var receivedMsg = new VerificationMessage(msg);
            Print($"[VRound #{receivedMsg.VerificationRoundInt}] Request for <{receivedMsg.RequestedValue}>...", true);
            NetMQMessage responseMessage = ComposeResponseValuedMessage(receivedMsg);
            MessagingHelper.TrySendMessage(e.Socket, responseMessage);
        }
        else
        {
            var receivedMsg = new ReceivedMessage(msg);
            PrintMessage(receivedMsg);
            if (receivedMsg.Message == "Registration completed!")
            {
                _isRegistered = true;
            }
        }
    }
    private string ParseNewMessageFromConsole()
    {
        var msg = "";
        Print("Message: ", false);
        msg = Console.ReadLine()!;
        Print("To: ", false);
        msg += ";" + Console.ReadLine()!;
        return msg;
    }
    private NetMQMessage ComposeResponseValuedMessage(VerificationMessage receivedMsg)
    {
        var requestedValue = receivedMsg.RequestedValue.Split(";");
        var requestedValueName = requestedValue[0];
        var requestedValueParam = requestedValue.Length == 2 ? requestedValue[1] : null;
        var responseValue = new BigInteger(0);
        switch (requestedValueName)
        {
            case "x":
                _curVerifParams.R = new Random().NextBigInteger(1, _protocolData.Modulo);
                _curVerifParams.X = BigInteger.ModPow(_curVerifParams.R, 2, _protocolData.Modulo);
                responseValue = _curVerifParams.X;
                break;
            case "y":
                _curVerifParams.E = BigInteger.Parse(requestedValueParam!);
                _curVerifParams.Y = _curVerifParams.R * BigInteger.ModPow(_protocolData.PrivateKey, _curVerifParams.E, _protocolData.Modulo);
                responseValue = _curVerifParams.Y;
                break;
            default:
                break;
        }
        return MessagingHelper.ComposeMessageToServer(
            "Value",
            requestedValueName,
            responseValue.ToString()); //сообщение)
    }
    object printLock = new object();
    void PrintMessage(ReceivedMessage message)
    {
        Print($"{message.Sender}: {message.Message}", true);
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
