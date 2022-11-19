using LSB.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSB.Classes;
public class LSB
{
    


    IDataProvider _originalProvider;
    IDataProvider _hiddenProvider;
    LSBkey _key;
    /// <summary>
    /// </summary>
    /// <param name="originalDataProvider">провайдер данных, в которых будем прятать данные</param>
    /// <param name="hiddenDataProvider">то, что будем прятать</param>
    public LSB(IDataProvider originalDataProvider, IDataProvider hiddenDataProvider, LSBKey key)
    {
        _originalProvider = originalDataProvider;
        _hiddenProvider = hiddenDataProvider;
    }
    public LSB()
    {

    }
    public IEnumerable<byte> HideData()
    {

    }

    public IEnumerable<byte> UnhideData()
    {

    }


}
