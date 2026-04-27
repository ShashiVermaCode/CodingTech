using System;
using System.Collections.Generic;
using System.IO;
using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using LEAComplaintsEmail.Models;

namespace LEAComplaintsEmail
{
    public class LEAExcelExportService
    {
        private readonly string _outputPath;

        public LEAExcelExportService(IConfiguration config)
        {
            _outputPath = config["ExcelSettings:OutputPath"] ?? "Exports";
        }

        public string ExportToExcel(List<LEAComplaintsEmailModel> data)
        {
            if (!Directory.Exists(_outputPath))
                Directory.CreateDirectory(_outputPath);

            string fileName = $"LEA_Complaints_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx";
            string fullPath = Path.Combine(_outputPath, fileName);

            using (var workbook = new XLWorkbook())
            {
                var sheet = workbook.Worksheets.Add("LEA Complaints");

                // ===== HEADER =====
                sheet.Cell(1, 1).Value = "Acknowledgement No";
                sheet.Cell(1, 2).Value = "Transaction Date";
                sheet.Cell(1, 3).Value = "Reporting Date";
                sheet.Cell(1, 4).Value = "Transaction Id / UTR";
                sheet.Cell(1, 5).Value = "Layers";
                sheet.Cell(1, 6).Value = "Transaction Amount";
                sheet.Cell(1, 7).Value = "Disputed Amount";
                sheet.Cell(1, 8).Value = "Merchant Name";

                var headerRange = sheet.Range(1, 1, 1, 8);
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = XLColor.LightBlue;
                headerRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                // ===== DATA =====
                int row = 2;

                foreach (var txn in data)
                {
                    sheet.Cell(row, 1).Value = txn.AcknowledgementNo;
                    sheet.Cell(row, 2).Value = txn.TransactionDate;
                    sheet.Cell(row, 2).Style.DateFormat.Format = "dd-MM-yyyy";
                    //sheet.Cell(row, 2).Value = txn.TransactionDate;
                    //sheet.Cell(row, 3).Value = txn.ReportingDate;
                    sheet.Cell(row, 3).Value = txn.ReportingDate;
                    sheet.Cell(row, 3).Style.DateFormat.Format = "dd-MM-yyyy hh:mm AM/PM";
                    sheet.Cell(row, 4).Value = txn.UTRNumber;
                    sheet.Cell(row, 5).Value = txn.Layers;
                    sheet.Cell(row, 6).Value = txn.TransactionAmount;
                    sheet.Cell(row, 7).Value = txn.DisputedAmount;
                    sheet.Cell(row, 8).Value = txn.MerchantName;

                    row++;
                }

                // Auto adjust
                sheet.Columns().AdjustToContents();

                workbook.SaveAs(fullPath);
            }

            return fullPath;
        }
    }
}