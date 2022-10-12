public class Program
{
    static string _key = "yrt8ETXd4nPoDAAsCSg2Cjv9fecjbVhv";
    static void Main(string[] args)
    {
        Magma.MagmaCrypt magma;

        var data = File.ReadAllBytes(@"C:\Users\Владимир\Desktop\clips.txt");

        magma = new Magma.MagmaCrypt(data, _key);

        var encrypted = magma.Crypt();

        magma = new Magma.MagmaCrypt(encrypted, _key);

        var decrypted = magma.Crypt(true);

        File.WriteAllBytes(@"C:\Users\Владимир\Desktop\clips_copy.txt", decrypted.ToArray());
    }
}