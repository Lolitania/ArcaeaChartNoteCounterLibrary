namespace Moe.Lowiro.Arcaea
{
    internal class LongObject : Event
    {
        internal int EndTiming { get; set; }

        internal bool HasHead { get; set; }

        internal LongObject(int timing, int endTiming)
        {
            Timing = timing;
            EndTiming = endTiming;
            HasHead = true;
        }

        internal int CalculateNote(float bpm, float tpdf)
        {
            if (Timing >= EndTiming) return 0;
            // Do NOT check "Code Optimization" in the Project Properties!!!
            // I HATE FLOATING POINT ERROR...
            float d = EndTiming - Timing;
            float unit = bpm >= 255 ? 60000 : 30000;
            unit /= bpm;
            unit /= tpdf;
            var cf = d / unit;
            var ci = (int)cf;
            return ci <= 1 ? 1 : HasHead ? ci - 1 : ci;
        }
    }
}