namespace EllipticCurves;
public class EllipticCurve
{
    List<EllipticPoint> _points;
    int _module = 3;
    public EllipticCurve(int module)
    {
        _module = module; 
        _points = new List<EllipticPoint>();
        FindPoints();
    }
    public List<EllipticPoint> Points => _points;
    public int Module => _module;
    private void FindPoints()
    {
        _points.Add(new EllipticPoint(0, 0) { IsO = true });

        for (int x = 0; x < _module; ++x)
        {
            for (int y = 0; y < _module; ++y)
            {
                if (((int)Math.Pow(y, 2) % _module) == (((int)Math.Pow(x, 3) + x + 3)) % _module)
                {
                    _points.Add(new EllipticPoint(x, y));
                }
            }
        }
    } 
}