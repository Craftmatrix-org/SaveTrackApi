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
    public class TransactionController : ControllerBase
    {
        private readonly MySQLService _db;

        public TransactionController(MySQLService db)
        {
            _db = db;
        }

        [HttpGet]
        public async Task<IActionResult> GetTransactions()
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

                var transactions = await _db.GetDataAsync<TransactionDto>("Transactions");
                var categories = await _db.GetDataAsync<CategoryDto>("Categories");
                var accounts = await _db.GetDataAsync<AccountDto>("Accounts");

                var userTransactions = transactions
                    .Where(t => t.UserID == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new
                    {
                        t.Id,
                        t.Description,
                        t.UserID,
                        t.Amount,
                        t.CategoryID,
                        t.AccountID,
                        t.CreatedAt,
                        t.UpdatedAt,
                        IsPositive = categories.FirstOrDefault(c => c.Id == t.CategoryID)?.isPositive ?? false,
                        CategoryName = categories.FirstOrDefault(c => c.Id == t.CategoryID)?.Name ?? "Unknown",
                        AccountName = accounts.FirstOrDefault(a => a.Id == t.AccountID)?.Label ?? "Unknown"
                    });

                return Ok(userTransactions);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving transactions", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTransaction(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest(new { error = "Invalid transaction ID" });
                }

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

                var transactions = await _db.GetDataAsync<TransactionDto>("Transactions");
                var categories = await _db.GetDataAsync<CategoryDto>("Categories");
                var accounts = await _db.GetDataAsync<AccountDto>("Accounts");

                var transaction = transactions.FirstOrDefault(t => t.Id == id && t.UserID == userId);

                if (transaction == null)
                {
                    return NotFound(new { error = $"Transaction with ID {id} not found or you don't have permission to access it." });
                }

                var result = new
                {
                    transaction.Id,
                    transaction.Description,
                    transaction.UserID,
                    transaction.Amount,
                    transaction.CategoryID,
                    transaction.AccountID,
                    transaction.CreatedAt,
                    transaction.UpdatedAt,
                    IsPositive = categories.FirstOrDefault(c => c.Id == transaction.CategoryID)?.isPositive ?? false,
                    CategoryName = categories.FirstOrDefault(c => c.Id == transaction.CategoryID)?.Name ?? "Unknown",
                    AccountName = accounts.FirstOrDefault(a => a.Id == transaction.AccountID)?.Label ?? "Unknown"
                };

                return Ok(result);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the transaction", details = ex.Message });
            }
        }

        [HttpGet("account/{accountId}")]
        public async Task<IActionResult> GetTransactionsByAccount(Guid accountId)
        {
            try
            {
                if (accountId == Guid.Empty)
                {
                    return BadRequest(new { error = "Invalid account ID" });
                }

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

                var transactions = await _db.GetDataAsync<TransactionDto>("Transactions");
                var categories = await _db.GetDataAsync<CategoryDto>("Categories");
                var accounts = await _db.GetDataAsync<AccountDto>("Accounts");

                // Verify the account belongs to the user
                var account = accounts.FirstOrDefault(a => a.Id == accountId && a.UserID == userId);
                if (account == null)
                {
                    return NotFound(new { error = "Account not found or you don't have permission to access it." });
                }

                var accountTransactions = transactions
                    .Where(t => t.AccountID == accountId && t.UserID == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new
                    {
                        t.Id,
                        t.Description,
                        t.UserID,
                        t.Amount,
                        t.CategoryID,
                        t.AccountID,
                        t.CreatedAt,
                        t.UpdatedAt,
                        IsPositive = categories.FirstOrDefault(c => c.Id == t.CategoryID)?.isPositive ?? false,
                        CategoryName = categories.FirstOrDefault(c => c.Id == t.CategoryID)?.Name ?? "Unknown",
                        AccountName = account.Label
                    });

                return Ok(accountTransactions);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving transactions for the account", details = ex.Message });
            }
        }

        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetTransactionsByCategory(Guid categoryId)
        {
            try
            {
                if (categoryId == Guid.Empty)
                {
                    return BadRequest(new { error = "Invalid category ID" });
                }

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

                var transactions = await _db.GetDataAsync<TransactionDto>("Transactions");
                var categories = await _db.GetDataAsync<CategoryDto>("Categories");
                var accounts = await _db.GetDataAsync<AccountDto>("Accounts");

                // Verify the category belongs to the user
                var category = categories.FirstOrDefault(c => c.Id == categoryId && c.UserID == userId);
                if (category == null)
                {
                    return NotFound(new { error = "Category not found or you don't have permission to access it." });
                }

                var categoryTransactions = transactions
                    .Where(t => t.CategoryID == categoryId && t.UserID == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new
                    {
                        t.Id,
                        t.Description,
                        t.UserID,
                        t.Amount,
                        t.CategoryID,
                        t.AccountID,
                        t.CreatedAt,
                        t.UpdatedAt,
                        IsPositive = category.isPositive,
                        CategoryName = category.Name,
                        AccountName = accounts.FirstOrDefault(a => a.Id == t.AccountID)?.Label ?? "Unknown"
                    });

                return Ok(categoryTransactions);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving transactions for the category", details = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateTransaction([FromBody] TransactionDto transaction)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var validationResult = ValidateTransactionData(transaction);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { error = validationResult.ErrorMessage });
                }

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

                // Verify account and category belong to the user
                var accounts = await _db.GetDataAsync<AccountDto>("Accounts");
                var categories = await _db.GetDataAsync<CategoryDto>("Categories");

                var account = accounts.FirstOrDefault(a => a.Id == transaction.AccountID && a.UserID == userId);
                if (account == null)
                {
                    return BadRequest(new { error = "Account not found or you don't have permission to access it." });
                }

                var category = categories.FirstOrDefault(c => c.Id == transaction.CategoryID && c.UserID == userId);
                if (category == null)
                {
                    return BadRequest(new { error = "Category not found or you don't have permission to access it." });
                }

                transaction.Id = Guid.NewGuid();
                transaction.UserID = userId;
                transaction.CreatedAt = DateTime.UtcNow;
                transaction.UpdatedAt = DateTime.UtcNow;

                await _db.PostDataAsync<TransactionDto>("Transactions", transaction);
                return CreatedAtAction(nameof(GetTransaction), new { id = transaction.Id }, transaction);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the transaction", details = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTransaction(Guid id, [FromBody] TransactionDto transaction)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest(new { error = "Invalid transaction ID" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var validationResult = ValidateTransactionData(transaction);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { error = validationResult.ErrorMessage });
                }

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

                var transactions = await _db.GetDataAsync<TransactionDto>("Transactions");
                var existingTransaction = transactions.FirstOrDefault(t => t.Id == id && t.UserID == userId);

                if (existingTransaction == null)
                {
                    return NotFound(new { error = $"Transaction with ID {id} not found or you don't have permission to access it." });
                }

                // Verify account and category belong to the user
                var accounts = await _db.GetDataAsync<AccountDto>("Accounts");
                var categories = await _db.GetDataAsync<CategoryDto>("Categories");

                var account = accounts.FirstOrDefault(a => a.Id == transaction.AccountID && a.UserID == userId);
                if (account == null)
                {
                    return BadRequest(new { error = "Account not found or you don't have permission to access it." });
                }

                var category = categories.FirstOrDefault(c => c.Id == transaction.CategoryID && c.UserID == userId);
                if (category == null)
                {
                    return BadRequest(new { error = "Category not found or you don't have permission to access it." });
                }

                transaction.Id = id;
                transaction.UserID = userId;
                transaction.CreatedAt = existingTransaction.CreatedAt;
                transaction.UpdatedAt = DateTime.UtcNow;

                await _db.PutDataAsync<TransactionDto>("Transactions", id, transaction);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the transaction", details = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTransaction(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return BadRequest(new { error = "Invalid transaction ID" });
                }

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

                var transactions = await _db.GetDataAsync<TransactionDto>("Transactions");
                var transaction = transactions.FirstOrDefault(t => t.Id == id && t.UserID == userId);

                if (transaction == null)
                {
                    return NotFound(new { error = $"Transaction with ID {id} not found or you don't have permission to access it." });
                }

                await _db.DeleteDataAsync("Transactions", id);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the transaction", details = ex.Message });
            }
        }

        private (bool IsValid, string ErrorMessage) ValidateTransactionData(TransactionDto transaction)
        {
            if (transaction.AccountID == Guid.Empty)
            {
                return (false, "Valid Account ID is required.");
            }

            if (transaction.CategoryID == Guid.Empty)
            {
                return (false, "Valid Category ID is required.");
            }

            if (transaction.Amount <= 0)
            {
                return (false, "Transaction amount must be greater than zero.");
            }

            if (transaction.Amount > 999999.99m)
            {
                return (false, "Transaction amount cannot exceed 999,999.99.");
            }

            if (!string.IsNullOrEmpty(transaction.Description) && transaction.Description.Length > 500)
            {
                return (false, "Description cannot exceed 500 characters.");
            }

            return (true, string.Empty);
        }
    }
}