using LinearCongruentialGenerator;
using System.Collections;
using System.Data.Common;



/*
 * Если генерировать числа с каким-то ограничем снизу или сверху, то в таком случае
 * псведослучайность нарушается, из-за чего сгенерированная последовательность битов не проходит блочный тест,
 * т.к. в ней отствует часть битов, которые в совокупонсти с остальными битами формируют ту самую псведослучайность
 * и логично, если часть этих битов вырезать, то вся псведослучайность пойдет лесом
 *
 * Если ограничитель не задавать, то всё будет ок и блочный тест пройдет с успехом + частота встречи 1 в последовательности будет крайне близка к 0.5
 *
 */


void SavePlot(Dictionary<int, int> setDict, string path, string name, bool takeOnlyLower100 = true)
{
    var maxLimit = takeOnlyLower100 ? 100 : int.MaxValue;
    var sorted = setDict.OrderBy(pair => pair.Key);
    var x_Data = sorted.Select(num => (double)num.Key)
        .Where(num => num <= maxLimit)
        .ToArray();
    var y_Data = sorted.Where(pair => pair.Key <= maxLimit)
        .Select(pair => (double)pair.Value)
        .ToArray();

    var creator = new PlotCreator();
    creator.CreatePlot(x_Data, "Сгенерированное число", y_Data, "Кол-во раз сгенерировано");
    creator.Save(path, name);
}

string tag;
int module, a, b, x0;
int lowerBound = int.MinValue; int upperBound = int.MaxValue;
int length = -1;
ILCG lcg;

Console.Write("1 - NORMAL, 2 - RSA: ");

if (int.Parse(Console.ReadLine()!) == 1)
{
    Console.Write("module = "); module = int.Parse(Console.ReadLine()!);
    Console.Write("a = "); a = int.Parse(Console.ReadLine()!);
    Console.Write("b = "); b = int.Parse(Console.ReadLine()!);
    Console.Write("x0 = "); x0 = int.Parse(Console.ReadLine()!);
    Console.Write("length = "); length = int.Parse(Console.ReadLine()!);

    lcg = new LCG(module, a, b, x0);
    tag = "NORMAL";
    //lcg.GenerateSets(length);
}
else
{
    Console.Write("Нижняя граница генерации: "); lowerBound = int.Parse(Console.ReadLine()!);
    Console.Write("Верхняя граница генерации: "); upperBound = int.Parse(Console.ReadLine()!);
    lcg = new LCG_RSA();
    tag = "RSA";
}

lcg.GenerateSet(new object [] { length, lowerBound, upperBound });
lcg.Print();
SavePlot(lcg.SetDictionary, $@"C:\Users\Владимир\Desktop", $"[{tag}] plot.png", false);

var blockLen = 20;
Console.Write("Размер блока для теста: "); blockLen = int.Parse(Console.ReadLine());

var blockTestValue = NIST_BlockTest.BlockTest(new BitArray(lcg.TotalBits.ToArray()), blockLen);
Console.WriteLine($"Блок тест валью: {blockTestValue}");

var blockTestLite = new NIST_BlockTest_Lite(lcg.TotalBits, blockLen);
Console.WriteLine($"Частота появления 1: {blockTestLite.GetAvgFreq()}");


/*
 *  m = 101
    a = 66
    c = 16
    x0 = 18
 */