using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;

namespace NCompileBench
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

            Console.WriteLine($"Initializing NCompileBench. Version: {fileVersionInfo.ProductVersion}");
            Console.WriteLine($"Run with -verbose flag to see more details during the benchmark");
            Console.WriteLine("****");
            Console.WriteLine($"Created by Mikael Koskinen: https://mikaelkoskinen.net");
            Console.WriteLine($"Source code available from https://github.com/mikoskinen/NCompileBench (MIT)");
            Console.WriteLine($"Based on .NET Performance repository: https://github.com/dotnet/performance by Microsoft (MIT)");
            Console.WriteLine($"Uses BenchmarkDotNet: https://github.com/dotnet/BenchmarkDotNet (MIT)");
            Console.WriteLine($"Compiles source code available from https://github.com/dotnet/roslyn/releases/tag/perf-assets-v1");
            Console.WriteLine("****");

            await Setup();

            Console.WriteLine("Starting benchmark. Please sit tight, this may take up-to 10 minutes.");

            var verbose = args?.Any(x => string.Equals(x, "-verbose", StringComparison.InvariantCultureIgnoreCase)) == true;

            var cts = new CancellationTokenSource();

            if (!verbose)
            {
                ThreadPool.QueueUserWorkItem(Spin, cts.Token);
            }

            var directoryName = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            var artifactsPath = new DirectoryInfo(directoryName);

            var config = BenchmarkConfig.Create(artifactsPath, verbose);
            
            var summary = BenchmarkRunner.Run<CompilationBenchmarks>(config);

            cts.Cancel();

            await Task.Delay(TimeSpan.FromMilliseconds(750));

            Console.WriteLine("****");

            var nonConcurrentResult = summary.Reports.Single(x => (bool) x.BenchmarkCase.Parameters[0].Value == false)
                .ResultStatistics;
            var concurrentResult = summary.Reports.Single(x => (bool) x.BenchmarkCase.Parameters[0].Value == true)
                .ResultStatistics;
            
            var nonConcurrentTimespan = TimeSpan.FromMilliseconds(TimeUnit.Convert(nonConcurrentResult.Mean, TimeUnit.Nanosecond, TimeUnit.Millisecond));
            var concurrentTimespan =
                TimeSpan.FromMilliseconds(TimeUnit.Convert(concurrentResult.Mean, TimeUnit.Nanosecond,
                    TimeUnit.Millisecond));

            var scoreMulti = CalculateScore(concurrentTimespan);
            var scoreSingle = CalculateScore(nonConcurrentTimespan);
            Console.WriteLine($"NCompileBench Score: {scoreMulti} (non-concurrent score: {scoreSingle})");
            await DisplayComparisons();
            Console.WriteLine("****");
            Console.WriteLine("System information:");
            Console.WriteLine(summary.HostEnvironmentInfo.OsVersion.Value);

            Console.WriteLine(
                $"{summary.HostEnvironmentInfo.CpuInfo.Value.ProcessorName}, {summary.HostEnvironmentInfo.CpuInfo.Value.PhysicalProcessorCount} CPU, {summary.HostEnvironmentInfo.CpuInfo.Value.LogicalCoreCount} logical and {summary.HostEnvironmentInfo.CpuInfo.Value.PhysicalCoreCount} physical cores");
            Console.WriteLine("****");
            Console.WriteLine($"More detailed results are available from {Path.Combine(directoryName, "results")}");
        }

        private static async Task DisplayComparisons()
        {
            try
            {
                var comparisonUrl =
                    $"https://gist.githubusercontent.com/mikoskinen/63785777734b5ef02a0fccb0106c1742/raw/NCompileBench%2520Results.txt?time={DateTime.Now.Ticks}";
                var content = await new HttpClient().GetStringAsync(comparisonUrl);

                if (string.IsNullOrWhiteSpace(content))
                {
                    return;
                }

                Console.WriteLine("****");
                Console.WriteLine("Comparison results:");

                foreach (var benchmarkLine in content.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var parts = benchmarkLine.Split("##", StringSplitOptions.RemoveEmptyEntries);
                    var cpu = Truncate(parts[0], 20);
                    var multiScore = int.Parse(parts[1]);
                    var singleScore = int.Parse(parts[2]);

                    Console.WriteLine($"{cpu.PadRight(20)}: {multiScore} ({singleScore})");
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private static int CalculateScore(TimeSpan duration)
        {
            return (int) Math.Round(TimeSpan.FromHours(1).TotalMilliseconds / duration.TotalMilliseconds,
                0, MidpointRounding.AwayFromZero);
        }

        private static async Task Setup()
        {
            var codePackage = Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location), "CodeAnalysisReproWithAnalyzers.zip");
            var sourceDownloadDir = Path.Combine(AppContext.BaseDirectory, "benchmarkedCodes");
            var sourceDir = Path.Combine(sourceDownloadDir, "CodeAnalysisReproWithAnalyzers");

            if (!Directory.Exists(sourceDir))
            {
                await FileTasks.Unzip(codePackage, sourceDownloadDir);
            }

            Environment.SetEnvironmentVariable(Helpers.TestProjectEnvVarName, sourceDir);
        }

        static void Spin(object obj)
        {
            var counter = 0;
            var token = (CancellationToken) obj;

            while (token.IsCancellationRequested == false)
            {
                counter++;

                Console.SetCursorPosition(0, Console.CursorTop);

                switch (counter % 3)
                {
                    case 0:
                        Console.Write(".   ");
                        counter = 0;

                        break;
                    case 1:
                        Console.Write("..  ");

                        break;
                    case 2:
                        Console.Write("... ");

                        break;
                    case 3:
                        Console.Write("....");

                        break;
                }

                Console.SetCursorPosition(0, Console.CursorTop);

                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }
        }

        private static string Truncate(string value, int maxChars)
        {
            return value.Length <= maxChars ? value : value.Substring(0, maxChars) + "...";
        }
    }
}
