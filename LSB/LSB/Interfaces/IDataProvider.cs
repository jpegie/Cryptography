using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSB.Interfaces;
public interface IDataProvider
{
    IEnumerable<byte> Data { get; }
    string Extension { get; } //возможно, боттлнек
}
