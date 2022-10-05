using System.ComponentModel;
using System.Linq.Expressions;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using Magma;
public class Program
{
    static string _key = "yrt8ETXd4nPoDAAsCSg2Cjv9fecjbVhv";
    static void Main(string[] args)
    {
        MagmaCrypt magma;

        var inputStr = "";
        Console.Write("S: ");
        inputStr = Console.ReadLine();
        var inputBytes = Encoding.Default.GetBytes(inputStr!);
        
        magma = new MagmaCrypt(inputBytes, _key);
        var encrypted = magma.Crypt();
        var encryptedStr = Encoding.Default.GetString(encrypted.ToArray());
        Console.WriteLine($"E: {encryptedStr}");

        magma = new MagmaCrypt(encrypted, _key);
        var decrypted = magma.Crypt(true);
        var decryptedStr = Encoding.Default.GetString(decrypted.ToArray());
        Console.WriteLine($"D: {decryptedStr}");
    }
}