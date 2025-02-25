using Microsoft.AspNetCore.Mvc;
using Craftmatrix.org.Services;
using Craftmatrix.org.Model;

namespace Craftmatrix.org.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class TransactionController : ControllerBase
    {
        private readonly MySQLService _mysqlservice;
        private readonly JwtService _jwtService;


        public TransactionController(JwtService jwtService, MySQLService mySQLService)
        {
            _mysqlservice = mySQLService;
            _jwtService = jwtService;
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactions()
        {
            var transactions = await _mysqlservice.GetDataAsync<TransactionDto>("Transactions");
            return Ok(transactions);
        }

        [HttpGet("{uid}")]
        public async Task<IActionResult> GetTransaction(Guid uid)
        {
            var transactions = await _mysqlservice.GetDataAsync<TransactionDto>("Transactions");
            var filter = transactions.Where(t => t.UserID == uid);
            return Ok(filter);
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransaction([FromBody] TransactionDto transaction)
        {
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

        [HttpDelete]
        public async Task<IActionResult> DeleteTransaction(Guid id)
        {
            var result = await _mysqlservice.DeleteDataAsync("Transactions", id);
            return Ok(result);
        }

    }
}
