using System.Text;

public class Program
{
    static string _key = "yrt8ETXd4nPoDAAsCSg2Cjv9fecjbVhv";
    static string inputFile = @"C:\Users\Владимир\Downloads\мем.jpg";
    static string outputFile = @"C:\Users\Владимир\Downloads\мем_____.jpg";
    static void Main(string[] args)
    {
        Magma.MagmaCrypt magma = new Magma.MagmaCrypt(_key);
        var data = File.ReadAllBytes(inputFile);

        var encrypted = magma.Crypt(data);
        var decrypted = magma.Crypt(encrypted, true);

        File.WriteAllBytes(outputFile, decrypted.ToArray());
    }
}