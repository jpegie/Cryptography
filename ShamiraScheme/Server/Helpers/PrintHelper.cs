namespace Server.Helpers;
public static class PrintHelper
{
    const int MAX_PRINTING_CHARS = 64;
    static object printLock = new object();
    public static void PrintMessage(ValuedMessage message)
    {
        Print("@Ding-ding! Received message!@", true);
        foreach (var frame in message.Frames)
        {
            Print($"{message.Sender}: {frame.Key} = {frame.Value}", true, true);
        }
    }
    public static void Print(string message, bool addNewLine, bool cutLenth = false)
    {
        lock (printLock)
        {
            var msgToPrint = "";
            msgToPrint += msgToPrint.Length == 0 && cutLenth ? "" : "...";
            if (cutLenth)
            {
                msgToPrint = String.Join("", message.Take(MAX_PRINTING_CHARS).ToList()) + "...";
            }
            else
            {
                msgToPrint = message;
            }
            //если в строке есть что-то, то, чтобы не потерять, перейду на новую строку
            if (Console.CursorLeft != 0)
            {
                Console.WriteLine();
            }
            if (addNewLine)
            {
                Console.WriteLine(msgToPrint);
            }
            else
            {
                Console.Write(msgToPrint);
            }
        }
    }
    public static void AddNewLine()
    {
        Print("", true);
    }

    public static void PrintBanknotes(List<Banknote> banknotes)
    {
        int i = 0;
        Print("\nCurrent banknotes list: ", true);
        foreach(var b in banknotes)
        {
            Print($"{i}) value = {b.Value}, ", false);
            Print($"sign = {b.Sign}, ", false, true);
            Print($"s*r = {b.SMultR}, ", false, true);
            Print($"s = {b.S}, ", false, true);
            Print($"r = {b.R}", true, true);
            i++;
        }
    }
}
