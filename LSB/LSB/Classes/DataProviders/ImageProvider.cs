using LSB.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LSB.Classes.DataProviders;
public class ImageProvider : IDataProvider
{
    private IEnumerable<byte> _data;
    private string _extension;
    private string _imagePath;
    
    public ImageProvider(string imagePath)
    {
        _imagePath = imagePath; 
    }
    public IEnumerable<byte> Data => _data;
    string Extension => _extension;
    public bool LoadData()
    {
        try
        {
            _data = File.ReadAllBytes(_imagePath);
            _extension = Path.GetExtension(_imagePath);
            return true;
        }
        catch(Exception ex)
        {
            return false;
        }  
    }
}
