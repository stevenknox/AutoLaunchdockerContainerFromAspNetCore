using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace AutoLaunchSeq
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.Seq("http://localhost:5341")
                .CreateLogger();

            await StartSeq();

            try
            {
                Log.Information("Starting up");
                CreateHostBuilder(args).Build().Run();
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Application start-up failed");
            }
            finally
            {
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });

        private static async Task StartSeq() =>
            await RunStartupProcess(Environment.CurrentDirectory, "docker", "run --rm -it -e ACCEPT_EULA=Y -p 5341:80 datalust/seq");

            public static async Task ConsumeAsync(StreamReader reader, StringBuilder lines)
        {
            await Task.Yield();

            string line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                lines.AppendLine(line);
                Console.WriteLine(line);
            }
        }

        public static async Task<bool> RunStartupProcess(string workingDirectory, string command, string args)
        {
            var psi = new ProcessStartInfo(command, args)
            {
                WorkingDirectory = workingDirectory,
                UseShellExecute = false,
                CreateNoWindow = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            var p = new Process();
            try
            {
                p.StartInfo = psi;
                p.Start();

                var output = new StringBuilder();
                var errors = new StringBuilder();
                var outputTask = ConsumeAsync(p.StandardOutput, output);
                var errorTask = ConsumeAsync(p.StandardError, errors);

                return true;
            }
            finally
            {
                p.Dispose();
            }
        }
    }
}
