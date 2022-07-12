using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Moe.Lowiro.Arcaea
{
    public class Chart
    {
        public static int CountNote(string path) => new Chart(new StringReader(File.ReadAllText(path, encoding))).note;

        public static int CountNote(Stream stream)
        {
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            return new Chart(new StringReader(encoding.GetString(bytes))).note;
        }

        private static readonly Encoding encoding = new UTF8Encoding(false);
        private readonly int note = 0;

        private Chart(StringReader reader)
        {
            bool header = true;
            int line = 1;
            float tpdf = 1;
            Group mainGroup = null;
            Group currGroup = null;
            List<Group> groups = new List<Group>();
            List<Arc> arcs = new List<Arc>();
            while (reader.Peek() != -1)
            {
                string data = reader.ReadLine().Replace(" ", string.Empty);
                if (data.Length > 0)
                {
                    if (header)
                    {
                        if (data.StartsWith("AudioOffset:"))
                        {
                            if (!int.TryParse(data.Substring(12), out _))
                            {
                                throw new ChartFormatException(ChartErrorType.AudioOffset, line);
                            }
                        }
                        else if (data.StartsWith("TimingPointDensityFactor:"))
                        {
                            if (!float.TryParse(data.Substring(25), out tpdf))
                            {
                                throw new ChartFormatException(ChartErrorType.TimingPointDensityFactor, line);
                            }
                        }
                        else if (data == "-")
                        {
                            header = false;
                            mainGroup = new Group(tpdf);
                            currGroup = mainGroup;
                        }
                        else if (data.StartsWith("timing("))
                        {
                            throw new ChartFormatException(ChartErrorType.Delimiter, line);
                        }
                        else
                        {
                            throw new ChartFormatException(ChartErrorType.FileFormat, line);
                        }
                    }
                    else
                    {
                        if (data.Length == 0) { }
                        else if (data.StartsWith("timinggroup(") && data.EndsWith("){"))
                        {
                            if (currGroup != mainGroup)
                            {
                                throw new ChartFormatException(ChartErrorType.TimingGroup, line);
                            }
                            bool allowInput = true;
                            foreach (string arg in data.Substring(12, data.Length - 14).Split('_'))
                            {
                                if (arg.Length == 0) { }
                                else if (arg == "noinput")
                                {
                                    allowInput = false;
                                }
                                else if (arg == "fadingholds") { }
                                else if (arg.StartsWith("anglex"))
                                {
                                    if (!int.TryParse(arg.Substring(6), out _))
                                    {
                                        throw new ChartFormatException(ChartErrorType.TimingGroup, line);
                                    }
                                }
                                else if (arg.StartsWith("angley"))
                                {
                                    if (!int.TryParse(arg.Substring(6), out _))
                                    {
                                        throw new ChartFormatException(ChartErrorType.TimingGroup, line);
                                    }
                                }
                                else
                                {
                                    throw new ChartFormatException(ChartErrorType.TimingGroup, line);
                                }
                            }
                            currGroup = new Group(tpdf, allowInput);
                        }
                        else if (data == "};")
                        {
                            if (currGroup == mainGroup)
                            {
                                throw new ChartFormatException(ChartErrorType.TimingGroup, line);
                            }
                            currGroup.Preprocess();
                            groups.Add(currGroup);
                            currGroup = mainGroup;
                        }
                        else if (data.StartsWith("timing(") && data.EndsWith(");"))
                        {
                            if (data.Length >= 14)
                            {
                                string[] args = data.Substring(7, data.Length - 9).Split(',');
                                if (args.Length == 3 &&
                                    int.TryParse(args[0], out int t) &&
                                    float.TryParse(args[1], out float bpm) &&
                                    float.TryParse(args[2], out float bpl) &&
                                    t >= 0 &&
                                    (bpm == 0 || bpl != 0))
                                {
                                    currGroup.Add(new Timing(t, Math.Abs(bpm)));
                                    continue;
                                }
                            }
                            throw new ChartFormatException(ChartErrorType.Timing, line);
                        }
                        else if (data[0] == '(' && data.EndsWith(");"))
                        {
                            if (data.Length >= 6)
                            {
                                string[] args = data.Substring(1, data.Length - 3).Split(',');
                                if (args.Length == 2 &&
                                    int.TryParse(args[0], out int t) &&
                                    int.TryParse(args[1], out int tk) &&
                                    t >= 0 &&
                                    tk >= 0 &&
                                    tk <= 5)
                                {
                                    currGroup.Add();
                                    continue;
                                }
                            }
                            throw new ChartFormatException(ChartErrorType.Tap, line);
                        }
                        else if (data.StartsWith("hold(") && data.EndsWith(");"))
                        {
                            if (data.Length >= 12)
                            {
                                string[] args = data.Substring(5, data.Length - 7).Split(',');
                                if (args.Length == 3 &&
                                    int.TryParse(args[0], out int st) &&
                                    int.TryParse(args[1], out int et) &&
                                    int.TryParse(args[2], out int tk) &&
                                    st >= 0 &&
                                    et >= 0 &&
                                    tk >= 0 &&
                                    tk <= 5)
                                {
                                    currGroup.Add(new LongObject(st, et));
                                    continue;
                                }
                            }
                            throw new ChartFormatException(ChartErrorType.Hold, line);
                        }
                        else if (data.StartsWith("arc(") && data[data.Length - 1] == ';')
                        {
                            string ts = null;
                            int s = data.IndexOf('[');
                            int e = data.IndexOf(']');
                            if (s >= 30 && e >= 31 && s < e)
                            {
                                ts = data.Substring(s + 1, e - s - 1);
                                data = data.Remove(s, e - s + 1);
                            }
                            if (data.Length >= 31)
                            {
                                string[] args = data.Substring(4, data.Length - 6).Split(',');
                                if (args.Length == 10 &&
                                    int.TryParse(args[0], out int st) &&
                                    int.TryParse(args[1], out int et) &&
                                    float.TryParse(args[2], out float sx) &&
                                    float.TryParse(args[3], out float ex) &&
                                    CheckArcCurve(args[4]) &&
                                    float.TryParse(args[5], out float sy) &&
                                    float.TryParse(args[6], out float ey) &&
                                    int.TryParse(args[7], out int cr) &&
                                    CheckArcFx(args[8]) &&
                                    bool.TryParse(args[9], out bool vd) &&
                                    st >= 0 &&
                                    et >= 0 &&
                                    ((vd |= ts != null) || (st <= et && cr >= 0 && cr <= 2)))
                                {
                                    if (ts != null)
                                    {
                                        vd = true;
                                    }
                                    if (vd)
                                    {
                                        if (ts != null)
                                        {
                                            foreach (string arg in ts.Split(','))
                                            {
                                                if (arg.Length >= 9 &&
                                                    arg.StartsWith("arctap(") &&
                                                    arg[arg.Length - 1] == ')' &&
                                                    int.TryParse(arg.Substring(7, arg.Length - 8), out int t) &&
                                                    t >= st &&
                                                    t <= et)
                                                {
                                                    currGroup.Add();
                                                }
                                                else
                                                {
                                                    throw new ChartFormatException(ChartErrorType.ArcTap, line);
                                                }
                                            }
                                        }
                                        continue;
                                    }
                                    else
                                    {
                                        Arc arc = new Arc(st, et, sx, ex, sy, ey);
                                        currGroup.Add(arc);
                                        arcs.Add(arc);
                                        continue;
                                    }
                                }
                            }
                            throw new ChartFormatException(ChartErrorType.Arc, line);
                        }
                        else if (data.StartsWith("scenecontrol(") && data.EndsWith(");"))
                        {
                            if (data.Length >= 24)
                            {
                                string[] args = data.Substring(13, data.Length - 15).Split(',');
                                if ((args.Length == 2 ||
                                    (args.Length == 4 &&
                                    float.TryParse(args[2], out float d) &&
                                    int.TryParse(args[3], out int v) &&
                                    d >= 0 &&
                                    v >= 0)) &&
                                    int.TryParse(args[0], out int t) &&
                                    t >= 0 &&
                                    CheckSceneCtrlFx(args[1]))
                                {
                                    continue;
                                }
                            }
                            throw new ChartFormatException(ChartErrorType.SceneControl, line);
                        }
                        else if (data.StartsWith("camera(") && data.EndsWith(");"))
                        {
                            if (data.Length >= 26)
                            {
                                string[] args = data.Substring(7, data.Length - 9).Split(',');
                                if (args.Length == 9 &&
                                    int.TryParse(args[0], out int t) &&
                                    float.TryParse(args[1], out _) &&
                                    float.TryParse(args[2], out _) &&
                                    float.TryParse(args[3], out _) &&
                                    float.TryParse(args[4], out _) &&
                                    float.TryParse(args[5], out _) &&
                                    float.TryParse(args[6], out _) &&
                                    CheckCameraMotion(args[7]) &&
                                    int.TryParse(args[8], out int d) &&
                                    t >= 0 &&
                                    d >= 0)
                                {
                                    continue;
                                }
                            }
                            throw new ChartFormatException(ChartErrorType.Camera, line);
                        }
                        else
                        {
                            throw new ChartFormatException(ChartErrorType.Unknown, line);
                        }
                    }
                }
                ++line;
            }
            if (currGroup != mainGroup)
            {
                throw new ChartFormatException(ChartErrorType.TimingGroup, line);
            }
            mainGroup.Preprocess();
            groups.Add(mainGroup);
            arcs.Sort((a, b) => a.Timing.CompareTo(b.Timing));
            Arc[] scra = arcs.ToArray();
            Array.Sort(scra, (a, b) => a.EndTiming.CompareTo(b.EndTiming));
            int m = scra.Length;
            int i = 0;
            foreach (Arc arc in arcs)
            {
                for (int j = i; j < m; ++j)
                {
                    Arc prev = scra[j];
                    if (prev.EndTiming <= arc.Timing - 10)
                    {
                        i = j;
                    }
                    else if (prev.EndTiming >= arc.Timing + 10)
                    {
                        break;
                    }
                    else if (arc != prev && arc.StartY == prev.EndY && Math.Abs(arc.StartX - prev.EndX) < 0.1)
                    {
                        arc.HasHead = false;
                    }
                }
            }
            foreach (Group group in groups)
            {
                note += group.NoteCount;
            }
        }

        private bool CheckArcCurve(string arg)
        {
            switch (arg)
            {
            case "b":
            case "s":
            case "si":
            case "so":
            case "sisi":
            case "siso":
            case "sosi":
            case "soso": return true;
            default: return false;
            }
        }

        private bool CheckArcFx(string arg)
        {
            switch (arg)
            {
            case "none":
            case "full":
            case "incremental":
            case "glass_wav":
            case "kick_wav":
            case "voice_wav": return true;
            default: return false;
            }
        }

        private bool CheckSceneCtrlFx(string arg)
        {
            switch (arg)
            {
            case "arcahvdebris":
            case "arcahvdistort":
            case "hidegroup":
            case "redline":
            case "trackdisplay":
            case "trackhide":
            case "trackshow":
            case "enwidencamera":
            case "enwidenlanes": return true;
            default: return false;
            }
        }

        private bool CheckCameraMotion(string arg)
        {
            switch (arg)
            {
            case "l":
            case "reset":
            case "s":
            case "qi":
            case "qo": return true;
            default: return false;
            }
        }
    }
}