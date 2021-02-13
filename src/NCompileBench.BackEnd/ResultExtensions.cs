using System;
using System.Globalization;
using NCompileBench.Shared;

namespace NCompileBench.BackEnd
{
    public static class ResultExtensions
    {
        public static string ToFileName(this Result scoreResult)
        {
            var systemText = $"{scoreResult.HardwareInfo.Model}";

            if (string.IsNullOrWhiteSpace(systemText))
            {
                systemText = scoreResult.HardwareInfo.SystemFamily;
            }

            var cpuText =
                $"{scoreResult.HardwareInfo.Cpu.Name}_{scoreResult.HardwareInfo.Cpu.Count}_{scoreResult.HardwareInfo.Cpu.NumberOfCores}_{scoreResult.HardwareInfo.Cpu.NumberOfLogicalProcessors}";

            var filename =
                $"result_{-100000000+scoreResult.Score:0000000000}_{-100000000+scoreResult.SingleCoreScore:0000000000}_{scoreResult.BenchmarkDate.ToString(CultureInfo.InvariantCulture)}_{systemText}_{cpuText}_{scoreResult.Id.ToString().Replace("-", "")}.json";
            var result = Uri.EscapeDataString(filename);

            return result;
        }

        public static ResultSummary ToResultSummary(this string fileName)
        {   
            var result = new ResultSummary();
            var unescapedFileName = Uri.UnescapeDataString(fileName);
            unescapedFileName = unescapedFileName.Replace(".json", "");
            
            var parts = unescapedFileName.Split('_');

            result.Score = 100000000 - Convert.ToInt32(parts[1].TrimStart('-').TrimStart('0'));
            result.SingleCoreScore = 100000000- Convert.ToInt32(parts[2].TrimStart('-').TrimStart('0'));
            result.BenchmarkDate =DateTimeOffset.Parse(parts[3], CultureInfo.InvariantCulture);
            result.System = parts[4];
            result.CpuName = parts[5];
            result.CpuCount = Convert.ToInt32(parts[6]);
            result.CoreCount = Convert.ToInt32(parts[7]);
            result.LogicalCoreCount = Convert.ToInt32(parts[8]);
            result.Id = Guid.ParseExact(parts[9], "N");
            result.ResultFileName = fileName;
            
            return result;
        }
    }
}
