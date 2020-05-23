using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using Newtonsoft.Json;
using Perfolizer.Horology;

namespace NCompileBench
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            try
            {
                var fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

                Console.WriteLine($"Initializing NCompileBench. Version: {fileVersionInfo.ProductVersion}");
                Console.WriteLine($"Run with -silent flag to see less details during the benchmark");
                Console.WriteLine($"Run with -submit to submit your latest benchmark");
                Console.WriteLine($"Run with -scores to view the online results without running the benchmark");
                Console.WriteLine("****");
                Console.WriteLine($"Created by Mikael Koskinen: https://mikaelkoskinen.net");
                Console.WriteLine($"Source code available from https://github.com/mikoskinen/NCompileBench (MIT)");
                Console.WriteLine($"Based on .NET Performance repository: https://github.com/dotnet/performance by Microsoft (MIT)");
                Console.WriteLine($"Uses BenchmarkDotNet: https://github.com/dotnet/BenchmarkDotNet (MIT)");
                Console.WriteLine($"Compiles source code available from https://github.com/dotnet/roslyn/releases/tag/perf-assets-v1");
                Console.WriteLine("****");

                var directoryName = Path.GetDirectoryName(typeof(Program).Assembly.Location);
                var resultDirectory = Path.Combine(directoryName, "results");

                var submit = args?.Any(x => string.Equals(x, "-submit", StringComparison.InvariantCultureIgnoreCase)) == true;
                if (submit)
                {
                    await HandleSubmit(resultDirectory);
                    return;
                }
            
                var scores = args?.Any(x => string.Equals(x, "-scores", StringComparison.InvariantCultureIgnoreCase)) == true;
                if (scores)
                {
                    await HandleScores();
                    return;
                }

                Console.WriteLine("Setting up the benchmark.");

                await Setup();

                Console.WriteLine("Starting benchmark in 5 seconds. Please sit tight, this may take up-to 10 minutes.");

                await Task.Delay(TimeSpan.FromSeconds(5));
            
                var silent = args?.Any(x => string.Equals(x, "-silent", StringComparison.InvariantCultureIgnoreCase)) == true;

                var cts = new CancellationTokenSource();

                if (silent)
                {
                    ThreadPool.QueueUserWorkItem(Spin, cts.Token);
                }

                var artifactsPath = new DirectoryInfo(directoryName);
                var config = BenchmarkConfig.Create(artifactsPath, silent);

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
                Console.WriteLine($"More detailed results are available from {resultDirectory}");

                Console.WriteLine();
                if (CreateScore(scoreMulti, scoreSingle, resultDirectory))
                {
                    Console.WriteLine($"You can submit your benchmark by running ncompilebench -submit");
                    Console.WriteLine();
                }
            }
            finally
            {
                Console.WriteLine($"Thank you for running NCompileBench!");
            }
        }

        private static async Task HandleScores()
        {
            await DisplayComparisons();
        }

        private static async Task HandleSubmit(string resultDirectory)
        {
            try
            {
                var lastScore = GetLastScore(resultDirectory);

                if (string.IsNullOrWhiteSpace(lastScore))
                {
                    Console.WriteLine("Couldn't find score. Please re-run the benchmark");

                    return;
                }

                var content = new StringBuilder();
                content.AppendLine("You can submit your score by copy-pasting it as a comment into the following Gist:");
                content.AppendLine("https://gist.github.com/mikoskinen/2560a85bc59ef6baad20d371ab0db6f2#file-ncompilebench-json-results");
                content.AppendLine();
                content.Append(lastScore);

                var submitFile = Path.Combine(resultDirectory, "submit.txt");
                await File.WriteAllTextAsync(submitFile, content.ToString());
                
                var process = Process.Start(new ProcessStartInfo(submitFile) { UseShellExecute = true });

                Console.WriteLine("NCompileBench is automatically closed after you close the text editor");

                process?.WaitForExit();
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to handle submitting: " + e);
            }
        }

        private static bool CreateScore(int scoreMulti, int scoreSingle, string resultDirectory)
        {
            try
            {
                var hwInfo = HardwareInfo.Get();
                var result = new Result(hwInfo, scoreMulti, scoreSingle);

                var json = JsonConvert.SerializeObject(result, Formatting.Indented);
                var dateTime = DateTime.UtcNow;
                var resultFile = Path.Combine(resultDirectory, $"result_{dateTime:yyyyMMdd}_{dateTime.Ticks}.json");
                
                File.WriteAllText(resultFile, json);

                return true;
            }
            catch (Exception)
            {
                // ignored
            }

            return false;
        }

        private static string GetLastScore(string resultDirectory)
        {
            try
            {
                var files = new DirectoryInfo(resultDirectory).GetFiles("result*.json");
                var latestFile = files.OrderByDescending(x => x.CreationTime).FirstOrDefault();

                if (latestFile == null)
                {
                    return null;
                }

                return File.ReadAllText(latestFile.FullName);
            }
            catch (Exception)
            {
                // ignored
            }

            return null;
        }

        private static async Task DisplayComparisons()
        {
            try
            {
                var comparisonUrl =
                    $"https://gist.githubusercontent.com/mikoskinen/2560a85bc59ef6baad20d371ab0db6f2/raw/NCompileBench%2520Json%2520Results?time={DateTime.Now.Ticks}";
                var content = await new HttpClient().GetStringAsync(comparisonUrl);

                if (string.IsNullOrWhiteSpace(content))
                {
                    return;
                }

                Console.WriteLine("****");
                Console.WriteLine("Comparison results:");

                var results = JsonConvert.DeserializeObject<List<Result>>(content);

                var table = new ConsoleTable.Table();
                table.SetHeaders("System", "CPU", "Score");

                foreach (var result in results)
                {
                    var systemText = $"{result.HardwareInfo.SystemFamily} {result.HardwareInfo.SystemSku}";
                    var cpuText = $"{result.HardwareInfo.Cpu.Name}, {result.HardwareInfo.Cpu.Count} CPU, {result.HardwareInfo.Cpu.NumberOfLogicalProcessors} logical and {result.HardwareInfo.Cpu.NumberOfCores} physical cores";

                    table.AddRow(systemText, cpuText, $"{result.Score} ({result.SingleCoreScore})");
                }
                
                Console.WriteLine(table);
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
