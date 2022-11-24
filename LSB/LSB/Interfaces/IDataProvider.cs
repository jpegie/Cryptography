using System;
namespace LSB.Interfaces;
public interface IDataProvider
{
    IEnumerable<byte> Data { get; }
}
