using Microsoft.AspNetCore.Mvc;
using Craftmatrix.org.Services;
using Craftmatrix.org.Model;
using Microsoft.AspNetCore.Authorization;

namespace Craftmatrix.org.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly MySQLService _mysqlservice;


        public TransactionController(MySQLService mySQLService)
        {
            _mysqlservice = mySQLService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactions()
        {
            var transactions = await _mysqlservice.GetDataAsync<TransactionDto>("Transactions");
            return Ok(transactions);
        }

        [HttpGet("specific/{id}")]
        public async Task<IActionResult> GetTransactions(Guid id)
        {
            var transactions = await _mysqlservice.GetDataAsync<TransactionDto>("Transactions");
            var filter = transactions.Where(t => t.Id == id);
            return Ok(filter);
        }

        // ("87779a89-eb5d-4610-9375-687287915607")
        [HttpGet("{uid}")]
        public async Task<IActionResult> GetTransaction(Guid uid)
        {
            var transactions = await _mysqlservice.GetDataAsync<TransactionDto>("Transactions");
            var category = await _mysqlservice.GetDataAsync<CategoryDto>("Categories");
            var account = await _mysqlservice.GetDataAsync<AccountDto>("Accounts");
            var filter = transactions
                         .Where(t => t.UserID == uid)
                         .OrderByDescending(t => t.CreatedAt)
                         .Select(t => new
                         {
                             t.Id,
                             t.Description,
                             t.UserID,
                             t.Amount,
                             t.CategoryID,
                             t.AccountID,
                             CreatedAt = TimeZoneInfo.ConvertTimeFromUtc(t.CreatedAt, TimeZoneInfo.Local),
                             UpdatedAt = TimeZoneInfo.ConvertTimeFromUtc(t.UpdatedAt, TimeZoneInfo.Local),
                             IsPositive = category.FirstOrDefault(c => c.Id == t.CategoryID)?.isPositive,
                             Concat = $"You used {account.FirstOrDefault(a => a.Id == t.AccountID)?.Label} in {category.FirstOrDefault(c => c.Id == t.CategoryID)?.Name}",
                         });
            return Ok(filter);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransaction([FromBody] TransactionDto transaction)
        {
            transaction.Id = Guid.NewGuid();
            transaction.CreatedAt = DateTime.UtcNow;
            transaction.UpdatedAt = DateTime.UtcNow;
            var result = await _mysqlservice.PostDataAsync<TransactionDto>("Transactions", transaction);
            return Ok(result);
        }

        [HttpPut("{uid}")]
        public async Task<IActionResult> UpdateTransaction(Guid uid, [FromBody] TransactionDto transaction)
        {
            transaction.UpdatedAt = DateTime.UtcNow;
            var result = await _mysqlservice.PutDataAsync<TransactionDto>("Transactions", uid, transaction);
            return Ok(result);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(Guid id)
        {
            var result = await _mysqlservice.DeleteDataAsync("Transactions", id);
            return Ok(result);
        }

    }
}
