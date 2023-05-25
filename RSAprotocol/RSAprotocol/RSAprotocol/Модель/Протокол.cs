using Newtonsoft.Json;
using System.Numerics;
using System.Text;

public class RSAprotocol
{
    List<BigInteger> _messages;
    RSAkeys _keys;
    PublicKey _chatterPublicKey;

    string _username = "defaultuser";
    public RSAprotocol(BigInteger p, BigInteger q, string username)
    {
        _messages = new List<BigInteger>();
        _keys = new RSAkeys(p, q);
        _username = username;
        FilesProvider.SaveFile(JsonConvert.SerializeObject(_keys.PublicKey), $"{username}_открытыйКлюч.txt");
        ParseChatterPublicKey();
    }

    public void SendMessage()
    {
        ParseChatterPublicKey();
        Console.Write("Введите сообщение: ");
        var messageStr = Console.ReadLine(); 
        var messageBytes = Encoding.UTF8.GetBytes(messageStr!);  
        //изначально сообщение создается незашифрованное + рядышком кладется подпись
        var msg = new Message()
        {
            ID = GetNewMessageID(),
            Sender = _username,
            Data = messageBytes,
            SignedData = GetSignature(messageBytes)
        };
        //сериализую для дальнейшего шифрования
        var serializedMessage = JsonConvert.SerializeObject(msg);
        //преобразую сериализованное сообщение в байты
        var serializedMessageBytes = Encoding.UTF8.GetBytes(serializedMessage);
        var encryptedMessage = new EncryptedSignedMessage 
        {
            //шифрование полученных байтов с помощью публичного ключа получателя
            Data = EncryptMessage(serializedMessageBytes, _chatterPublicKey!)
        };
        //сериализация конечного объекта с зашифрованными данными
        var encryptedMessageJson = JsonConvert.SerializeObject(encryptedMessage);
        FilesProvider.SaveFile(encryptedMessageJson, $"{_username}_сообщение.txt");
    }
    public void ReceiveMessage()
    {
        ParseChatterPublicKey();
        var messageJsonEncrypted = FilesProvider.LoadFile("сообщение");
        var encryptedSignedMessage = JsonConvert.DeserializeObject<EncryptedSignedMessage>(messageJsonEncrypted);
        var decryptedMessage = DecryptMessage(encryptedSignedMessage!.Data, _keys.PrivateKey);

        if (decryptedMessage == null)
        {
            Console.WriteLine("Файл поврежден или модифицирован!\n");
            return;
        }

        //из полученных байтов получаю строку в виде json (сериализованный объект типа Message)
        var decryptedMessageJson = Encoding.UTF8.GetString(decryptedMessage);
        var message = JsonConvert.DeserializeObject<Message>(decryptedMessageJson);

        if (!VerifySignature(message!, _chatterPublicKey!))
        {
            Console.WriteLine("Подписи не совпали!\n");
            return;
        }

        if (_messages.Contains(message!.ID))
        {
            Console.WriteLine($"Пришел дубликат сообщения #{message!.ID}");
            return;
        }
        else
        {
            //добавлю ID сообщения в список всех сообщений сессии
            _messages.Add(message!.ID);
        }

        var messageStr = "";
        //если передавалась осмысленная строка, то данные преобразуются в нее
        //иначе вывдется просто их HEX представление 
        try
        {
            messageStr = Encoding.UTF8.GetString(message!.Data);
        }
        catch
        {
            messageStr = Convert.ToHexString(message!.Data);
        }
        Console.WriteLine($"Полученное сообщение #{message.ID} от {message.Sender}: {messageStr}\n");
    }
    private BigInteger[] EncryptMessage(byte[] message, PublicKey key)
    {
        var encrypted = new BigInteger[message.Length];
        for(int i = 0; i < message.Length; ++i)
        {
            encrypted[i] = BigInteger.ModPow(message[i], key.E, key.N);
        }
        return encrypted;
    }
    private byte[]? DecryptMessage(BigInteger[] message, PrivateKey key)
    {
        try
        {
            var decrypted = new byte[message.Length];
            for (int i = 0; i < message.Length; ++i)
            {
                var encryptedVal = BigInteger.ModPow(message[i], key.D, key.N);
                decrypted[i] = (byte)encryptedVal;
            }
            return decrypted;
        }
        catch
        {
            return null;
        } 
    }
    private BigInteger[] GetSignature(byte[]message)
    {
        var signedMessage = new BigInteger[message.Length];
        for(int i = 0; i < message.Length; ++i)
        {
            signedMessage[i] = BigInteger.ModPow(message[i], _keys.PrivateKey.D, _keys.PrivateKey.N);
        }
        return signedMessage;
    }
    private bool VerifySignature(Message message, PublicKey publicKey)
    {
        try
        {
            var unsignedMessage = new BigInteger[message.Data.Length];
            for (int i = 0; i < unsignedMessage.Length; ++i)
            {
                unsignedMessage[i] = BigInteger.ModPow(message.SignedData[i], publicKey.E, publicKey.N);
                if (unsignedMessage[i] != message.Data[i])
                {
                    return false;
                }
            }
        }
        catch
        {
            return false;
        }
        
        return true;
    }
    private PublicKey ParseChatterPublicKey()
    {
        if (_chatterPublicKey == null)
        {
            var chatterKeyJson = FilesProvider.LoadFile("открытый ключ собеседника");
            _chatterPublicKey = JsonConvert.DeserializeObject<PublicKey>(chatterKeyJson)!;
        }
        return _chatterPublicKey;
    }
    private BigInteger GetNewMessageID()
    {
        var isFound = false;
        var rand = new Random();
        var id = new BigInteger(0);
        var bytes = new byte[15];
        while (!isFound)
        {
            rand.NextBytes(bytes);
            id = new BigInteger(bytes);
            if (id < 0)
            {
                id = BigInteger.Negate(id);
            }
            if (!_messages.Contains(id))
            {
                isFound = true;
            }
        }
        return id;
    }
}