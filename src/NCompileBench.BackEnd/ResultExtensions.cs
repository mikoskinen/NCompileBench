using System;
using System.Globalization;
using NCompileBench.Shared;

namespace NCompileBench.Backend
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

            // We want to order the blobs in descending order by score to make it easier to fetch highest scores.
            // The code below tries to make sure that the higher the score, the "smaller" the filename is.
            var filename =
                $"result_{scoreResult.Platform}_{scoreResult.Runtime}_{-100000000+scoreResult.Score:0000000000}_{-100000000+scoreResult.SingleCoreScore:0000000000}_{scoreResult.BenchmarkDate.ToString(CultureInfo.InvariantCulture)}_{systemText}_{cpuText}_{scoreResult.Id.ToString().Replace("-", "")}.json";
            var result = Uri.EscapeDataString(filename);

            return result;
        }

        public static ResultSummary ToResultSummary(this string fileName)
        {   
            var result = new ResultSummary();
            var unescapedFileName = Uri.UnescapeDataString(fileName);
            unescapedFileName = unescapedFileName.Replace(".json", "");
            
            var parts = unescapedFileName.Split('_');

            result.Platform = parts[1];
            result.Runtime = parts[2];
            result.Score = 100000000 - Convert.ToInt32(parts[3].TrimStart('-').TrimStart('0'));
            result.SingleCoreScore = 100000000- Convert.ToInt32(parts[4].TrimStart('-').TrimStart('0'));
            result.BenchmarkDate =DateTimeOffset.Parse(parts[5], CultureInfo.InvariantCulture);
            result.System = parts[6];
            result.CpuName = parts[7];
            result.CpuCount = Convert.ToInt32(parts[8]);
            result.CoreCount = Convert.ToInt32(parts[9]);
            result.LogicalCoreCount = Convert.ToInt32(parts[10]);
            result.Id = Guid.ParseExact(parts[11], "N");
            result.ResultFileName = fileName;
            
            return result;
        }
    }
}
