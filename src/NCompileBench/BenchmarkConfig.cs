using System.Collections;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

namespace NCompileBench
{
    public static class BenchmarkConfig
    {
        public static IConfig Create(
            DirectoryInfo artifactsPath)
        {
            var job = Job.Default
                // .WithWarmupCount(1)
                // .WithIterationTime(TimeInterval
                //     .FromMilliseconds(250))
                // // .WithMinIterationCount(1)
                // // .WithMaxIterationCount(2)
                // .WithWarmupCount(1)
                // .WithIterationCount(1)
                .WithRuntime(CoreRuntime.Core31)
                .WithPlatform(Platform.X64)
                .WithMaxRelativeError(0.01)
                .DontEnforcePowerPlan();

            // See https://github.com/dotnet/roslyn/issues/42393
            job = job.WithArguments(new Argument[] {new MsBuildArgument("/p:DebugType=portable")});

            var config = DefaultConfig.Instance
                .AddJob(job.AsDefault())
                .WithOption(ConfigOptions.DisableOptimizationsValidator, true)
                .WithOption(ConfigOptions.DisableLogFile, true)
                .WithArtifactsPath(artifactsPath.FullName)
                .AddDiagnoser(MemoryDiagnoser.Default)
                .AddExporter(JsonExporter.Full)
                .AddColumn(StatisticColumn.Median, StatisticColumn.Min, StatisticColumn.Max)
                .WithSummaryStyle(SummaryStyle.Default
                    .WithMaxParameterColumnWidth(
                        36));

            var loggersField = typeof(ManualConfig).GetField("loggers",
                BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);

            var loggers = (IList) loggersField.GetValue(config);
            loggers.Clear();

            config.AddLogger(new EmptyLogger());
            return config;
        }
    }
}
