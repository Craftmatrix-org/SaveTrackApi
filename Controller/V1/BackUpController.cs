using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.Transactions;
using dotenv.net;

namespace Craftmatrix.org.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    // [Authorize]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class BackupController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public BackupController(IConfiguration configuration)
        {
            _configuration = configuration;
            DotEnv.Load();
        }

        [HttpPost("backup")]
        public async Task<IActionResult> BackupDatabase()
        {
            string sourceConnectionString = $"Server={Environment.GetEnvironmentVariable("DB_HOST")};Port={Environment.GetEnvironmentVariable("DB_PORT")};User={Environment.GetEnvironmentVariable("DB_USER")};Password={Environment.GetEnvironmentVariable("DB_PASS")};Database={Environment.GetEnvironmentVariable("DB_DB")}";
            string destinationConnectionString = $"Server={Environment.GetEnvironmentVariable("DB_HOST")};Port={Environment.GetEnvironmentVariable("DB_PORT")};User={Environment.GetEnvironmentVariable("DB_USER")};Password={Environment.GetEnvironmentVariable("DB_PASS")};Database=savetrack";

            string[] tables = new string[]
            {
                "Accounts",
                "BudgetItems",
                "Budgets",
                "Categories",
                "Reports",
                "Transactions",
                "Transfers",
                "Users",
                "WishListParents",
                "WishLists"
            };

            try
            {
                using (TransactionScope scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                using (MySqlConnection sourceConnection = new MySqlConnection(sourceConnectionString))
                using (MySqlConnection destinationConnection = new MySqlConnection(destinationConnectionString))
                {
                    await sourceConnection.OpenAsync();
                    await destinationConnection.OpenAsync();

                    foreach (var table in tables)
                    {
                        string query = $"INSERT INTO {destinationConnection.Database}.{table} SELECT * FROM {sourceConnection.Database}.{table}";
                        using (MySqlCommand command = new MySqlCommand(query, destinationConnection))
                        {
                            await command.ExecuteNonQueryAsync();
                        }
                    }

                    scope.Complete();
                }

                return Ok("Backup completed successfully.");
            }
            catch (Exception ex)
            {
                // Log the exception (ex)
                return StatusCode(500, $"An error occurred during backup: {ex.Message}");
            }
        }
    }
}
