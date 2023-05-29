﻿using NetMQ.Sockets;
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

    public Client(string name)
    {
        _curVerifParams = new VerificationParams();
        _protocolData = new ClientProtocolData(name);
        _protocolData.GenerateKeys(29 * 17);
        _socket = new DealerSocket();
        _socket.Options.Identity = Encoding.UTF8.GetBytes(name);
        _socket.Connect($"{Consts.SERVER_HOST}:{Consts.PORT}");
        _socket.ReceiveReady += HandleReceivingMessage!;
        //событие ReceiveReady отрабатывает только через пуллер,
        //поэтому нужно создать его с прослушкой одного сокета - текущего
        _poller = new NetMQPoller { _socket };
    }
    public string Name => _protocolData.Name;
    private void Register()
    {
        var registerMessage = new ValuedMessage(
            _protocolData.Name, 
            Consts.SERVER_IDENTITY, 
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
            Register();
            while (true)
            {
                if (_isRegistered)
                {
                    Thread.Sleep(1000);
                    var newMessage = ParseNewMessageFromConsole();
                    var newMessageSerialized = MessagingHelper.SerializeMessage(newMessage);
                    var message = MessagingHelper.ComposeMessageToServer(newMessageSerialized);
                    MessagingHelper.TrySendMessage(_socket, message);
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
        switch (message!.Type)
        {
            case MessageType.Verification:
                //Print($"[VRound #{-1}] Request for <{-1}>...", true);
                NetMQMessage responseMessage = ComposeResponseValuedMessage(message);
                MessagingHelper.TrySendMessage(e.Socket, responseMessage);
                break;
            case MessageType.Registration:
                if (message.Frames[FramesNames.STATUS].ToString() == "Registration completed")
                {
                    _isRegistered = true;
                }
                PrintHelper.PrintMessage(message);
                break;
            case MessageType.Default:
                PrintHelper.PrintMessage(message);
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

            switch (frameName)
            {
                case FramesNames.X:
                    _curVerifParams.R = new Random().NextBigInteger(1, _protocolData.Modulo);
                    _curVerifParams.X = BigInteger.ModPow(_curVerifParams.R, 2, _protocolData.Modulo);
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
