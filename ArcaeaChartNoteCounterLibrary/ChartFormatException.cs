using System;

namespace Moe.Lowiro.Arcaea
{
    public class ChartFormatException : Exception
    {
        public override string ToString() => message;

        public ChartErrorType Type { get; }

        public int Line { get; }

        public ChartFormatException(ChartErrorType type, int line = 0)
        {
            Type = type;
            Line = line;
            switch (type)
            {
            case ChartErrorType.FileFormat: message = $"This is not a chart file."; break;
            case ChartErrorType.AudioOffset: message = $"Line {line}: Invalid AudioOffset value."; break;
            case ChartErrorType.TimingPointDensityFactor: message = $"Line {line}: Invalid TimingPointDensityFactor value."; break;
            case ChartErrorType.Delimiter: message = $"Line {line}: A delimiter is needed before event definitions."; break;
            case ChartErrorType.Timing: message = $"Line {line}: Timing event parameter error."; break;
            case ChartErrorType.Tap: message = $"Line {line}: Tap event parameter error."; break;
            case ChartErrorType.Hold: message = $"Line {line}: Hold event parameter error."; break;
            case ChartErrorType.Arc: message = $"Line {line}: Arc event parameter error."; break;
            case ChartErrorType.ArcTap: message = $"Line {line}: ArcTap event parameter error."; break;
            case ChartErrorType.Camera: message = $"Line {line}: Camera event parameter error."; break;
            case ChartErrorType.SceneControl: message = $"Line {line}: SceneControl event parameter error."; break;
            case ChartErrorType.TimingGroup: message = $"Line {line}: Invalid TimingGroup format."; break;
            default: message = $"Line {line}: Unknown event or data."; break;
            }
        }

        private readonly string message;
    }
}