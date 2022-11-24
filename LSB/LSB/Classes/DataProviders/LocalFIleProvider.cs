using LSB.Interfaces;

namespace LSB.Classes.DataProviders;
public class LocalFileProvider : IDataProvider
{
    private IEnumerable<byte> _data;
    private string _extension;
    private string _path;
    
    public LocalFileProvider(string imagePath, IEnumerable<byte>? data = null)
    {
        _path = imagePath;
        _data = data;
    }
    public IEnumerable<byte> Data => _data;
    public string Extension => _extension;
    public string FilePath => _path;
    public bool LoadData()
    {
        try
        {
            _data = File.ReadAllBytes(_path);
            _extension = Path.GetExtension(_path);
            return true;
        }
        catch(Exception)
        {
            return false;
        }  
    }
}
