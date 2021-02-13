using System;

namespace NCompileBench.Shared
{
    public class ResultSummary
    {
        public int Score { get; set; }
        public int SingleCoreScore { get; set; }
        public string System { get; set; }
        public string CpuName { get; set; }
        public int CpuCount { get; set; }
        public int CoreCount { get; set; }
        public int LogicalCoreCount { get; set; }
        
        public DateTimeOffset BenchmarkDate { get; set; }=  DateTimeOffset.UtcNow;
        public Guid Id { get; set; } = Guid.NewGuid();
        public string ResultFileName { get; set; }
    }
}
