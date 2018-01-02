
struct Coord
{
    public int tileX;
    public int tileY;

    public Coord(int x, int y)
    {
        tileX = x;
        tileY = y;
    }

    /// <summary>
    /// 两坐标之间平方之和
    /// </summary>
    public float SqrMagnitude(Coord coordB)
    {
        return (tileX - coordB.tileX) * (tileX - coordB.tileX) + (tileY - coordB.tileY) * (tileY - coordB.tileY);
    }

}
