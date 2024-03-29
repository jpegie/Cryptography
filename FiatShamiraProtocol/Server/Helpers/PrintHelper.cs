﻿namespace Server.Helpers;
public static class PrintHelper
{
    static object printLock = new object();
    public static void PrintMessage(ValuedMessage message)
    {
        Print("@Ding-ding! Received message!@", true);
        foreach (var frame in message.Frames)
        {
            Print($"{message.Sender}: {frame.Key} = {frame.Value}", true);
        }

    }
    public static void Print(string message, bool addNewLine)
    {
        lock (printLock)
        {
            //если в строке есть что-то, то, чтобы не потерять, перейду на новую строку
            if (Console.CursorLeft != 0)
            {
                Console.WriteLine();
            }
            if (addNewLine)
            {
                Console.WriteLine(message);
            }
            else
            {
                Console.Write(message);
            }
        }
    }
}
