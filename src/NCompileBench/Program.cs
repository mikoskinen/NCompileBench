﻿using System;
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
using CloudNative.CloudEvents;
using NCompileBench.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Perfolizer.Horology;
using Weikio.EventFramework.EventCreator;

namespace NCompileBench
{
    public class Program
    {
        private static FileVersionInfo _fileVersionInfo;

        public static async Task Main(string[] args)
        {
            try
            {
                _fileVersionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

                Console.WriteLine($"Initializing NCompileBench. Version: {_fileVersionInfo.ProductVersion}");
                Console.WriteLine($"Run with -verbose flag to see more details during the benchmark");
                Console.WriteLine($"Run with -submit to submit your latest benchmark");
                Console.WriteLine($"Run with -autosubmit to automatically submit your latest benchmark. If used together with -submit, previous benchmark result is submitted");
                Console.WriteLine($"Run with -scores to view the online results without running the benchmark");
                Console.WriteLine($"Run with -nomaxiterations to get even more accurate results. Please view readme for more details");
                Console.WriteLine("****");
                Console.WriteLine($"Results and help available from: https://www.ncompilebench.io");
                Console.WriteLine($"Created by Mikael Koskinen: https://mikaelkoskinen.net");
                Console.WriteLine($"Source code available from https://github.com/mikoskinen/NCompileBench (MIT)");
                Console.WriteLine($"Based on .NET Performance repository: https://github.com/dotnet/performance by Microsoft (MIT)");
                Console.WriteLine($"Uses BenchmarkDotNet: https://github.com/dotnet/BenchmarkDotNet (MIT)");
                Console.WriteLine($"Compiles source code available from https://github.com/dotnet/roslyn/releases/tag/perf-assets-v1");
                Console.WriteLine("****");
                
                var directoryName = Path.GetDirectoryName(typeof(Program).Assembly.Location);
                var resultDirectory = Path.Combine(directoryName, "results");

                var submit = args?.Any(x => string.Equals(x, "-submit", StringComparison.InvariantCultureIgnoreCase)) == true;
                var autoSubmit = args?.Any(x => string.Equals(x, "-autosubmit", StringComparison.InvariantCultureIgnoreCase)) == true;

                if (submit)
                {
                    await HandleSubmit(resultDirectory, autoSubmit);

                    return;
                }

                var scores = args?.Any(x => string.Equals(x, "-scores", StringComparison.InvariantCultureIgnoreCase)) == true;

                if (scores)
                {
                    await HandleScores();

                    return;
                }

                Console.WriteLine("Setting up the benchmark");

                await Setup();

                Console.WriteLine("Starting benchmark in 5 seconds. Please sit tight, this may take up-to 10 minutes");

                await Task.Delay(TimeSpan.FromSeconds(5));

                var verbose = args?.Any(x => string.Equals(x, "-verbose", StringComparison.InvariantCultureIgnoreCase)) == true;
                var noMaxIterations = args?.Any(x => string.Equals(x, "-nomaxiterations", StringComparison.InvariantCultureIgnoreCase)) == true;

                var cts = new CancellationTokenSource();

                if (verbose == false)
                {
                    ThreadPool.QueueUserWorkItem(Spin, cts.Token);
                }

                var artifactsPath = new DirectoryInfo(directoryName);
                var config = BenchmarkConfig.Create(artifactsPath, verbose, noMaxIterations);

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
                    if (autoSubmit)
                    {
                        await HandleSubmit(resultDirectory, true);
                        return;
                    }
                    
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

        private static async Task HandleSubmit(string resultDirectory, bool autoSubmit)
        {
            try
            {
                var lastScore = GetLastScore(resultDirectory);

                if (string.IsNullOrWhiteSpace(lastScore))
                {
                    Console.WriteLine("Couldn't find score. Please re-run the benchmark");

                    return;
                }

                try
                {
                    var resultObject = JArray.Parse(lastScore);
                }
                catch (Exception)
                {
                    Console.WriteLine("The scores from the previous version aren't compatible with the latest version of NCompileBench. Please update to the latest version");

                    return;
                }

                if (autoSubmit == false)
                {
                    var content = new StringBuilder();
                    content.AppendLine("The following content will be submitted to NCompileBench database.");
                    content.AppendLine("You can cancel the submissions by closing the NCompileBench application before closing this text editor or by emptying and saving this file before closing the text editor.");
                    content.AppendLine();
                    content.Append(lastScore);

                    var submitFile = Path.Combine(resultDirectory, "submit.txt");
                    await File.WriteAllTextAsync(submitFile, content.ToString());

                    var process = Process.Start(new ProcessStartInfo(submitFile) { UseShellExecute = true });

                    Console.WriteLine("NCompileBench is automatically closed after you close the text editor");

                    process?.WaitForExit();

                    var updatedContent = await File.ReadAllTextAsync(submitFile);

                    if (string.IsNullOrWhiteSpace(updatedContent))
                    {
                        Console.WriteLine("Result file is empty, cancelling the submission");
                        return;
                    }
                }

                Console.WriteLine("Submitting the result, please wait...");
                await DoSubmit(lastScore);

                Console.WriteLine("Result submitted");
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to handle submitting: " + e);
            }
        }

        private static async Task DoSubmit(string lastScore)
        {
            var url = "https://ncompilebench.azurewebsites.net/api/events";

            if (Environment.GetEnvironmentVariable("NCOMPILEBENCH_BACKEND") != null)
            {
                url = Environment.GetEnvironmentVariable("NCOMPILEBENCH_BACKEND");
            }
            
            try
            {
                var stringContent = new StringContent(lastScore, Encoding.UTF8, "application/cloudevents+json");
                var client = new HttpClient();

                await client.PostAsync(url, stringContent);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to submit score to Url: {url}. Backend may be down. Please try again later. Exception details:{Environment.NewLine}{e}");
            }
        }

        private static bool CreateScore(int scoreMulti, int scoreSingle, string resultDirectory)
        {
            try
            {
                var hwInfo = HardwareInfoProvider.Get();
                
                // Platform and runtime are currently hard coded in the benchmark
                var platformResult = new Result(hwInfo, "X64", "netcoreapp3.1", scoreMulti, scoreSingle);

                // But in the future we may want to run the benchmark for multiple platforms (X64, ARM)
                // Use List of results when transferring data between the app and the backend
                var result = new List<Result>() { platformResult };
                var json = JsonConvert.SerializeObject(result, Formatting.Indented);
                
                var encryptedScore = Encryptor.Encrypt(json);

                var cloudEvent = CloudEventCreator.CreateJson(result, 
                    new CloudEventCreationOptions()
                    {
                        EventTypeName = "ncompilebench.resultcreated",
                        AdditionalExtensions = new ICloudEventExtension[]
                        {
                            new EncryptedKeyCloudEventExtension(encryptedScore.EncryptedKey),
                            new EncryptedResultCloudEventExtension(encryptedScore.EncryptedText)
                        },
                        Source = new Uri($"http://ncompilebench.io/version/{_fileVersionInfo.ProductVersion}")
                    });

                var dateTime = DateTime.UtcNow;
                var resultFile = Path.Combine(resultDirectory, $"result_{dateTime:yyyyMMdd}_{dateTime.Ticks}.json");

                File.WriteAllText(resultFile, cloudEvent);

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Writing score failed. If possible, submit this as an issue in GitHub: " + ex);
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

                foreach (var result in results.OrderByDescending(x => x.Score))
                {
                    var systemText = $"{result.HardwareInfo.Model}";

                    if (string.IsNullOrWhiteSpace(systemText))
                    {
                        systemText = result.HardwareInfo.SystemFamily;
                    }

                    var cpuText =
                        $"{result.HardwareInfo.Cpu.Name}, {result.HardwareInfo.Cpu.Count} CPU, {result.HardwareInfo.Cpu.NumberOfCores}/{result.HardwareInfo.Cpu.NumberOfLogicalProcessors} cores";

                    table.AddRow(systemText, cpuText, $"{result.Score} ({result.SingleCoreScore})");
                }

                Console.WriteLine(table);
                Console.WriteLine("****");
                Console.WriteLine("More results are available from https://www.ncompilebench.io");
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

                switch (counter % 3)
                {
                    case 0:
                        Console.Write("\r.   ");
                        counter = 0;

                        break;
                    case 1:
                        Console.Write("\r..  ");

                        break;
                    case 2:
                        Console.Write("\r... ");

                        break;
                    case 3:
                        Console.Write("\r....");

                        break;
                }

                Thread.Sleep(TimeSpan.FromMilliseconds(500));
            }

            Console.SetCursorPosition(0, Console.CursorTop);
        }
    }
}
