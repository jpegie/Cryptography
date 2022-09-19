using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using System.ComponentModel;

var kMatrix = DenseMatrix.OfArray(new [,] { { 2.0, 1.0 }, { 9.0, 3.0 } });

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
        var cryptedCouple = MultiplyCoupleToMatrix(couple, kMatrix);
        encryptedStr += cryptedCouple;
    }
    return encryptedStr;
}

string MultiplyCoupleToMatrix(string couple, Matrix<double> matrix)
{
    var coupleArr = couple.ToCharArray();
    var coupleMatrix = DenseMatrix.OfArray(new[,] 
    { 
        { (double)coupleArr[0] }, 
        { (double)coupleArr[1] } 
    });
    var resultMatrix = (DenseMatrix)matrix.Multiply(coupleMatrix);
    var result = new char[2];
    result[0] = (char)(resultMatrix.Column(0)[0] % 256);
    result[1] = (char)(resultMatrix.Column(0)[1] % 256);   
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

string Decrypt(string str)
{
    var decryptingMatrix = GetMatrixForDecrypt();
    var couples = MakeCouples(str);
    var decryptedStr = "";
    foreach(var couple in couples)
    {
        decryptedStr += MultiplyCoupleToMatrix(couple, decryptingMatrix);
    }
    return decryptedStr;
}

Matrix<double> GetMatrixForDecrypt()
{ 
    var result = new DenseMatrix(2, 2);
    var kMatrixDet = kMatrix.Determinant();
    var kMatrixInversed = kMatrix.Inverse();
    kMatrixInversed.Multiply(kMatrixDet, kMatrixInversed);

    var modularInversedDet = GetModularInversedNumber((int)kMatrixDet, 256);

    kMatrixInversed *= modularInversedDet;

    for (int i=0;i<2; ++i)
    {
        for(int j=0;j<2 ; ++j)
        {
            if (kMatrixInversed[i,j] < 0)
            {
                kMatrixInversed[i, j] = (uint)Math.Floor(kMatrixInversed[i, j]) % 256;
            }
            else
            {
                kMatrixInversed[i, j] = (uint)Math.Ceiling(kMatrixInversed[i, j]) % 256;
            }
        }
    }
    return kMatrixInversed;
}

int GetModularInversedNumber(int number, int module)
{
    int x, y;
    int d = GCD(number, module, out x, out y);
    if (d != 1)
    {
        throw new Exception("Нет решения, т.к. НОД != 1!");
    }
    return x;
}

int GCD(int a, int b, out int x, out int y)
{

    if (a == 0)
    {
        x = 0;
        y = 1;
        return b;
    }

    int gcd = GCD(b % a, a, out x, out y);

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
