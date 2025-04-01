using System.Collections.Generic;

namespace Moe.Lowiro.Arcaea
{
    internal sealed class Group
    {
        internal int NoteCount
        {
            get
            {
                if (!allowInput) return 0;
                foreach (var obj in longs)
                {
                    note += obj.CalculateNote(bpms[obj.Timing], tpdf);
                }

                return note;
            }
        }

        internal Group(float tpdf, bool allowInput = true)
        {
            this.tpdf = tpdf;
            this.allowInput = allowInput;
        }

        internal void Add()
        {
            if (!allowInput) return;
            ++note;
        }

        internal void Add(LongObject obj)
        {
            if (!allowInput) return;
            longs.Add(obj);
        }

        internal void Add(Timing timing)
        {
            if (!allowInput) return;
            if (timings.Count == 0)
            {
                timing.Timing = 0;
            }

            timings.Add(timing);
        }

        internal void Preprocess()
        {
            if (!allowInput) return;
            timings.Sort((a, b) => a.Timing.CompareTo(b.Timing));
            longs.Sort((a, b) => a.Timing.CompareTo(b.Timing));
            var map = new SortedList<int, Timing>();
            foreach (var timing in timings)
            {
                map[timing.Timing] = timing;
            }

            var keys = new List<int>(map.Keys) { int.MaxValue }.ToArray();
            var i = 0;
            var bpm = map[0].Bpm;
            var set = new SortedSet<int>();
            foreach (var obj in longs)
            {
                set.Add(obj.Timing);
            }

            foreach (var timing in set)
            {
                while (timing >= keys[i + 1])
                {
                    ++i;
                    bpm = map[keys[i]].Bpm;
                }

                bpms.Add(timing, bpm);
            }
        }

        private readonly List<Timing> timings = [];
        private readonly List<LongObject> longs = [];
        private readonly Dictionary<int, float> bpms = new();
        private readonly float tpdf;
        private readonly bool allowInput;
        private int note;
    }
}