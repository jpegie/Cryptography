int [,] kMatrix = 
{ 
    { 2, 1 }, 
    { 9, 3 } 
};

string Enrypt(string str)
{
    if (str.Length % 2 != 0)
    {
        str += " ";
    }
    var encryptedStr = "";
    var couples = MakeCouples(str);
    foreach(var couple in couples)
    {
        var cryptedCouple = MultiplyCoupleToKMatrix(couple);
        encryptedStr += cryptedCouple;
    }
    return encryptedStr;
}

string MultiplyCoupleToKMatrix(string couple)
{
    var coupleArr = couple.ToCharArray();
    var result = new char[2];
    for(int i = 0; i < 2; ++i)
    {
        result[i] = (char)((kMatrix[i, 0] * coupleArr[0] + kMatrix[i, 1] * coupleArr[1]) % 256);
    }
    return new string(result);
}

List<string> MakeCouples(string str)
{
    List<string> couples = new List<string>();
    for (int i = 0; i < str.Length / 2; ++i)
    {
        couples.Add(str.Substring(i * 2, 2));
    }
    return couples;
}

int [,] GetTransposedKMatrix()
{
    int[,] kMatrixTransposed = new[,] 
    { 
        { 0, 0 }, 
        { 0, 0 } 
    };
    for (int i = 0; i < 2; ++i)
    {
        for (int j = 0; j < 2; ++j)
        {
            kMatrixTransposed[i, j] = kMatrix[j, i];
        }
    }
    return kMatrixTransposed;
}

string Decrypt(string str)
{
   
    var decryptingMatrix = GetMatrixForDecrypt();
    var couples = MakeCouples(str);
    var decryptedStr = "";
    foreach(var couple in couples)
    {
        decryptedStr += DecryptCouple(couple, decryptingMatrix);
    }
    return decryptedStr;
}

int [,] GetMatrixForDecrypt()
{
    int[,] result = new[,]
    {
        { 0, 0 },
        { 0, 0 }
    };
    var kMatrixTransposed = GetTransposedKMatrix();
    var inversedMatrix = (int[,])kMatrixTransposed.Clone();
    var kMatrixDet = kMatrix[0,0]*kMatrix[1,1] - kMatrix[0,1]*kMatrix[1,0];

    inversedMatrix[0, 0] = kMatrixTransposed[1, 1];
    inversedMatrix[0, 1] = -kMatrixTransposed[1, 0];
    inversedMatrix[1, 0] = -kMatrixTransposed[0, 1];
    inversedMatrix[1, 1] = kMatrixTransposed[0, 0];
    
    int x, y;
    Gcd(kMatrixDet, 256, out x, out y);

    for (int i=0;i<2 ; ++i)
    {
        for(int j=0;j<2 ; ++j)
        {
            inversedMatrix[i,j] *= x;
            inversedMatrix[i, j] = (int)((uint)inversedMatrix[i, j] % 256);
        }
    }
    return inversedMatrix;
}


string DecryptCouple(string couple, int [,] decryptMatrix)
{
    char[] decryptedChars = new char[2];
    for(int i=0;i<2 ; ++i)
    {
        decryptedChars[i] = (char)((decryptMatrix[i,0] * couple[0] + decryptMatrix[i, 1] * couple[1])%256);
    }
    return new string(decryptedChars);
}

int Gcd(int a, int b, out int x, out int y)
{

    if (a == 0)
    {
        x = 0;
        y = 1;
        return b;
    }

    int gcd = Gcd(b % a, a, out x, out y);

    int newY = x;
    int newX = y - (b / a) * x;

    x = newX;
    y = newY;
    return gcd;
}


Console.Write("String to encrypt: ");
var strToEncrypt = Console.ReadLine();

var encrypted = Enrypt(strToEncrypt!);
Console.WriteLine($"Encrypted: {encrypted}");

var decrypted = Decrypt(encrypted);
Console.WriteLine($"Decrypted: {decrypted}");
