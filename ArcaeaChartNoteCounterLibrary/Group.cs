using System.Collections.Generic;

namespace Moe.Lowiro.Arcaea
{
    internal sealed class Group
    {
        internal int NoteCount
        {
            get
            {
                if (allowInput)
                {
                    foreach (LongObject obj in longs)
                    {
                        note += obj.CalculateNote(bpms[obj.Timing], tpdf);
                    }
                    return note;
                }
                else
                {
                    return 0;
                }
            }
        }

        internal Group(float tpdf, bool allowInput = true)
        {
            this.tpdf = tpdf;
            this.allowInput = allowInput;
        }

        internal void Add()
        {
            if (allowInput)
            {
                ++note;
            }
        }

        internal void Add(LongObject obj)
        {
            if (allowInput)
            {
                longs.Add(obj);
            }
        }

        internal void Add(Timing timing)
        {
            if (allowInput)
            {
                if (timings.Count == 0)
                {
                    timing.Timing = 0;
                }
                timings.Add(timing);
            }
        }

        internal void Preprocess()
        {
            if (allowInput)
            {
                timings.Sort((a, b) => a.Timing.CompareTo(b.Timing));
                longs.Sort((a, b) => a.Timing.CompareTo(b.Timing));
                SortedList<int, Timing> map = new SortedList<int, Timing>();
                foreach (Timing timing in timings)
                {
                    if (map.ContainsKey(timing.Timing))
                    {
                        map[timing.Timing] = timing;
                    }
                    else
                    {
                        map.Add(timing.Timing, timing);
                    }
                }
                int[] keys = new List<int>(map.Keys) { int.MaxValue }.ToArray();
                int i = 0;
                float bpm = map[0].Bpm;
                SortedSet<int> set = new SortedSet<int>();
                foreach (LongObject obj in longs)
                {
                    set.Add(obj.Timing);
                }
                foreach (int timing in set)
                {
                    while (timing >= keys[i + 1])
                    {
                        ++i;
                        bpm = map[keys[i]].Bpm;
                    }
                    bpms.Add(timing, bpm);
                }
            }
        }

        private readonly List<Timing> timings = new List<Timing>();
        private readonly List<LongObject> longs = new List<LongObject>();
        private readonly Dictionary<int, float> bpms = new Dictionary<int, float>();
        private readonly float tpdf;
        private readonly bool allowInput;
        private int note = 0;
    }
}