using EllipticCurves;

const string _alphabetSymbols = "qwertyuiopasdfghjklzxcvbnm1234567890!@ ";
const int _module = 41;
const int _a = 1;
const int _bobHiddenKey = 77;
int _aliceK = -1;
EllipticPoint _bobG = new EllipticPoint(-1, -1);
EllipticPoint _bobOpennedKey = new EllipticPoint(-1, -1);
EllipticPointsOperations.Module = _module;
EllipticPointsOperations.A = _a;

var alphabet = new Dictionary<char, EllipticPoint>();
var curvePoints = new EllipticCurve(_module).Points; FillAlphabet();
var strToEncrypt = "";
var strToDecrypt = "";
var encryptedStr = "";
var decryptedStr = "";
var encryptParam = new EnryptParameters(); encryptParam.Alphabet = alphabet;
var decryptParam = new DecryptParameters(); decryptParam.Alphabet = alphabet;   
var answer = "-2";

EllipticPointsOperations.Module = 41;
var a = new EllipticPoint(33, 4);
var c = new EllipticPoint(8, 20);
var b = EllipticPointsOperations.Sum(a, c);

while (answer != "5")
{
    Console.WriteLine(
    "What wanna do?\n" +
    "|_ 0 : enter Bob's openned key\n" +
    "|_ 1 : enter Alice's 'k'\n" +
    "|_ 2 : encrypt (for Alice)\n" +
    "|_ 3 : decrypt (for Bob)\n" +
    "|_ 4 : print openned keys\n" +
    "|_ 5 : exit");
    Console.Write("Input: ");
    answer = Console.ReadLine();

    if (answer == "0")
    {
        Console.WriteLine("Here's all curve points, choose one (Bob's G): ");
        for(int i = 0; i < curvePoints.Count; ++i)
        {
            if (i % 5 == 0 && i != 0) Console.WriteLine();
            Console.Write($"{i}. [{curvePoints[i].X}; {curvePoints[i].Y}]\t({EllipticPointsOperations.GetPointOrder(curvePoints[i])})\t");
        }

        Console.Write("\nIndex of point: ");
        var choosenIndex = int.Parse(Console.ReadLine()!);
        _bobG = curvePoints[choosenIndex];

        //the best security method
        Console.WriteLine("Are u Bob (your cat gonna die if you lie)? (Yes/No)");
        Console.Write("Input: ");
        if (Console.ReadLine() == "Yes")
        {
            _bobOpennedKey = EllipticPointsOperations.Multiply(_bobG, _bobHiddenKey);
            Console.WriteLine($"Openned key is generated - [{_bobOpennedKey.X};{_bobOpennedKey.Y}]");
        }
        else
        {
            Console.WriteLine("Coordinates of openned key (point): ");
            Console.Write("X: "); var x = int.Parse(Console.ReadLine()!);
            Console.Write("Y: "); var y = int.Parse(Console.ReadLine()!);
            _bobOpennedKey = new EllipticPoint(x, y);
        } 
    }
    if (answer == "1")
    {
        Console.Write("K: ");
        _aliceK = int.Parse(Console.ReadLine()!);
    }
    if (answer == "2")
    {
        if (_aliceK == -1)
        {
            Console.WriteLine("Do '1' step before encrypting");
            continue;
        }
        if (_bobG.X == -1 || _bobG.Y == -1)
        {
            Console.WriteLine("Do step '0' before decrypting");
            continue;
        }
        Console.Write("Text to encrypt: ");
        strToEncrypt = Console.ReadLine();
        encryptParam.BobG = _bobG;
        encryptParam.StringToEncrypt = strToEncrypt;
        encryptParam.AliceK = _aliceK;
        encryptParam.BobOpennedKey = _bobOpennedKey;
        encryptedStr = new EllipticCurveEcnrypter().Crypt(encryptParam);

        Console.BackgroundColor = ConsoleColor.Green;
        Console.ForegroundColor = ConsoleColor.Black;
        Console.Write($"Encrypted string: {encryptedStr}");
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
    }
    if (answer == "3")
    {
        if (_aliceK == -1)
        {
            Console.WriteLine("Do step '1' before decrypting");
            continue;
        }
        if (_bobG.X == -1 || _bobG.Y == -1)
        {
            Console.WriteLine("Do step '0' before decrypting");
            continue;
        }
        Console.Write("Text to decrypt: ");
        strToDecrypt = Console.ReadLine();
        decryptParam.BobHiddenKey = _bobHiddenKey;
        //decryptParam.AliceK = _aliceK;
        decryptParam.EncryptedString = encryptedStr;
        decryptParam.EncryptedString = strToDecrypt;
        decryptedStr = new EllipticCurveDecrypter().Decrypt(decryptParam);
        Console.BackgroundColor = ConsoleColor.Green;
        Console.ForegroundColor = ConsoleColor.Black;
        Console.Write($"Decrypted string: {decryptedStr}");
        Console.BackgroundColor = ConsoleColor.Black;
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();
    }
    if (answer == "4")
    {
        Console.WriteLine(
            $"Alice's 'k': {_aliceK}\n" + 
            $"Bob G: [{_bobG.X}; {_bobG.Y}]\n" +
            $"Bob openned key: [{_bobOpennedKey.X};{_bobOpennedKey.Y}]");
        continue;
    }
    Console.WriteLine("#");
}

void FillAlphabet()
{
    alphabet = new Dictionary<char, EllipticPoint>();
   for (int i=0; i < curvePoints.Count ; ++i)
   {
        alphabet.Add(_alphabetSymbols[i], curvePoints[i]);
   }
}