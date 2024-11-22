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
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            return new Chart(new StringReader(encoding.GetString(bytes))).note;
        }

        private static readonly Encoding encoding = new UTF8Encoding(false);
        private readonly int note = 0;

        private Chart(StringReader reader)
        {
            var header = true;
            var line = 1;
            var tpdf = 1f;
            Group mainGroup = null;
            Group currGroup = null;
            var groups = new List<Group>();
            var arcs = new List<Arc>();
            while (reader.Peek() != -1)
            {
                var data = reader.ReadLine().Replace(" ", string.Empty);
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

                            var allowInput = true;
                            foreach (var arg in data.Substring(12, data.Length - 14).Split('_'))
                            {
                                switch (arg)
                                {
                                case "": break;
                                case "noinput":
                                    allowInput = false;
                                    break;
                                case "fadingholds": break;
                                default:
                                    if (arg.StartsWith("anglex"))
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

                                    break;
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
                                var args = data.Substring(7, data.Length - 9).Split(',');
                                if (args.Length == 3 &&
                                    int.TryParse(args[0], out var t) &&
                                    float.TryParse(args[1], out var bpm) &&
                                    float.TryParse(args[2], out var bpl) &&
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
                                var args = data.Substring(1, data.Length - 3).Split(',');
                                if (args.Length == 2 &&
                                    int.TryParse(args[0], out var t) &&
                                    float.TryParse(args[1], out _) &&
                                    t >= 0)
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
                                var args = data.Substring(5, data.Length - 7).Split(',');
                                if (args.Length == 3 &&
                                    int.TryParse(args[0], out var st) &&
                                    int.TryParse(args[1], out var et) &&
                                    float.TryParse(args[2], out _) &&
                                    st >= 0 &&
                                    et >= st)
                                {
                                    currGroup.Add(new LongObject(st, et));
                                    continue;
                                }
                            }

                            throw new ChartFormatException(ChartErrorType.Hold, line);
                        }
                        else if (data.StartsWith("arc(") && data[data.Length - 1] == ';')
                        {
                            string dataExtra = null;
                            {
                                var sb = data.IndexOf('[');
                                var eb = data.IndexOf(']');
                                if (sb >= 30 && sb < eb)
                                {
                                    dataExtra = data.Substring(sb + 1, eb - sb - 1);
                                    data = data.Remove(sb, eb - sb + 1);
                                }
                            }

                            if (data.Length >= 31)
                            {
                                var args = data.Substring(4, data.Length - 6).Split(',');
                                var status = ArcStatus.Unknown;
                                switch (args[9])
                                {
                                case "false":
                                    status = dataExtra == null
                                        ? ArcStatus.Normal
                                        : ArcStatus.TraceWithArcTap;
                                    break;
                                case "true":
                                    status = dataExtra == null
                                        ? ArcStatus.Trace
                                        : ArcStatus.TraceWithArcTap;
                                    break;
                                case "designant":
                                    status = dataExtra == null
                                        ? ArcStatus.Designant
                                        : ArcStatus.DesignantWithArcTap;
                                    break;
                                }

                                if (args.Length == 10 &&
                                    int.TryParse(args[0], out var st) &&
                                    int.TryParse(args[1], out var et) &&
                                    float.TryParse(args[2], out var sx) &&
                                    float.TryParse(args[3], out var ex) &&
                                    CheckArcCurve(args[4]) &&
                                    float.TryParse(args[5], out var sy) &&
                                    float.TryParse(args[6], out var ey) &&
                                    int.TryParse(args[7], out var cl) &&
                                    status != ArcStatus.Unknown &&
                                    st >= 0 &&
                                    et >= 0 &&
                                    (status != ArcStatus.Normal || (st <= et && cl >= 0 && cl <= 3)))
                                {
                                    switch (status)
                                    {
                                    case ArcStatus.Normal:
                                        if (cl == 3 && st == et)
                                        {
                                            currGroup.Add();
                                        }
                                        else
                                        {
                                            var arc = new Arc(st, et, sx, ex, sy, ey);
                                            currGroup.Add(arc);
                                            arcs.Add(arc);
                                        }

                                        break;
                                    case ArcStatus.TraceWithArcTap:
                                        foreach (var cmd in dataExtra.Split(','))
                                        {
                                            if (cmd.Length >= 9 &&
                                                cmd.StartsWith("arctap(") &&
                                                cmd[cmd.Length - 1] == ')' &&
                                                int.TryParse(cmd.Substring(7, cmd.Length - 8), out _))
                                            {
                                                currGroup.Add();
                                            }
                                            else
                                            {
                                                throw new ChartFormatException(ChartErrorType.ArcTap, line);
                                            }
                                        }

                                        break;
                                    }

                                    continue;
                                }
                            }

                            throw new ChartFormatException(ChartErrorType.Arc, line);
                        }
                        else if (data.StartsWith("scenecontrol(") && data.EndsWith(");"))
                        {
                            if (data.Length >= 24)
                            {
                                var args = data.Substring(13, data.Length - 15).Split(',');
                                if ((args.Length == 2 ||
                                     (args.Length == 4 &&
                                      float.TryParse(args[2], out var d) &&
                                      int.TryParse(args[3], out var v) &&
                                      d >= 0 &&
                                      v >= 0)) &&
                                    int.TryParse(args[0], out var t) &&
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
                                var args = data.Substring(7, data.Length - 9).Split(',');
                                if (args.Length == 9 &&
                                    int.TryParse(args[0], out var t) &&
                                    float.TryParse(args[1], out _) &&
                                    float.TryParse(args[2], out _) &&
                                    float.TryParse(args[3], out _) &&
                                    float.TryParse(args[4], out _) &&
                                    float.TryParse(args[5], out _) &&
                                    float.TryParse(args[6], out _) &&
                                    CheckCameraMotion(args[7]) &&
                                    int.TryParse(args[8], out var d) &&
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
            arcs.Sort((a, b) =>
            {
                var result = a.Timing.CompareTo(b.Timing);
                if (result == 0)
                {
                    result = a.EndTiming.CompareTo(b.EndTiming);
                }

                return result;
            });
            for (int i = 0, count = arcs.Count; i < count; ++i)
            {
                var arc = arcs[i];
                for (var j = i + 1; j < count; ++j)
                {
                    var next = arcs[j];
                    if (next.Timing >= arc.EndTiming + 10)
                    {
                        break;
                    }

                    if (next.Timing <= arc.EndTiming - 10)
                    {
                        continue;
                    }

                    if (next.HasHead && arc.EndY == next.StartY && Math.Abs(next.StartX - arc.EndX) < 0.1)
                    {
                        next.HasHead = false;
                    }
                }
            }

            foreach (var group in groups)
            {
                note += group.NoteCount;
            }
        }

        private static bool CheckArcCurve(string arg)
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

        private static bool CheckSceneCtrlFx(string arg)
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

        private static bool CheckCameraMotion(string arg)
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