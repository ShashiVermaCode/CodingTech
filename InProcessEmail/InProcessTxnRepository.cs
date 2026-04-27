using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using InProcessEmail.Models;
using Dapper;

namespace InProcessEmail
{
    public interface IInProcessTxnRepository
    {
        Task<(List<InProcessTxnModel> TxnList, InProcessTxnEmailResult? EmailResult)> SendInProcessTxnEmailAsync(DateTime startDate, DateTime endDate);

    }
    public class InProcessTxnRepository : IInProcessTxnRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<InProcessTxnRepository> _logger;

        public InProcessTxnRepository(IConfiguration config, ILogger<InProcessTxnRepository> logger)
        {
            _connectionString = config.GetConnectionString("PartnerDB")!;
            _logger = logger;
        }
        public async Task<(List<InProcessTxnModel> TxnList, InProcessTxnEmailResult? EmailResult)> SendInProcessTxnEmailAsync(DateTime startDate, DateTime endDate)
        {
            var txnList = new List<InProcessTxnModel>();
            InProcessTxnEmailResult? emailResult = null;

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                using var cmd = new SqlCommand("usp_SendInProcessTxnEmail", conn)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 120
                };

                cmd.Parameters.AddWithValue("@StartDate", startDate.Date);
                cmd.Parameters.AddWithValue("@EndDate", endDate.Date);

                using var reader = await cmd.ExecuteReaderAsync();

                // ✅ Result Set 1 - Excel data
                while (await reader.ReadAsync())
                {
                    var firstCol = reader.GetValue(0)?.ToString() ?? string.Empty;
                    if (firstCol.Contains("<Error>"))
                    {
                        _logger.LogWarning($"Proc error: {firstCol}");
                        return (txnList, null);
                    }

                    txnList.Add(new InProcessTxnModel
                    {
                        Id = reader.IsDBNull(reader.GetOrdinal("TxnId"))
                                              ? Guid.Empty
                                              : reader.GetGuid(reader.GetOrdinal("TxnId")),
                        UserCode = reader["UserCode"]?.ToString() ?? string.Empty,
                        MName = reader["MName"]?.ToString() ?? string.Empty,
                        TxnAmount = reader.IsDBNull(reader.GetOrdinal("TxnAmount"))
                                              ? 0 : reader.GetDecimal(reader.GetOrdinal("TxnAmount")),
                        TxnFees = reader.IsDBNull(reader.GetOrdinal("TxnFees"))
                                              ? 0 : reader.GetDecimal(reader.GetOrdinal("TxnFees")),
                        MerchantRefId = reader["MerchantRefId"]?.ToString() ?? string.Empty,
                        TxnMode = reader["TxnMode"]?.ToString() ?? string.Empty,
                        BankId = reader["BankId"]?.ToString() ?? string.Empty,
                        BeneName = reader["BeneName"]?.ToString() ?? string.Empty,
                        BeneMobile = reader["BeneMobile"]?.ToString() ?? string.Empty,
                        BeneAccountNumber = reader["BeneAccountNumber"]?.ToString() ?? string.Empty,
                        BeneIFSC = reader["BeneIFSC"]?.ToString() ?? string.Empty,
                        PayoutRefId = reader["PayoutRefId"]?.ToString() ?? string.Empty,
                        TxnStatus = reader["TxnStatus"]?.ToString() ?? string.Empty,
                        BankTxnId = reader["BankTxnId"]?.ToString() ?? string.Empty,
                        BankStatus = reader["BankStatus"]?.ToString() ?? string.Empty,
                        BankMessage = reader["BankMessage"]?.ToString() ?? string.Empty,
                        TxnDate = reader["TxnDate"]?.ToString() ?? string.Empty,
                        PipeName = reader["PipeName"]?.ToString() ?? string.Empty,
                        RRN = reader["RRN"]?.ToString() ?? string.Empty,
                        UpdateDate = reader["UpdateDate"]?.ToString() ?? string.Empty,
                        SourceTable = reader["SourceTable"]?.ToString() ?? string.Empty
                    });
                }

                // ✅ Result Set 2 - Email result
                if (await reader.NextResultAsync() && await reader.ReadAsync())
                {
                    emailResult = new InProcessTxnEmailResult
                    {
                        InsertedEmailId = reader.IsDBNull(reader.GetOrdinal("InsertedEmailId"))
                                            ? Guid.Empty
                                            : reader.GetGuid(reader.GetOrdinal("InsertedEmailId")),
                        CCEmails = reader["CCEmails"]?.ToString() ?? string.Empty,
                        BCCEmails = reader["BCCEmails"]?.ToString() ?? string.Empty,
                        FromEmail = reader["FromEmail"]?.ToString() ?? string.Empty,
                        EmailSubject = reader["EmailSubject"]?.ToString() ?? string.Empty,
                        Body = reader["Body"]?.ToString() ?? string.Empty,
                        TotalRecords = Convert.ToInt32(reader["TotalRecords"])
                    };
                }
            }
            catch (SqlException ex) { _logger.LogError($"[SQL Error]: {ex.Message}"); throw; }
            catch (Exception ex) { _logger.LogError($"[Error]: {ex.Message}"); throw; }

            return (txnList, emailResult);
        }
    }
}
