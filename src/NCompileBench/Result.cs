using System;

namespace NCompileBench
{
    public class Result
    {
        public DateTimeOffset BenchmarkDate { get; set; } = DateTimeOffset.UtcNow;
        public HardwareInfo HardwareInfo { get; set; }
        public int Score { get; set; }
        public int SingleCoreScore { get; set; }

        public Result(HardwareInfo hardwareInfo, int score, int singleCoreScore)
        {
            HardwareInfo = hardwareInfo;
            Score = score;
            SingleCoreScore = singleCoreScore;
        }
    }
}
