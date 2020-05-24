using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace NCompileBench
{
    public static class BenchmarkConfig
    {
        public static IConfig Create(DirectoryInfo artifactsPath, bool verbose, bool noMaxIterations)
        {
            var job = Job.Default
                .WithRuntime(CoreRuntime.Core31)
                .WithPlatform(Platform.X64)
                .WithMaxRelativeError(0.1)
                .WithToolchain(new InProcessEmitToolchain(TimeSpan.FromHours(3), true))
                .DontEnforcePowerPlan();

            if (noMaxIterations == false)
            {
                job = job.WithMaxIterationCount(20).WithMaxWarmupCount(7);
            }

            if (Debugger.IsAttached)
            {
                job = job.WithIterationCount(1).WithWarmupCount(1);
            }
            
            // See https://github.com/dotnet/roslyn/issues/42393
            job = job.WithArguments(new Argument[] {new MsBuildArgument("/p:DebugType=portable")});

            var config = DefaultConfig.Instance
                .AddJob(job.AsDefault())
                .WithOption(ConfigOptions.DisableOptimizationsValidator, true)
                .WithOption(ConfigOptions.DisableLogFile, true)
                .WithArtifactsPath(artifactsPath.FullName)
                .AddExporter(JsonExporter.Full)
                .AddColumn(StatisticColumn.Median, StatisticColumn.Min, StatisticColumn.Max)
                .WithSummaryStyle(SummaryStyle.Default
                    .WithMaxParameterColumnWidth(
                        36));

            if (verbose == false)
            {
                var loggersField = typeof(ManualConfig).GetField("loggers",
                    BindingFlags.NonPublic | BindingFlags.GetField | BindingFlags.Instance);

                var loggers = (IList) loggersField.GetValue(config);
                loggers.Clear();

                config.AddLogger(new EmptyLogger());
            }
            
            return config;
        }
    }
}
