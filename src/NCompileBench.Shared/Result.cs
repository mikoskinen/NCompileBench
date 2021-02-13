using System;

namespace NCompileBench.Shared
{
    public class Result
    {
        public DateTimeOffset BenchmarkDate { get; set; }=  DateTimeOffset.UtcNow;
        public Guid Id { get; set; } = Guid.NewGuid();
        public HardwareInfo HardwareInfo { get; }
        public int Score { get; }
        public int SingleCoreScore { get; }

        public Result(HardwareInfo hardwareInfo, int score, int singleCoreScore)
        {
            HardwareInfo = hardwareInfo;
            Score = score;
            SingleCoreScore = singleCoreScore;
        }
    }
}
