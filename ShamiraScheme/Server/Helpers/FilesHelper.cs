using System.IO;

namespace Server.Helpers;
public class FilesHelper
{
    public static bool WriteData(string fileName, string dirPath, byte[] data)
    {
        try
        {
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            var filePath = Path.Combine(dirPath, fileName);
            File.WriteAllBytes(filePath, data);

            PrintHelper.Print($"Data succesfully written into '{filePath}'", true);
            return true;
        }
        catch
        {
            return false;
        }
    }
    public static byte[] ReadFile(string filePath)
    {
        return File.ReadAllBytes(filePath);
    }
    public static void SaveFile(byte[] data)
    {
        PrintHelper.Print("Directory to save to: ", false);
        var directory = Console.ReadLine()!;
        PrintHelper.Print("File name: ", false);
        var fileName = Console.ReadLine()!;
        WriteData(fileName, directory, data);
    }
}
