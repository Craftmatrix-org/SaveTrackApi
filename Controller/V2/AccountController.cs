using Microsoft.AspNetCore.Mvc;
using Craftmatrix.org.Services;
using Craftmatrix.org.Model;
using Microsoft.AspNetCore.Authorization;
using Craftmatrix.org.Helpers;
using System.ComponentModel.DataAnnotations;

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
            try
            {
                var authHeader = Request.Headers["Authorization"].FirstOrDefault();

                if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                {
                    return BadRequest(new { error = "Missing or invalid Authorization header" });
                }

                var rmb = authHeader.Split(" ")[1];
                var jti = JwtHelper.GetJtiFromToken(rmb);

                if (string.IsNullOrEmpty(jti) || !Guid.TryParse(jti, out var userId))
                {
                    return Unauthorized(new { error = "Invalid token or user ID" });
                }

                var accounts = await _db.GetDataAsync<AccountDto>("Accounts");
                var transactions = await _db.GetDataAsync<TransactionDto>("Transactions");
                var categories = await _db.GetDataAsync<CategoryDto>("Categories");
                
                var accountList = accounts.Where(e => e.UserID == userId).OrderByDescending(e => e.UpdatedAt).ToList();
                
                if (accountList == null || !accountList.Any())
                {
                    return Ok(new List<object>()); // Return empty list instead of NotFound
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
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving accounts", details = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] AccountDto account)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var validationResult = ValidateAccountData(account);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { error = validationResult.ErrorMessage });
                }

                account.Id = Guid.NewGuid();
                account.CreatedAt = DateTime.UtcNow;
                account.UpdatedAt = DateTime.UtcNow;

                var UserExist = await _db.GetDataAsync<UserDto>("Users");
                var checkUser = UserExist.Where(e => e.Id == account.UserID);
                if (!checkUser.Any())
                {
                    return BadRequest(new { error = "User does not exist" });
                }

                await _db.PostDataAsync<AccountDto>("Accounts", account);
                return CreatedAtAction(nameof(ListAccounts), new { id = account.Id }, account);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the account", details = ex.Message });
            }
        }

        private (bool IsValid, string ErrorMessage) ValidateAccountData(AccountDto account)
        {
            if (string.IsNullOrWhiteSpace(account.Label))
            {
                return (false, "Account label is required and cannot be empty.");
            }

            if (account.Label.Length > 100)
            {
                return (false, "Account label cannot exceed 100 characters.");
            }

            if (account.UserID == Guid.Empty)
            {
                return (false, "Valid User ID is required.");
            }

            if (account.InitValue < -999999.99m || account.InitValue > 999999.99m)
            {
                return (false, "Initial value must be between -999,999.99 and 999,999.99.");
            }

            if (account.Limit.HasValue && (account.Limit < 0 || account.Limit > 999999.99m))
            {
                return (false, "Account limit must be between 0 and 999,999.99.");
            }

            if (!string.IsNullOrEmpty(account.Description) && account.Description.Length > 500)
            {
                return (false, "Description cannot exceed 500 characters.");
            }

            return (true, string.Empty);
        }

    }

}
