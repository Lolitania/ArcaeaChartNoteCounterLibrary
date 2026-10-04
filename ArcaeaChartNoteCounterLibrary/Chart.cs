using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Moe.Lowiro.Arcaea
{
    public partial class Chart
    {
        public static int CountNote(string path, bool specialGreen = false) =>
            new Chart(new StringReader(File.ReadAllText(path, encoding)), specialGreen).note;

        public static int CountNote(Stream stream, bool specialGreen = false)
        {
            var bytes = new byte[stream.Length];
            _ = stream.Read(bytes, 0, bytes.Length);
            return new Chart(new StringReader(encoding.GetString(bytes)), specialGreen).note;
        }

        private static readonly UTF8Encoding encoding = new(false);
        private readonly int note;

        /// <param name="specialGreen">Green arcs are not counted in "yourbestnightmare_3".</param>
        private Chart(StringReader reader, bool specialGreen)
        {
            var header = true;
            var lineCount = 1;
            var tpdf = 1f; // TimingPointDensityFactor
            Group mainGroup = null;
            Group currGroup = null;
            var groups = new List<Group>();
            var arcs = new List<Arc>();
            while (reader.Peek() != -1)
            {
                var line = reader.ReadLine().Replace(" ", string.Empty);
                if (line.Length > 0)
                {
                    if (header)
                    {
                        if (line.StartsWith("AudioOffset:"))
                        {
                            if (!int.TryParse(line.AsSpan(12), out _))
                            {
                                throw new ChartFormatException(ChartErrorType.AudioOffset, lineCount);
                            }
                        }
                        else if (line.StartsWith("TimingPointDensityFactor:"))
                        {
                            if (!float.TryParse(line.AsSpan(25), out tpdf))
                            {
                                throw new ChartFormatException(ChartErrorType.TimingPointDensityFactor, lineCount);
                            }
                        }
                        else if (line == "-")
                        {
                            header = false;
                            mainGroup = new Group(tpdf);
                            currGroup = mainGroup;
                        }
                        else if (line.StartsWith("timing("))
                        {
                            throw new ChartFormatException(ChartErrorType.Delimiter, lineCount);
                        }
                        else
                        {
                            throw new ChartFormatException(ChartErrorType.FileFormat, lineCount);
                        }
                    }
                    else
                    {
                        if (line.Length == 0) { }
                        else if (line.StartsWith("timinggroup(") && line.EndsWith("){"))
                        {
                            if (currGroup != mainGroup)
                            {
                                throw new ChartFormatException(ChartErrorType.TimingGroup, lineCount);
                            }

                            var allowInput = true;
                            foreach (var arg in line.Substring(12, line.Length - 14).Split('_'))
                            {
                                switch (arg)
                                {
                                case "":
                                case "fadingholds":
                                    break;
                                case "noinput":
                                    allowInput = false;
                                    break;
                                default:
                                    if ((!arg.StartsWith("anglex") || !int.TryParse(arg.AsSpan(6), out _)) &&
                                        (!arg.StartsWith("angley") || !int.TryParse(arg.AsSpan(6), out _)) &&
                                        !TraceColRegex().IsMatch(arg))
                                    {
                                        throw new ChartFormatException(ChartErrorType.TimingGroup, lineCount);
                                    }

                                    break;
                                }
                            }

                            currGroup = new Group(tpdf, allowInput);
                        }
                        else if (line == "};")
                        {
                            if (currGroup == mainGroup)
                            {
                                throw new ChartFormatException(ChartErrorType.TimingGroup, lineCount);
                            }

                            currGroup.Preprocess();
                            groups.Add(currGroup);
                            currGroup = mainGroup;
                        }
                        else if (line.StartsWith("timing(") && line.EndsWith(");"))
                        {
                            if (line.Length < 14)
                            {
                                throw new ChartFormatException(ChartErrorType.Timing, lineCount);
                            }

                            var args = line.Substring(7, line.Length - 9).Split(',');
                            if (args.Length != 3 ||
                                !int.TryParse(args[0], out var timing) ||
                                !float.TryParse(args[1], out var bpm) ||
                                !float.TryParse(args[2], out var bpl) ||
                                timing < 0 ||
                                (bpm != 0 && bpl == 0))
                            {
                                throw new ChartFormatException(ChartErrorType.Timing, lineCount);
                            }

                            currGroup.Add(new Timing(timing, Math.Abs(bpm)));
                        }
                        else if (line[0] == '(' && line.EndsWith(");"))
                        {
                            if (line.Length < 6)
                            {
                                throw new ChartFormatException(ChartErrorType.Tap, lineCount);
                            }

                            var args = line.Substring(1, line.Length - 3).Split(',');
                            if (args.Length != 2 ||
                                !int.TryParse(args[0], out var t) ||
                                !float.TryParse(args[1], out _) ||
                                t < 0)
                            {
                                throw new ChartFormatException(ChartErrorType.Tap, lineCount);
                            }

                            currGroup.Add();
                        }
                        else if (line.StartsWith("hold(") && line.EndsWith(");"))
                        {
                            if (line.Length < 12)
                            {
                                throw new ChartFormatException(ChartErrorType.Hold, lineCount);
                            }

                            var args = line.Substring(5, line.Length - 7).Split(',');
                            if (args.Length != 3 ||
                                !int.TryParse(args[0], out var timingStart) ||
                                !int.TryParse(args[1], out var timingEnd) ||
                                !float.TryParse(args[2], out _) ||
                                timingStart < 0 ||
                                timingEnd < timingStart)
                            {
                                throw new ChartFormatException(ChartErrorType.Hold, lineCount);
                            }

                            currGroup.Add(new LongObject(timingStart, timingEnd));
                        }
                        else if (line.StartsWith("arc(") && line[^1] == ';')
                        {
                            string appendant = null;
                            {
                                var bracketStart = line.IndexOf('[');
                                var bracketEnd = line.IndexOf(']');
                                if (bracketStart >= 30 && bracketStart < bracketEnd)
                                {
                                    appendant = line.Substring(bracketStart + 1, bracketEnd - bracketStart - 1);
                                    line = line.Remove(bracketStart, bracketEnd - bracketStart + 1);
                                }
                            }

                            if (line.Length < 31)
                            {
                                throw new ChartFormatException(ChartErrorType.Arc, lineCount);
                            }

                            var args = line.Substring(4, line.Length - 6).Split(',');
                            if (args.Length is 10 or 11)
                            {
                                var status = args[9] switch
                                {
                                    "false"     => appendant is null ? ArcStatus.Normal : ArcStatus.TraceWithTap,
                                    "true"      => appendant is null ? ArcStatus.Trace : ArcStatus.TraceWithTap,
                                    "designant" => appendant is null ? ArcStatus.Designant : ArcStatus.DesignantWithTap,
                                    _           => ArcStatus.Unknown
                                };

                                if (!int.TryParse(args[0], out var timingStart) ||
                                    !int.TryParse(args[1], out var timingEnd) ||
                                    !float.TryParse(args[2], out var xStart) ||
                                    !float.TryParse(args[3], out var xEnd) ||
                                    !CheckArcCurve(args[4]) ||
                                    !float.TryParse(args[5], out var yStart) ||
                                    !float.TryParse(args[6], out var yEnd) ||
                                    !int.TryParse(args[7], out var colour) ||
                                    (args.Length != 10 && !float.TryParse(args[10], out _)) ||
                                    status == ArcStatus.Unknown ||
                                    timingStart < 0 ||
                                    timingEnd < 0 ||
                                    (status == ArcStatus.Normal && (timingStart > timingEnd || colour is < 0 or > 3)))
                                {
                                    throw new ChartFormatException(ChartErrorType.Arc, lineCount);
                                }

                                switch (status)
                                {
                                case ArcStatus.Normal:
                                    switch (colour)
                                    {
                                    case 2 when specialGreen:
                                        break;
                                    case 3 when timingStart == timingEnd:
                                        currGroup.Add();
                                        break;
                                    default:
                                        {
                                            var arc = new Arc(timingStart, timingEnd, xStart, xEnd, yStart, yEnd);
                                            currGroup.Add(arc);
                                            arcs.Add(arc);
                                            break;
                                        }
                                    }

                                    break;
                                case ArcStatus.TraceWithTap:
                                    foreach (var cmd in appendant.Split(','))
                                    {
                                        if (cmd.Length < 9 ||
                                            !cmd.StartsWith("arctap(") ||
                                            cmd[^1] != ')' ||
                                            !int.TryParse(cmd.AsSpan(7, cmd.Length - 8), out _))
                                        {
                                            throw new ChartFormatException(ChartErrorType.ArcTap, lineCount);
                                        }

                                        currGroup.Add();
                                    }

                                    break;
                                }
                            }
                        }
                        else if (line.StartsWith("scenecontrol(") && line.EndsWith(");"))
                        {
                            if (line.Length < 24)
                            {
                                throw new ChartFormatException(ChartErrorType.SceneControl, lineCount);
                            }

                            var args = line.Substring(13, line.Length - 15).Split(',');
                            if ((
                                    args.Length != 2 &&
                                    (
                                        args.Length != 4 ||
                                        !float.TryParse(args[2], out var duration) ||
                                        !int.TryParse(args[3], out var value) ||
                                        !(duration >= 0) ||
                                        value < 0
                                    )
                                ) ||
                                !int.TryParse(args[0], out var timing) ||
                                timing < 0 ||
                                !CheckSceneCtrlFx(args[1]))
                            {
                                throw new ChartFormatException(ChartErrorType.SceneControl, lineCount);
                            }
                        }
                        else if (line.StartsWith("camera(") && line.EndsWith(");"))
                        {
                            if (line.Length < 26)
                            {
                                throw new ChartFormatException(ChartErrorType.Camera, lineCount);
                            }

                            var args = line.Substring(7, line.Length - 9).Split(',');
                            if (args.Length != 9 ||
                                !int.TryParse(args[0], out var timing) ||
                                !float.TryParse(args[1], out _) ||
                                !float.TryParse(args[2], out _) ||
                                !float.TryParse(args[3], out _) ||
                                !float.TryParse(args[4], out _) ||
                                !float.TryParse(args[5], out _) ||
                                !float.TryParse(args[6], out _) ||
                                !CheckCameraMotion(args[7]) ||
                                !int.TryParse(args[8], out var duration) ||
                                timing < 0 ||
                                duration < 0)
                            {
                                throw new ChartFormatException(ChartErrorType.Camera, lineCount);
                            }
                        }
                        else
                        {
                            throw new ChartFormatException(ChartErrorType.Unknown, lineCount);
                        }
                    }
                }

                ++lineCount;
            }

            if (mainGroup is null)
            {
                throw new ChartFormatException(ChartErrorType.FileFormat);
            }

            if (currGroup != mainGroup)
            {
                throw new ChartFormatException(ChartErrorType.TimingGroup, lineCount);
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

                    if (next.HasHead && arc.YEnd == next.YStart && Math.Abs(next.XStart - arc.XEnd) < 0.1)
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

        private static bool CheckArcCurve(string arg) => arg switch
        {
            "b" or "s" or "si" or "so" or "sisi" or "siso" or "sosi" or "soso" => true,
            _                                                                  => false
        };

        private static bool CheckSceneCtrlFx(string arg) => arg switch
        {
            "arcahvdebris" or "arcahvdistort" or "hidegroup" or "redline" or "trackdisplay" or "trackhide"
                or "trackshow" or "enwidencamera" or "enwidenlanes" => true,
            _ => false
        };

        private static bool CheckCameraMotion(string arg) => arg switch
        {
            "l" or "reset" or "s" or "qi" or "qo" => true,
            _                                     => false
        };

        [GeneratedRegex("tracecol[0-9a-fA-F]{6}")]
        private static partial Regex TraceColRegex();
    }
}