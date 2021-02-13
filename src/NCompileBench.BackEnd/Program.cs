using System;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;

namespace NCompileBench.BackEnd
{
    public class Program
    {
        public static void Main(string[] args)
        {
            try
            {
                var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

                var logConfigFileName = $"nlog.{environment}.config";

                if (!File.Exists(logConfigFileName))
                {
                    logConfigFileName = "nlog.config";
                }

                var logFactory = NLogBuilder.ConfigureNLog(logConfigFileName);
                var logger = logFactory.GetCurrentClassLogger();

                logger.Info("Starting NCompileBench Backend as {Environment}", environment);
                logger.Info("Used logging configuration {File}", logConfigFileName);

                CreateHostBuilder(args).Build().Run();
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();

                    webBuilder.ConfigureLogging(logging =>
                        {
                            logging.ClearProviders();
                            logging.SetMinimumLevel(LogLevel.Trace);
                        })
                        .UseNLog();
                });
    }
}
