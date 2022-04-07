namespace Moe.Lowiro.Arcaea
{
    internal sealed class Arc : LongObject
    {
        internal float StartX { get; }

        internal float EndX { get; }

        internal float StartY { get; }

        internal float EndY { get; }

        internal Arc(int st, int et, float sx, float ex, float sy, float ey) : base(st, et)
        {
            StartX = sx;
            EndX = ex;
            StartY = sy;
            EndY = ey;
        }
    }
}