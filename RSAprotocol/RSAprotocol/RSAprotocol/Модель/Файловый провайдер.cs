using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

public class FilesProvider
{
    public static string SaveFile(string text, string fileName)
    {
        var isPathValid = false;
        var path = "";

        while (!isPathValid)
        {
            Console.Write($"Путь для сохранения {fileName}: ");
            path = Console.ReadLine();
            if (path == String.Empty)
            {
                path = AppDomain.CurrentDomain.BaseDirectory;
            }
            if (Directory.Exists(path))
            {
                isPathValid = true;
            }
            else
            {
                Console.WriteLine("Такой директории не существует, введите валидную\n");
            }
        }
        path = Path.Combine(path!, fileName);
        File.WriteAllText(path!, text);
        Console.WriteLine($"Файл сохранен по пути: {path}\n");
        return path;
    }
    public static string LoadFile(string filename)
    {
        var isFileValid = false;
        var path = "";

        while (!isFileValid)
        {
            Console.Write($"Путь до файла \"{filename}\": ");
            path = Console.ReadLine();

            if (File.Exists(path))
            {
                isFileValid = true;
            }
            else
            {
                Console.WriteLine("Такого файла не существует, введите валидный путь");
            }
        }
        var text = File.ReadAllText(path!);
        Console.WriteLine($"Файл \"{filename}\" загружен\n");
        return text;
    }
}
