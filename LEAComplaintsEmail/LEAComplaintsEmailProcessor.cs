using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LEAComplaintsEmail.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LEAComplaintsEmail
{
    public class LEAComplaintsEmailProcessor
    {
        private readonly IConfiguration _config;
        private readonly ILEAComplaintsEmailService _emailService;
        private readonly ILogger<LEAComplaintsEmailProcessor> _logger;
        private readonly ILEAComplaintsEmailRepository _repo;
        private readonly LEAExcelExportService _excelService;

        public LEAComplaintsEmailProcessor(
            ILEAComplaintsEmailService emailService,
            IConfiguration config,
            ILogger<LEAComplaintsEmailProcessor> logger,
            ILEAComplaintsEmailRepository repo,
            LEAExcelExportService excelService)
        {
            _emailService = emailService;
            _config = config;
            _logger = logger;
            _repo = repo;
            _excelService = excelService;
        }

        public async Task RunAsync()
        {
            Console.WriteLine("===========================================");
            Console.WriteLine("   LEA Complaints Email Processor Started  ");
            Console.WriteLine("===========================================");

            try
            {
                var startDate = _config["LEAEmail:StartDate"];
                var endDate = _config["LEAEmail:EndDate"];

                if (string.IsNullOrWhiteSpace(startDate) || string.IsNullOrWhiteSpace(endDate))
                {
                    Console.WriteLine("StartDate / EndDate missing in config.");
                    return;
                }

                Console.WriteLine($"Date Range: {startDate} - {endDate}");

                //--------------------------------------------------
                // STEP 1: FETCH DATA
                //--------------------------------------------------
                var leaData = await _repo.GetLEADataAsync(startDate, endDate);

                if (leaData == null || !leaData.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("No LEA data found. Email not sent.");
                    Console.ResetColor();
                    return;
                }
                //--------------------------------------------------
                // STEP 2: GENERATE EMAIL DATA FROM SP
                //--------------------------------------------------
                var results = await _repo.GenerateLEAComplaintsEmailAsync(startDate, endDate);

                if (results == null || !results.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("Email data not generated from SP.");
                    Console.ResetColor();
                    return;
                }

                Console.WriteLine($"Email rows generated: {results.Count()}");


                //--------------------------------------------------
                // STEP 3: SEND EMAILS
                //--------------------------------------------------
                var separator = new[] { ';', ',' };

                foreach (var result in results)
                {
                    if (string.IsNullOrWhiteSpace(result.ToEmail))
                    {
                        Console.WriteLine("[SKIP] ToEmail is empty.");
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(result.Body) &&
                        result.Body.Contains("No records found", StringComparison.OrdinalIgnoreCase))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("[SKIP] No records in email body.");
                        Console.ResetColor();
                        continue;
                    }

                    var toList = result.ToEmail
                        .Split(separator, StringSplitOptions.RemoveEmptyEntries)
                        .Select(ExtractEmail)
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .ToList();

                    if (!toList.Any())
                    {
                        Console.WriteLine("[SKIP] No valid TO emails.");
                        continue;
                    }

                    var ccList = new List<string>();

                    if (!string.IsNullOrWhiteSpace(result.CCEmails))
                    {
                        ccList = result.CCEmails
                            .Split(separator, StringSplitOptions.RemoveEmptyEntries)
                            .Select(ExtractEmail)
                            .Where(x => !string.IsNullOrWhiteSpace(x))
                            .ToList();
                    }

                    bool sent = await _emailService.SendLEAComplaintsEmailAsync(
                        toList,
                        ccList,
                        result.EmailSubject,
                        result.Body
                    );

                    if (sent)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"Email sent → {string.Join(",", toList)}");
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"Email failed → {string.Join(",", toList)}");
                    }

                    Console.ResetColor();
                }

                Console.WriteLine("===========================================");
                Console.WriteLine("   Process Completed Successfully          ");
                Console.WriteLine("===========================================");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LEA Processor Error");

                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ERROR] {ex.Message}");
                Console.ResetColor();
            }
        }

        private static string ExtractEmail(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            int start = input.IndexOf('<');
            int end = input.IndexOf('>');

            if (start >= 0 && end > start)
                return input.Substring(start + 1, end - start - 1).Trim();

            return input.Trim();
        }
    }
}