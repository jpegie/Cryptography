using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSB.Classes;
public static class Consts
{
    public static int BytesCapacityInExtension = 10;
    public static UInt16 ModificatedBitsInByte = 2;
    public static int SplittedPartsAmount = 8 / ModificatedBitsInByte;
}

