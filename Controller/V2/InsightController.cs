using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Craftmatrix.org.Model;
using Craftmatrix.org.Services;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Craftmatrix.org.Controller.V2
{
    [ApiController]
    [Route("api/v2/[controller]")]
    [ApiVersion("2.0")]
    [Authorize]
    public class InsightController : ControllerBase
    {
        private readonly MySQLService _db;
        private readonly GeminiService _geminiService;

        public InsightController(MySQLService db, GeminiService geminiService)
        {
            _db = db;
            _geminiService = geminiService;
        }

        private async Task<Guid> GetUserIdAsync()
        {
            // Try to get user ID from NameIdentifier claim first (for proper JWT tokens)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            // If NameIdentifier is not a valid GUID, try Subject claim (for debug tokens with email)
            var subjectClaim = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!string.IsNullOrEmpty(subjectClaim))
            {
                // If it's an email, look up the user in the database
                if (subjectClaim.Contains("@"))
                {
                    var users = await _db.GetDataAsync<UserDto>("Users");
                    var user = users.FirstOrDefault(u => u.Email == subjectClaim);
                    if (user != null)
                    {
                        return user.Id;
                    }
                }
                // If it's a GUID string, parse it
                else if (Guid.TryParse(subjectClaim, out var subjectGuid))
                {
                    return subjectGuid;
                }
            }

            throw new UnauthorizedAccessException("Unable to determine user ID from JWT token");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<InsightDto>>> GetInsights()
        {
            var userId = await GetUserIdAsync();
            var insights = await _db.GetDataAsync<InsightDto>("Insights");
            var userInsights = insights.Where(i => i.UserID == userId)
                .OrderByDescending(i => i.CreatedAt)
                .Take(20)
                .ToList();

            return Ok(userInsights);
        }

        [HttpGet("unread")]
        public async Task<ActionResult<IEnumerable<InsightDto>>> GetUnreadInsights()
        {
            var userId = await GetUserIdAsync();
            var insights = await _db.GetDataAsync<InsightDto>("Insights");
            var unreadInsights = insights.Where(i => i.UserID == userId && !i.IsRead)
                .OrderByDescending(i => i.CreatedAt)
                .ToList();

            return Ok(unreadInsights);
        }

        [HttpPost("generate/spending-analysis")]
        public async Task<ActionResult<InsightDto>> GenerateSpendingAnalysis()
        {
            var userId = await GetUserIdAsync();
            
            // Get recent transactions and categories
            var allTransactions = await _db.GetDataAsync<TransactionDto>("Transactions");
            var transactions = allTransactions.Where(t => t.UserID == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(50)
                .ToList();

            var allCategories = await _db.GetDataAsync<CategoryDto>("Categories");
            var categories = allCategories.Where(c => c.UserID == userId).ToList();

            if (!transactions.Any())
            {
                return BadRequest("No transaction data available for analysis.");
            }

            try
            {
                var aiInsight = await _geminiService.AnalyzeSpendingPatternsAsync(
                    transactions.Cast<object>().ToList(),
                    categories.Cast<object>().ToList()
                );

                var insight = new InsightDto
                {
                    UserID = userId,
                    Type = "analysis",
                    Title = "Spending Pattern Analysis",
                    Content = aiInsight,
                    Priority = "medium",
                    Category = "spending",
                    AIProvider = "Google Gemini",
                    CreatedAt = DateTime.UtcNow
                };

                await _db.PostDataAsync("Insights", insight);

                return Ok(insight);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating insight: {ex.Message}");
            }
        }

        [HttpPost("generate/budget-advice")]
        public async Task<ActionResult<InsightDto>> GenerateBudgetAdvice()
        {
            var userId = await GetUserIdAsync();
            
            // Get current budget and recent transactions
            var allBudgets = await _db.GetDataAsync<BudgetDto>("Budgets");
            var budget = allBudgets.Where(b => b.UserID == userId)
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefault();

            var allTransactions = await _db.GetDataAsync<TransactionDto>("Transactions");
            var transactions = allTransactions.Where(t => t.UserID == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(30)
                .ToList();

            if (budget == null)
            {
                return BadRequest("No budget found. Create a budget first to get personalized advice.");
            }

            try
            {
                var aiInsight = await _geminiService.GenerateBudgetAdviceAsync(budget, transactions.Cast<object>().ToList());

                var insight = new InsightDto
                {
                    UserID = userId,
                    Type = "suggestion",
                    Title = "Budget Optimization Advice",
                    Content = aiInsight,
                    Priority = "high",
                    Category = "budget",
                    AIProvider = "Google Gemini",
                    CreatedAt = DateTime.UtcNow
                };

                await _db.PostDataAsync("Insights", insight);

                return Ok(insight);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating budget advice: {ex.Message}");
            }
        }

        [HttpPost("generate/budget-warnings")]
        public async Task<ActionResult<IEnumerable<InsightDto>>> GenerateBudgetWarnings()
        {
            var userId = await GetUserIdAsync();
            
            // Get categories and recent transactions 
            var allCategories = await _db.GetDataAsync<CategoryDto>("Categories");
            var categories = allCategories.Where(c => c.UserID == userId).ToList();

            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            
            var allTransactions = await _db.GetDataAsync<TransactionDto>("Transactions");
            var transactions = allTransactions.Where(t => t.UserID == userId && 
                           t.CreatedAt.Month == currentMonth && 
                           t.CreatedAt.Year == currentYear).ToList();

            if (!categories.Any())
            {
                return BadRequest("No budget limits set. Add budget limits to categories to get warnings.");
            }

            try
            {
                var aiInsight = await _geminiService.CheckBudgetWarningsAsync(
                    categories.Cast<object>().ToList(),
                    transactions.Cast<object>().ToList()
                );

                var insight = new InsightDto
                {
                    UserID = userId,
                    Type = "warning",
                    Title = "Budget Alert",
                    Content = aiInsight,
                    Priority = "high",
                    Category = "budget",
                    AIProvider = "Google Gemini",
                    CreatedAt = DateTime.UtcNow
                };

                await _db.PostDataAsync("Insights", insight);

                return Ok(new[] { insight });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating budget warnings: {ex.Message}");
            }
        }

        [HttpPost("generate/savings-advice")]
        public async Task<ActionResult<InsightDto>> GenerateSavingsAdvice()
        {
            var userId = await GetUserIdAsync();
            
            // Get wishlist goals, recent transactions, and accounts
            var allWishlists = await _db.GetDataAsync<WishListDto>("WishLists");
            var wishlist = allWishlists.Where(w => w.UserID == userId).ToList();

            var allTransactions = await _db.GetDataAsync<TransactionDto>("Transactions");
            var transactions = allTransactions.Where(t => t.UserID == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(30)
                .ToList();

            var allAccounts = await _db.GetDataAsync<AccountDto>("Accounts");
            var accounts = allAccounts.Where(a => a.UserID == userId).ToList();

            if (!wishlist.Any())
            {
                return BadRequest("No savings goals found. Add items to your wishlist to get personalized savings advice.");
            }

            try
            {
                var aiInsight = await _geminiService.GenerateSavingsAdviceAsync(
                    wishlist.Cast<object>().ToList(),
                    transactions.Cast<object>().ToList(),
                    accounts
                );

                var insight = new InsightDto
                {
                    UserID = userId,
                    Type = "tip",
                    Title = "Savings Goal Advice",
                    Content = aiInsight,
                    Priority = "medium",
                    Category = "goal",
                    AIProvider = "Google Gemini",
                    CreatedAt = DateTime.UtcNow
                };

                await _db.PostDataAsync("Insights", insight);

                return Ok(insight);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error generating savings advice: {ex.Message}");
            }
        }

        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var userId = await GetUserIdAsync();
            var allInsights = await _db.GetDataAsync<InsightDto>("Insights");
            var insight = allInsights.FirstOrDefault(i => i.Id == id && i.UserID == userId);

            if (insight == null)
            {
                return NotFound();
            }

            insight.IsRead = true;
            await _db.PutDataAsync("Insights", id, insight);

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInsight(int id)
        {
            var userId = await GetUserIdAsync();
            var allInsights = await _db.GetDataAsync<InsightDto>("Insights");
            var insight = allInsights.FirstOrDefault(i => i.Id == id && i.UserID == userId);

            if (insight == null)
            {
                return NotFound();
            }

            await _db.DeleteDataAsync("Insights", id);

            return NoContent();
        }
    }
}