using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using InProcessEmail;

namespace InProcessEmail
{
    public class InProcessEmailProcessor
    {

        private readonly IConfiguration _config;
        private readonly IInProcessEmailService _inprocessEmailService;
        private readonly ILogger<InProcessEmailProcessor> _logger;
        private readonly ILogger<InProcessTxnRepository> _inProcessRepo;
        private readonly IInProcessTxnRepository _inProcessTxnRepo;
        private readonly ExcelExportService _excelExportService;
        public InProcessEmailProcessor(IConfiguration config, IInProcessEmailService inprocessEmailService, ILogger<InProcessEmailProcessor> logger, ILogger<InProcessTxnRepository> inProcessRepo,
            IInProcessTxnRepository inProcessTxnRepo,    
        ExcelExportService excelExportService)        
        {
            _config = config;
            _logger = logger;
            _inProcessRepo = inProcessRepo;
            _inprocessEmailService = inprocessEmailService;
            _inProcessTxnRepo = inProcessTxnRepo;
            _excelExportService = excelExportService;
        }

        public async Task RunAsync()
        {
            Console.WriteLine("=================================================");
            Console.WriteLine("  Starting InProcess Txn Email...");
            Console.WriteLine("=================================================");

            try
            {
                var startDate = _config["InProcessEmail:StartDate"];
                var endDate = _config["InProcessEmail:EndDate"];
                var parsedStart = DateTime.Parse(startDate!);
                var parsedEnd = DateTime.Parse(endDate!);

                Console.WriteLine($"Date Range: {parsedStart:dd-MM-yyyy} - {parsedEnd:dd-MM-yyyy}");
                var (txnList, emailResult) = await _inProcessTxnRepo
                    .SendInProcessTxnEmailAsync(parsedStart, parsedEnd);

                if (txnList == null || !txnList.Any())
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("No InProcess transactions found.");
                    Console.ResetColor();
                    return;
                }

                if (emailResult == null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("No email data from proc.");
                    Console.ResetColor();
                    return;
                }

                Console.WriteLine($"Total records: {txnList.Count}");

                var attachments = new List<string>();

                var payoutData = txnList.Where(x =>
                    x.SourceTable == "Payout" || x.SourceTable == "Payout (Purging)").ToList();

                var vpaData = txnList.Where(x =>
                    x.SourceTable == "VPA" || x.SourceTable == "VPA (Purging)").ToList();

                if (payoutData.Any())
                {
                    string f = _excelExportService.ExportPayoutToExcel(txnList);
                    attachments.Add(f);
                    Console.WriteLine($"Payout Excel ({payoutData.Count} records): {Path.GetFileName(f)}");
                }

                if (vpaData.Any())
                {
                    string f = _excelExportService.ExportVpaToExcel(txnList);
                    attachments.Add(f);
                    Console.WriteLine($"VPA Excel ({vpaData.Count} records): {Path.GetFileName(f)}");
                }

                // ✅ CC parse
                var separator = new[] { ';', ',' };
                var ccList = new List<string>();

                if (!string.IsNullOrWhiteSpace(emailResult.CCEmails))
                {
                    foreach (var cc in emailResult.CCEmails
                             .Split(separator, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var clean = ExtractEmail(cc);
                        if (!string.IsNullOrWhiteSpace(clean))
                            ccList.Add(clean);
                    }
                }

                // ✅ Email send with attachments
                Console.WriteLine("=================================================");
                Console.WriteLine("  Sending Email...");
                Console.WriteLine("=================================================");

                bool sent = await _inprocessEmailService.SendInprocessEmailAsync(
                    new List<string> { emailResult.FromEmail },
                    ccList,
                    emailResult.EmailSubject,
                    emailResult.Body,
                    attachments
                );

                if (sent)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"Email sent → {emailResult.FromEmail}");
                    Console.WriteLine($"   Total Records : {emailResult.TotalRecords}");
                    Console.WriteLine($"   Attachments   : {attachments.Count} file(s)");
                    foreach (var f in attachments)
                        Console.WriteLine($"{Path.GetFileName(f)}");
                    Console.ResetColor();
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("Email sending failed.");
                    Console.ResetColor();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"[RunAsync Error] {ex.Message}");
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Failed: {ex.Message}");
                Console.ResetColor();
            }
        }
        private static string ExtractEmail(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            int start = input.IndexOf("<");
            int end = input.IndexOf(">");

            if (start >= 0 && end > start)
                return input.Substring(start + 1, end - start - 1).Trim();

            return input.Trim();
        }
    }

}

