using System;
using System.Threading.Tasks;
using LEAComplaintsEmail;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LEAComplaintsEmail
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<ILEAComplaintsEmailRepository, LEAComplaintsEmailRepository>();
                    services.AddSingleton<ILEAComplaintsEmailService, LEAComplaintsEmailService>();
                    services.AddSingleton<LEAComplaintsEmailProcessor>();
                    services.AddSingleton<LEAExcelExportService>();
                })
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                })
                .Build();

            try
            {
                var processor = host.Services.GetRequiredService<LEAComplaintsEmailProcessor>();
                await processor.RunAsync();
            }
            //catch (Exception ex)
            //{
            //    Console.ForegroundColor = ConsoleColor.Red;
            //    Console.WriteLine($"\n[FATAL] Application error: {ex.Message}");
            //    Console.ResetColor();
            //}
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n[FATAL] {ex.Message}");
                Console.WriteLine($"\n[STACK TRACE]\n{ex.StackTrace}"); 
                Console.ResetColor();
            }

            Console.WriteLine("\nPress any key to exit...");
            Console.ReadKey();
        }
    }
}
