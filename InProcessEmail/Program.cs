using System;
using System.Threading.Tasks;
using InProcessEmail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;


namespace InProcessEmail
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    var basePath = AppContext.BaseDirectory;
                    Console.WriteLine($"[DEBUG] Base Path: {basePath}");
                    Console.WriteLine($"[DEBUG] appsettings exists: {File.Exists(Path.Combine(basePath, "appsettings.json"))}");

                    config.SetBasePath(basePath);
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                })
                .ConfigureServices((context, services) =>
                {
                    services.AddScoped<IInProcessEmailService, InProcessEmailService>();
                    services.AddSingleton<InProcessEmailProcessor>();
                    services.AddSingleton<IInProcessTxnRepository, InProcessTxnRepository>();
                    services.AddSingleton<ExcelExportService>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .Build();

            try
            {
                var processor = host.Services.GetRequiredService<InProcessEmailProcessor>();
                await processor.RunAsync();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[FATAL] Application error: {ex.Message}");
                Console.ResetColor();
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}