using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using LSB_;

var input = "0";

do
{
    Console.Write("Enter 0 for hide, 1 for reveal, 2 for exit: ");
    input = Console.ReadLine();

    if (input == "0")
    {
        Console.Write("Enter text to hide: ");
        var textBytes = Encoding.UTF8.GetBytes(Console.ReadLine()!);

        Console.Write("Image path (where to hide text): ");
        var imagePath = Console.ReadLine();

        var image = new Bitmap(Image.FromFile(imagePath));


        if (!IsAbleToHide(textBytes, image))
        {
            Console.WriteLine("Text's size > than possible to hide in the image");
            continue;
        }

        var modifiedImage = LSBcrypter.HideData(image, textBytes);
        var newPath = imagePath.Insert(imagePath.LastIndexOf("."), "_modified");
        modifiedImage.Save(newPath);

        Console.WriteLine($"Image saved into {newPath}" +
                          $"\nAmount of hidden bytes: {textBytes.Length}");
    }
    else if (input == "1")
    {
        Console.Write("Image path (with hidden text): ");
        var image = new Bitmap(Image.FromFile(Console.ReadLine()!));

        Console.Write("Amount of hidden bytes: ");
        var hiddenData = LSBcrypter.RevealData(image, int.Parse(Console.ReadLine()!));

        var hiddenText = Encoding.UTF8.GetString(hiddenData.ToArray());
        Console.WriteLine($"Hidden text in image: {hiddenText}");
    }
    Console.WriteLine("\n#\n");
}
while (input != "3");

bool IsAbleToHide(IEnumerable<byte> data, Bitmap image)
{
    //каждый пиксель = 3 бита информации
    //все пиксели * 3 / 8 = кол-во байт, которые можем туда запихать
    if (image.Width * image.Height * 3 / 8 < data.Count())
    {
        return false;
    }
    else
    {
        return true;
    }
}

