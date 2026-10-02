namespace Moe.Lowiro.Arcaea
{
    internal sealed class Arc : LongObject
    {
        internal float XStart { get; }

        internal float XEnd { get; }

        internal float YStart { get; }

        internal float YEnd { get; }

        internal Arc(int timingStart, int timingEnd, float xStart, float xEnd, float yStart, float yEnd) :
            base(timingStart, timingEnd)
        {
            XStart = xStart;
            XEnd = xEnd;
            YStart = yStart;
            YEnd = yEnd;
        }
    }
}