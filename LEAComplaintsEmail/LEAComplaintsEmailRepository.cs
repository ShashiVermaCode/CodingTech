using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using LEAComplaintsEmail.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LEAComplaintsEmail
{
    public interface ILEAComplaintsEmailRepository
    {
        Task<string?> GetLEADataAsync(string startDate, string endDate);
        Task<IEnumerable<LEAComplaintsEmailResult>> GenerateLEAComplaintsEmailAsync(string startDate, string endDate);
    }
    public class LEAComplaintsEmailRepository : ILEAComplaintsEmailRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<LEAComplaintsEmailRepository> _logger;
        private readonly IConfiguration _config;
        private readonly string _edKey;
        private readonly string _ivKey;


        public LEAComplaintsEmailRepository(IConfiguration config, ILogger<LEAComplaintsEmailRepository> logger)
        {
            _config = config;

            _connectionString = config.GetConnectionString("PartnerDB")
                ?? throw new InvalidOperationException("PartnerDB connection string is missing.");

            _logger = logger;

            _edKey = _config["Encryption:EDKey"]
                ?? throw new Exception("Encryption:EDKey missing");

            _ivKey = _config["Encryption:IVKey"]
                ?? throw new Exception("Encryption:IVKey missing");
        }

        private string SafeDecrypt(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            Span<byte> buffer = stackalloc byte[input.Length];
            bool isBase64 = Convert.TryFromBase64String(input, buffer, out int bytesWritten);

            if (!isBase64 || bytesWritten < 17)
                return input; 

            try
            {
                return InternalEncryption.Decrypt(input, _edKey, _ivKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Decryption failed for a value, returning raw. " +
                                   "Possibly encrypted with different key. Error: {Msg}", ex.Message);
                return input;
            }
        }
        public async Task<string?> GetLEADataAsync(string startDate, string endDate)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@StartDate", startDate);
            parameters.Add("@EndDate", endDate);

            using IDbConnection db = new SqlConnection(_connectionString);

            try
            {
                var result = await db.QueryFirstOrDefaultAsync<string>(
                    "usp_GetLEAReport",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching LEA report.");
                throw;
            }
        }

        public async Task<IEnumerable<LEAComplaintsEmailResult>> GenerateLEAComplaintsEmailAsync(string startDate, string endDate)
        {
            var parameters = new DynamicParameters();
            parameters.Add("@Startdt", DateTime.ParseExact(startDate, "dd/MM/yyyy", null).Date);
            parameters.Add("@EndDt", DateTime.ParseExact(endDate, "dd/MM/yyyy", null).Date);

            using IDbConnection db = new SqlConnection(_connectionString);
            try
            {
                var result = await db.QueryAsync<LEAComplaintsEmailResult>(
                    "usp_SendLEAComplaintsEmailData",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating LEA complaints email.");
                throw;
            }
        }
    }
}
