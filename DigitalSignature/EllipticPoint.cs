namespace EllipticCurves;
public class EllipticPoint
{
    int _x, _y;
    int _order = 1;
    bool _isO = false;
    public EllipticPoint(int x, int y)
    {
        this._x = x;
        this._y = y;
    }
    public int X
    {
        get => _x;
        set => _x = value;
    }
    public int Y
    {
        get => _y;
        set => _y = value;
    }
    public int Order
    {
        get => _order;
        set => _order = value;
    }

    public bool Equals(EllipticPoint point)
    {
        return ((point.X == _x) && (point.Y == _y));
    }
    public bool IsO
    {
        get => _isO;
        set => _isO = value;
    }
}