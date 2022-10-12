namespace MagmaCrypt;

public class Fragment
{
    List<byte> _data;
    int _index;
    public Fragment(IEnumerable<byte> data, int index)
    {
        _data = data.ToList();
        _index = index;
    }
    public List<byte> Data => _data;
    public int Index => _index;
    
}
