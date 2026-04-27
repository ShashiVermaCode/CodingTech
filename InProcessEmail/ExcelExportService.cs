using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using InProcessEmail.Models;
using Microsoft.Extensions.Logging;

namespace InProcessEmail
{
        public class ExcelExportService
        {
            private readonly string _outputPath;
            private readonly ILogger<ExcelExportService> _logger;

            public ExcelExportService(IConfiguration config, ILogger<ExcelExportService> logger)
            {
                _outputPath = config["ExcelSettings:OutputPath"]!;
                _logger = logger;
            }

            public string ExportPayoutToExcel(List<InProcessTxnModel> data)
            {
                var filtered = data.Where(x =>
                    x.SourceTable == "Payout" || x.SourceTable == "Payout (Purging)"
                ).ToList();

                return GenerateExcel(filtered, "Payout_InProcessTxn");
            }

            public string ExportVpaToExcel(List<InProcessTxnModel> data)
            {
                var filtered = data.Where(x =>
                    x.SourceTable == "VPA" || x.SourceTable == "VPA (Purging)"
                ).ToList();

                return GenerateExcel(filtered, "VPA_InProcessTxn");
            }

            private string GenerateExcel(List<InProcessTxnModel> data, string filePrefix)
            {
                if (!Directory.Exists(_outputPath))
                    Directory.CreateDirectory(_outputPath);

                string fileName = $"{filePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
                string fullPath = Path.Combine(_outputPath, fileName);

                using var workbook = new XLWorkbook();
                var sheet = workbook.Worksheets.Add("InProcess Txns");

                // ✅ Header Row
                sheet.Cell(1, 1).Value = "Row No";
                sheet.Cell(1, 2).Value = "Merchant Name";
                sheet.Cell(1, 3).Value = "User Code";
                sheet.Cell(1, 4).Value = "Txn Amount";
                sheet.Cell(1, 5).Value = "Txn Fees";
                sheet.Cell(1, 6).Value = "Merchant Ref ID";
                sheet.Cell(1, 7).Value = "Txn Mode";
                sheet.Cell(1, 8).Value = "Bank";
                sheet.Cell(1, 9).Value = "Bene Name";
                sheet.Cell(1, 10).Value = "Bene Mobile";
                sheet.Cell(1, 11).Value = "Bene Account No";
                sheet.Cell(1, 12).Value = "Bene IFSC";
                sheet.Cell(1, 13).Value = "Payout Ref ID";
                sheet.Cell(1, 14).Value = "Txn Status";
                sheet.Cell(1, 15).Value = "Bank Txn ID";
                sheet.Cell(1, 16).Value = "Bank Status";
                sheet.Cell(1, 17).Value = "Bank Message";
                sheet.Cell(1, 18).Value = "Txn Date";
                sheet.Cell(1, 19).Value = "Pipe Name";
                sheet.Cell(1, 20).Value = "RRN";
                sheet.Cell(1, 21).Value = "Update Date";
                sheet.Cell(1, 22).Value = "Source";

                // ✅ Full header styling (A1:V1 — 22 columns)
                var headerRange = sheet.Range("A1:V1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#DAE9F8");
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // ✅ Data Rows
                int row = 2;
                int rowNum = 1;
                foreach (var txn in data)
                {
                    sheet.Cell(row, 1).Value = rowNum++;
                    sheet.Cell(row, 2).Value = txn.MName;
                    sheet.Cell(row, 3).Value = txn.UserCode;
                    sheet.Cell(row, 4).Value = txn.TxnAmount;
                    sheet.Cell(row, 5).Value = txn.TxnFees;
                    sheet.Cell(row, 6).Value = txn.MerchantRefId;
                    sheet.Cell(row, 7).Value = txn.TxnMode;
                    sheet.Cell(row, 8).Value = txn.BankId;
                    sheet.Cell(row, 9).Value = txn.BeneName;
                    sheet.Cell(row, 10).Value = txn.BeneMobile;
                    sheet.Cell(row, 11).Value = txn.BeneAccountNumber;
                    sheet.Cell(row, 12).Value = txn.BeneIFSC;
                    sheet.Cell(row, 13).Value = txn.PayoutRefId;
                    sheet.Cell(row, 14).Value = txn.TxnStatus;
                    sheet.Cell(row, 15).Value = txn.BankTxnId;
                    sheet.Cell(row, 16).Value = txn.BankStatus;
                    sheet.Cell(row, 17).Value = txn.BankMessage;
                    sheet.Cell(row, 18).Value = txn.TxnDate;
                    sheet.Cell(row, 19).Value = txn.PipeName;
                    sheet.Cell(row, 20).Value = txn.RRN;
                    sheet.Cell(row, 21).Value = txn.UpdateDate;
                    sheet.Cell(row, 22).Value = txn.SourceTable;
                    row++;
                }

                sheet.Columns().AdjustToContents();
                workbook.SaveAs(fullPath);

                _logger.LogInformation($"Excel saved: {fullPath} | Records: {data.Count}");
                return fullPath;
            }
        }
    }

