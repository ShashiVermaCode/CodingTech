using System;
using System.Collections.Generic;

namespace InProcessEmail.Models
{
    public class InProcessTxnModel
    {
        public int RowNum { get; set; }
        public Guid Id { get; set; }
        public Guid MerchantId { get; set; }
        public string UserCode { get; set; }
        public string MName { get; set; }
        public Guid PipeId { get; set; }
        public decimal TxnAmount { get; set; }
        public decimal TxnFees { get; set; }
        public string MerchantRefId { get; set; }
        public string TxnMode { get; set; }
        public string BankId { get; set; }
        public string BeneName { get; set; }
        public string BeneMobile { get; set; }
        public string BeneAccountNumber { get; set; }
        public string BeneIFSC { get; set; }
        public string PayoutRefId { get; set; }
        public string TxnStatus { get; set; }
        public string BankTxnId { get; set; }
        public string BankStatus { get; set; }
        public string BankMessage { get; set; }
        public string TxnDate { get; set; }
        public string PipeName { get; set; }
        public string RRN { get; set; }
        public string UpdateDate { get; set; }
        public string SourceTable { get; set; }
    }
    public class InProcessTxnEmailResult
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
