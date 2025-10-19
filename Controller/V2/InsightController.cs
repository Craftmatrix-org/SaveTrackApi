using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Craftmatrix.org.Data;
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
        private readonly AppDbContext _context;
        private readonly GeminiService _geminiService;

        public InsightController(AppDbContext context, GeminiService geminiService)
        {
            _context = context;
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
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == subjectClaim);
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
            var insights = await _context.Insights
                .Where(i => i.UserID == userId)
                .OrderByDescending(i => i.CreatedAt)
                .Take(20)
                .ToListAsync();

            return Ok(insights);
        }

        [HttpGet("unread")]
        public async Task<ActionResult<IEnumerable<InsightDto>>> GetUnreadInsights()
        {
            var userId = await GetUserIdAsync();
            var insights = await _context.Insights
                .Where(i => i.UserID == userId && !i.IsRead)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return Ok(insights);
        }

        [HttpPost("generate/spending-analysis")]
        public async Task<ActionResult<InsightDto>> GenerateSpendingAnalysis()
        {
            var userId = await GetUserIdAsync();
            
            // Get recent transactions and categories
            var transactions = await _context.Transactions
                .Where(t => t.UserID == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(50)
                .ToListAsync();

            var categories = await _context.Categories
                .Where(c => c.UserID == userId)
                .ToListAsync();

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

                _context.Insights.Add(insight);
                await _context.SaveChangesAsync();

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
            var budget = await _context.Budgets
                .Where(b => b.UserID == userId)
                .OrderByDescending(b => b.CreatedAt)
                .FirstOrDefaultAsync();

            var transactions = await _context.Transactions
                .Where(t => t.UserID == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(30)
                .ToListAsync();

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

                _context.Insights.Add(insight);
                await _context.SaveChangesAsync();

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
            var categories = await _context.Categories
                .Where(c => c.UserID == userId)
                .ToListAsync();

            var currentMonth = DateTime.UtcNow.Month;
            var currentYear = DateTime.UtcNow.Year;
            
            var transactions = await _context.Transactions
                .Where(t => t.UserID == userId && 
                           t.CreatedAt.Month == currentMonth && 
                           t.CreatedAt.Year == currentYear)
                .ToListAsync();

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

                _context.Insights.Add(insight);
                await _context.SaveChangesAsync();

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
            var wishlist = await _context.WishLists
                .Where(w => w.UserID == userId)
                .ToListAsync();

            var transactions = await _context.Transactions
                .Where(t => t.UserID == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(30)
                .ToListAsync();

            var accounts = await _context.Accounts
                .Where(a => a.UserID == userId)
                .ToListAsync();

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

                _context.Insights.Add(insight);
                await _context.SaveChangesAsync();

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
            var insight = await _context.Insights
                .FirstOrDefaultAsync(i => i.Id == id && i.UserID == userId);

            if (insight == null)
            {
                return NotFound();
            }

            insight.IsRead = true;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteInsight(int id)
        {
            var userId = await GetUserIdAsync();
            var insight = await _context.Insights
                .FirstOrDefaultAsync(i => i.Id == id && i.UserID == userId);

            if (insight == null)
            {
                return NotFound();
            }

            _context.Insights.Remove(insight);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}