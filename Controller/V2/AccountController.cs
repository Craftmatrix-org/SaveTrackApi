using Microsoft.AspNetCore.Mvc;
using Craftmatrix.org.Services;
using Craftmatrix.org.Model;
using Microsoft.AspNetCore.Authorization;
using Craftmatrix.org.Helpers;

namespace Craftmatrix.org.V2.Controllers
{
    [ApiController]
    [ApiVersion("2.0")]
    [Authorize]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly MySQLService _db;

        public AccountController(MySQLService db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> ListAccounts()
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return BadRequest("Missing or invalid Authorization header");

            var rmb = authHeader.Split(" ")[1];
            var jti = JwtHelper.GetJtiFromToken(rmb);

            var accounts = await _db.GetDataAsync<AccountDto>("Accounts");
            var transactions = await _db.GetDataAsync<TransactionDto>("Transactions");
            var categories = await _db.GetDataAsync<CategoryDto>("Categories");
            var accountList = accounts.Where(e => e.UserID == Guid.Parse(jti)).OrderByDescending(e => e.UpdatedAt).ToList();
            if (accountList == null || !accountList.Any())
            {
                return NotFound();
            }

            var result = accountList.Select(account => new
            {
                account.Id,
                account.UserID,
                account.Label,
                account.Description,
                account.InitValue,
                account.CreatedAt,
                account.UpdatedAt,
                Total = account.InitValue + transactions
                    .Where(t => t.AccountID == account.Id)
                    .Sum(t =>
                    {
                        var category = categories.FirstOrDefault(c => c.Id == t.CategoryID);
                        return category != null && category.isPositive ? t.Amount : -t.Amount;
                    })
            });

            var orderedResult = result.OrderByDescending(r => r.Total);
            return Ok(orderedResult);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AccountDto account)
        {
            account.Id = Guid.NewGuid();
            account.CreatedAt = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;

            var UserExist = await _db.GetDataAsync<UserDto>("Users");
            var checkUser = UserExist.Where(e => e.Id == account.UserID);
            if (!checkUser.Any())
            {
                return BadRequest("User does not exist");
            }

            await _db.PostDataAsync<AccountDto>("Accounts", account);
            return Ok(account);
        }

    }

}
