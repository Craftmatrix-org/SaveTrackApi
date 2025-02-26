using Microsoft.AspNetCore.Mvc;
using Craftmatrix.org.Services;
using Craftmatrix.org.Model;

namespace Craftmatrix.org.Controllers
{
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly MySQLService _mysqlservice;
        private readonly JwtService _jwtService;

        public AccountController(JwtService jwtService, MySQLService mySQLService)
        {
            _mysqlservice = mySQLService;
            _jwtService = jwtService;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var accounts = await _mysqlservice.GetDataAsync<AccountDto>("Accounts");
            return Ok(accounts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var accounts = await _mysqlservice.GetDataAsync<AccountDto>("Accounts");
            var transactions = await _mysqlservice.GetDataAsync<TransactionDto>("Transactions");
            var categories = await _mysqlservice.GetDataAsync<CategoryDto>("Categories");
            var accountList = accounts.Where(e => e.UserID == id).OrderByDescending(e => e.UpdatedAt).ToList();
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

            return Ok(result);
        }

        [HttpGet("specific/{id}")]
        public async Task<IActionResult> GetSpecific(Guid id)
        {
            var account = await _mysqlservice.GetDataAsync<AccountDto>("Accounts");
            var iam = account.Where(e => e.Id == id).ToList();
            if (iam == null)
            {
                return NotFound();
            }
            return Ok(iam);
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AccountDto account)
        {
            account.Id = Guid.NewGuid();
            account.CreatedAt = DateTime.UtcNow;
            account.UpdatedAt = DateTime.UtcNow;

            var UserExist = await _mysqlservice.GetDataAsync<UserDto>("Users");
            var checkUser = UserExist.Where(e => e.Id == account.UserID);
            if (!checkUser.Any())
            {
                return BadRequest("User does not exist");
            }

            await _mysqlservice.PostDataAsync<AccountDto>("Accounts", account);
            return Ok(account);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put(Guid id, [FromBody] AccountDto account)
        {
            var accountExist = await _mysqlservice.GetDataAsync<AccountDto>("Accounts");
            var checkAccount = accountExist.Where(e => e.Id == id);
            if (!checkAccount.Any())
            {
                return NotFound();
            }

            account.UpdatedAt = DateTime.UtcNow;

            await _mysqlservice.PutDataAsync<AccountDto>("Accounts", id, account);
            return Ok(account);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var accountExist = await _mysqlservice.GetDataAsync<AccountDto>("Accounts");
            var checkAccount = accountExist.Where(e => e.Id == id);
            if (!checkAccount.Any())
            {
                return NotFound();
            }

            await _mysqlservice.DeleteDataAsync("Accounts", id);
            return Ok();
        }


    }
}
