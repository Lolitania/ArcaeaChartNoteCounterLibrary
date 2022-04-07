namespace Moe.Lowiro.Arcaea
{
    internal sealed class Timing : Event
    {
        internal float Bpm { get; }

        internal Timing(int timing, float bpm)
        {
            Timing = timing;
            Bpm = bpm;
        }
    }
}