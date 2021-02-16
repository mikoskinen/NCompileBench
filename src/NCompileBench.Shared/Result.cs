using System;

namespace NCompileBench.Shared
{
    public class Result
    {
        public DateTimeOffset BenchmarkDate { get; set; }=  DateTimeOffset.UtcNow;
        public Guid Id { get; set; } = Guid.NewGuid();
        public HardwareInfo HardwareInfo { get; }
        public string Platform { get; set; }
        public string Runtime { get; set; }
        public int Score { get; }
        public int SingleCoreScore { get; }

        public Result(HardwareInfo hardwareInfo, string platform, string runtime, int score, int singleCoreScore)
        {
            HardwareInfo = hardwareInfo;
            Platform = platform;
            Runtime = runtime;
            Score = score;
            SingleCoreScore = singleCoreScore;
        }
    }
}
