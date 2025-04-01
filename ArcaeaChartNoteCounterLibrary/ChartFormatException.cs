using System;

namespace Moe.Lowiro.Arcaea
{
    public class ChartFormatException(ChartErrorType type, int line = 0) : Exception
    {
        public override string ToString() => message;

        public ChartErrorType Type { get; } = type;

        public int Line { get; } = line;

        private readonly string message = type switch
        {
            ChartErrorType.FileFormat               => $"This is not a chart file.",
            ChartErrorType.AudioOffset              => $"Line {line}: Invalid AudioOffset value.",
            ChartErrorType.TimingPointDensityFactor => $"Line {line}: Invalid TimingPointDensityFactor value.",
            ChartErrorType.Delimiter                => $"Line {line}: A delimiter is needed before event definitions.",
            ChartErrorType.Timing                   => $"Line {line}: Timing event parameter error.",
            ChartErrorType.Tap                      => $"Line {line}: Tap event parameter error.",
            ChartErrorType.Hold                     => $"Line {line}: Hold event parameter error.",
            ChartErrorType.Arc                      => $"Line {line}: Arc event parameter error.",
            ChartErrorType.ArcTap                   => $"Line {line}: ArcTap event parameter error.",
            ChartErrorType.Camera                   => $"Line {line}: Camera event parameter error.",
            ChartErrorType.SceneControl             => $"Line {line}: SceneControl event parameter error.",
            ChartErrorType.TimingGroup              => $"Line {line}: Invalid TimingGroup format.",
            _                                       => $"Line {line}: Unknown event or data."
        };
    }
}