using System.Text;

public class Program
{
    static string _key = "yrt8ETXd4nPoDAAsCSg2Cjv9fecjbVhv";
    static string inputFile = @"C:\Users\Владимир\Downloads\VSCodeUserSetup-x64-1.71.2.exe";
    static string outputFile = @"C:\Users\Владимир\Downloads\VSCodeUserSetup-x64-1.71.2____.exe";
    static void Main(string[] args)
    {
        Magma.MagmaCrypt magma = new Magma.MagmaCrypt(_key);
        var data = File.ReadAllBytes(inputFile);

        var encrypted = magma.Crypt(data);
        var decrypted = magma.Crypt(encrypted, true);

        //Console.WriteLine($"Input data:\t {Encoding.Default.GetString(data)}");
        //Console.WriteLine($"Encrypted data:\t {Encoding.Default.GetString(encrypted.ToArray())}");
        //Console.WriteLine($"Decrypted data:\t {Encoding.Default.GetString(decrypted.ToArray())} \n" +
        //  $"Decrypted file is written into: {outputFile}");

        File.WriteAllBytes(outputFile, decrypted.ToArray());
    }
}