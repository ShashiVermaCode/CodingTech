using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LEAComplaintsEmail.Models
{
    public class LEAComplaintsEmailModel
    {
        public string AcknowledgementNo { get; set; } = "";
        public string TransactionDate { get; set; } = "";
        public string ReportingDate { get; set; } = "";
        public string UTRNumber { get; set; } = "";
        public int Layers { get; set; }
        public decimal TransactionAmount { get; set; }
        public decimal DisputedAmount { get; set; }
        public string MerchantName { get; set; } = "";
    }
    public class LEAComplaintsEmailResult
    {
        public Guid InsertedEmailId { get; set; }
        public string ToEmail { get; set; } = string.Empty;
        public string CCEmails { get; set; } = string.Empty;
        public string BCCEmails { get; set; } = string.Empty;
        public string FromEmail { get; set; } = string.Empty;
        public string EmailSubject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public int TotalRecords { get; set; }
    }



}
