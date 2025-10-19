using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Craftmatrix.org.Services;
using Craftmatrix.org.Model;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace Craftmatrix.org.Controller.V2
{
    [ApiController]
    [Route("api/v2/[controller]")]
    [ApiVersion("2.0")]
    [Authorize]
    public class ReportController : ControllerBase
    {
        private readonly MySQLService _db;

        public ReportController(MySQLService db)
        {
            _db = db;
        }

        private async Task<Guid> GetUserIdAsync()
        {
            // Try Subject claim first (this is what the debug token contains)
            var subjectClaim = User.FindFirst("sub")?.Value; // Use literal string instead of constant
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

            // Try to get user ID from NameIdentifier claim as fallback
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var userId))
            {
                return userId;
            }

            // Try email claim as fallback
            var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value ?? User.FindFirst("email")?.Value;
            if (!string.IsNullOrEmpty(emailClaim))
            {
                var users = await _db.GetDataAsync<UserDto>("Users");
                var user = users.FirstOrDefault(u => u.Email == emailClaim);
                if (user != null)
                {
                    return user.Id;
                }
            }

            throw new UnauthorizedAccessException("Unable to determine user identity");
        }

        /// <summary>
        /// Get all reports for the authenticated user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<ReportDto>>> GetReports([FromQuery] string? type = null)
        {
            try
            {
                var userId = await GetUserIdAsync();
                
                var reports = await _db.GetDataAsync<ReportDto>("Reports");
                var userReports = reports.Where(r => r.UserID == userId);

                if (!string.IsNullOrEmpty(type))
                {
                    userReports = userReports.Where(r => r.Type.ToLower() == type.ToLower());
                }

                var result = userReports
                    .OrderByDescending(r => DateTime.Parse(r.TimeStamp))
                    .ToList();

                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving reports", error = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific report by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ReportDto>> GetReport(Guid id)
        {
            try
            {
                var userId = await GetUserIdAsync();
                
                var reports = await _db.GetDataAsync<ReportDto>("Reports");
                var report = reports.FirstOrDefault(r => r.Id == id && r.UserID == userId);

                if (report == null)
                {
                    return NotFound(new { message = "Report not found" });
                }

                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the report", error = ex.Message });
            }
        }

        /// <summary>
        /// Create a new report
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ReportDto>> CreateReport([FromBody] CreateReportRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = await GetUserIdAsync();

                // Validate JSON format for Response if provided
                if (!string.IsNullOrEmpty(request.Response))
                {
                    try
                    {
                        JsonSerializer.Deserialize<object>(request.Response);
                    }
                    catch (JsonException)
                    {
                        return BadRequest(new { message = "Response must be valid JSON format" });
                    }
                }

                var report = new ReportDto
                {
                    Id = Guid.NewGuid(),
                    UserID = userId,
                    Type = request.Type.ToLower(),
                    Endpoint = request.Endpoint.Trim(),
                    Response = request.Response,
                    TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                };

                await _db.PostDataAsync("Reports", report);

                return CreatedAtAction(nameof(GetReport), new { id = report.Id }, report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the report", error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing report
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ReportDto>> UpdateReport(Guid id, [FromBody] UpdateReportRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = await GetUserIdAsync();
                
                var reports = await _db.GetDataAsync<ReportDto>("Reports");
                var report = reports.FirstOrDefault(r => r.Id == id && r.UserID == userId);

                if (report == null)
                {
                    return NotFound(new { message = "Report not found" });
                }

                // Validate JSON format for Response if provided
                if (!string.IsNullOrEmpty(request.Response))
                {
                    try
                    {
                        JsonSerializer.Deserialize<object>(request.Response);
                    }
                    catch (JsonException)
                    {
                        return BadRequest(new { message = "Response must be valid JSON format" });
                    }
                }

                report.Type = request.Type.ToLower();
                report.Endpoint = request.Endpoint.Trim();
                report.Response = request.Response;
                report.TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");

                await _db.PutDataAsync("Reports", id, report);

                return Ok(report);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the report", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a report
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteReport(Guid id)
        {
            try
            {
                var userId = await GetUserIdAsync();
                
                var reports = await _db.GetDataAsync<ReportDto>("Reports");
                var report = reports.FirstOrDefault(r => r.Id == id && r.UserID == userId);

                if (report == null)
                {
                    return NotFound(new { message = "Report not found" });
                }

                await _db.DeleteDataAsync("Reports", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the report", error = ex.Message });
            }
        }

        /// <summary>
        /// Generate comprehensive financial summary report
        /// </summary>
        [HttpPost("generate/summary")]
        public async Task<ActionResult<ReportDto>> GenerateSummaryReport([FromBody] GenerateReportRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = await GetUserIdAsync();

                var reportData = await GenerateFinancialSummary(userId, request.StartDate, request.EndDate);

                var report = new ReportDto
                {
                    Id = Guid.NewGuid(),
                    UserID = userId,
                    Type = "summary",
                    Endpoint = "/api/v2/report/generate/summary",
                    Response = JsonSerializer.Serialize(reportData),
                    TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                };

                if (request.SaveReport)
                {
                    await _db.PostDataAsync("Reports", report);

                    return CreatedAtAction(nameof(GetReport), new { id = report.Id }, report);
                }

                return Ok(new { data = reportData, generated = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while generating the summary report", error = ex.Message });
            }
        }

        /// <summary>
        /// Generate expense breakdown report
        /// </summary>
        [HttpPost("generate/expenses")]
        public async Task<ActionResult<ReportDto>> GenerateExpenseReport([FromBody] GenerateReportRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = await GetUserIdAsync();

                var reportData = await GenerateExpenseBreakdown(userId, request.StartDate, request.EndDate);

                var report = new ReportDto
                {
                    Id = Guid.NewGuid(),
                    UserID = userId,
                    Type = "expenses",
                    Endpoint = "/api/v2/report/generate/expenses",
                    Response = JsonSerializer.Serialize(reportData),
                    TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                };

                if (request.SaveReport)
                {
                    await _db.PostDataAsync("Reports", report);

                    return CreatedAtAction(nameof(GetReport), new { id = report.Id }, report);
                }

                return Ok(new { data = reportData, generated = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while generating the expense report", error = ex.Message });
            }
        }

        /// <summary>
        /// Generate income analysis report
        /// </summary>
        [HttpPost("generate/income")]
        public async Task<ActionResult<ReportDto>> GenerateIncomeReport([FromBody] GenerateReportRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = await GetUserIdAsync();

                var reportData = await GenerateIncomeAnalysis(userId, request.StartDate, request.EndDate);

                var report = new ReportDto
                {
                    Id = Guid.NewGuid(),
                    UserID = userId,
                    Type = "income",
                    Endpoint = "/api/v2/report/generate/income",
                    Response = JsonSerializer.Serialize(reportData),
                    TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                };

                if (request.SaveReport)
                {
                    await _db.PostDataAsync("Reports", report);

                    return CreatedAtAction(nameof(GetReport), new { id = report.Id }, report);
                }

                return Ok(new { data = reportData, generated = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while generating the income report", error = ex.Message });
            }
        }

        /// <summary>
        /// Generate goal progress report
        /// </summary>
        [HttpPost("generate/goals")]
        public async Task<ActionResult<ReportDto>> GenerateGoalReport([FromBody] GenerateReportRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = await GetUserIdAsync();

                var reportData = await GenerateGoalProgress(userId, request.StartDate, request.EndDate);

                var report = new ReportDto
                {
                    Id = Guid.NewGuid(),
                    UserID = userId,
                    Type = "goals",
                    Endpoint = "/api/v2/report/generate/goals",
                    Response = JsonSerializer.Serialize(reportData),
                    TimeStamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")
                };

                if (request.SaveReport)
                {
                    await _db.PostDataAsync("Reports", report);

                    return CreatedAtAction(nameof(GetReport), new { id = report.Id }, report);
                }

                return Ok(new { data = reportData, generated = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while generating the goal report", error = ex.Message });
            }
        }

        // Private helper methods for report generation
        private async Task<object> GenerateFinancialSummary(Guid userId, DateTime startDate, DateTime endDate)
        {
            var transactions = await _db.GetDataAsync<TransactionDto>("Transactions");
            var categories = await _db.GetDataAsync<CategoryDto>("Categories");
            var accounts = await _db.GetDataAsync<AccountDto>("Accounts");
            var bills = await _db.GetDataAsync<BillDto>("Bills");

            var userTransactions = transactions
                .Where(t => t.UserID == userId && t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .ToList();

            var userAccounts = accounts.Where(a => a.UserID == userId).ToList();
            var userBills = bills.Where(b => b.UserID == userId).ToList();

            var totalIncome = userTransactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
            var totalExpenses = Math.Abs(userTransactions.Where(t => t.Amount < 0).Sum(t => t.Amount));
            var netIncome = totalIncome - totalExpenses;
            var totalBalance = userAccounts.Sum(a => a.InitValue);

            var monthlyBills = userBills.Sum(b => b.Amount);
            var transactionCount = userTransactions.Count;

            var categoryBreakdown = userTransactions
                .Where(t => t.Amount < 0)
                .GroupBy(t => categories.FirstOrDefault(c => c.Id == t.CategoryID)?.Name ?? "Uncategorized")
                .Select(g => new { 
                    category = g.Key, 
                    amount = Math.Abs(g.Sum(t => t.Amount)),
                    percentage = Math.Round((Math.Abs(g.Sum(t => t.Amount)) / totalExpenses) * 100, 2)
                })
                .OrderByDescending(x => x.amount)
                .ToList();

            return new
            {
                period = new { startDate, endDate },
                summary = new { totalIncome, totalExpenses, netIncome, totalBalance, monthlyBills, transactionCount },
                breakdown = categoryBreakdown,
                accountSummary = userAccounts.Select(a => new { a.Label, Balance = a.InitValue, AccountType = a.isCredit })
            };
        }

        private async Task<object> GenerateExpenseBreakdown(Guid userId, DateTime startDate, DateTime endDate)
        {
            var transactions = await _db.GetDataAsync<TransactionDto>("Transactions");
            var categories = await _db.GetDataAsync<CategoryDto>("Categories");
            var accounts = await _db.GetDataAsync<AccountDto>("Accounts");

            var expenses = transactions
                .Where(t => t.UserID == userId && t.Amount < 0 && t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .ToList();

            var totalExpenses = Math.Abs(expenses.Sum(t => t.Amount));

            var categoryBreakdown = expenses
                .GroupBy(t => categories.FirstOrDefault(c => c.Id == t.CategoryID)?.Name ?? "Uncategorized")
                .Select(g => new { 
                    category = g.Key,
                    amount = Math.Abs(g.Sum(t => t.Amount)),
                    count = g.Count(),
                    percentage = Math.Round((Math.Abs(g.Sum(t => t.Amount)) / totalExpenses) * 100, 2),
                    averageTransaction = Math.Round(Math.Abs(g.Average(t => t.Amount)), 2)
                })
                .OrderByDescending(x => x.amount)
                .ToList();

            var accountBreakdown = expenses
                .GroupBy(t => accounts.FirstOrDefault(a => a.Id == t.AccountID)?.Label ?? "Unknown")
                .Select(g => new { 
                    account = g.Key,
                    amount = Math.Abs(g.Sum(t => t.Amount)),
                    count = g.Count()
                })
                .OrderByDescending(x => x.amount)
                .ToList();

            var monthlyTrend = expenses
                .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
                .Select(g => new { 
                    month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    amount = Math.Abs(g.Sum(t => t.Amount)),
                    count = g.Count()
                })
                .OrderBy(x => x.month)
                .ToList();

            return new
            {
                period = new { startDate, endDate },
                totalExpenses,
                categoryBreakdown,
                accountBreakdown,
                monthlyTrend
            };
        }

        private async Task<object> GenerateIncomeAnalysis(Guid userId, DateTime startDate, DateTime endDate)
        {
            var transactions = await _db.GetDataAsync<TransactionDto>("Transactions");
            var categories = await _db.GetDataAsync<CategoryDto>("Categories");

            var income = transactions
                .Where(t => t.UserID == userId && t.Amount > 0 && t.CreatedAt >= startDate && t.CreatedAt <= endDate)
                .ToList();

            var totalIncome = income.Sum(t => t.Amount);

            var categoryBreakdown = income
                .GroupBy(t => categories.FirstOrDefault(c => c.Id == t.CategoryID)?.Name ?? "Uncategorized")
                .Select(g => new { 
                    category = g.Key,
                    amount = g.Sum(t => t.Amount),
                    count = g.Count(),
                    percentage = Math.Round((g.Sum(t => t.Amount) / totalIncome) * 100, 2)
                })
                .OrderByDescending(x => x.amount)
                .ToList();

            var monthlyTrend = income
                .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
                .Select(g => new { 
                    month = $"{g.Key.Year}-{g.Key.Month:D2}",
                    amount = g.Sum(t => t.Amount),
                    count = g.Count()
                })
                .OrderBy(x => x.month)
                .ToList();

            return new
            {
                period = new { startDate, endDate },
                totalIncome,
                categoryBreakdown,
                monthlyTrend
            };
        }

        private async Task<object> GenerateGoalProgress(Guid userId, DateTime startDate, DateTime endDate)
        {
            var wishlistParents = await _db.GetDataAsync<WishListParentDto>("WishListParents");
            var wishlists = await _db.GetDataAsync<WishListDto>("WishLists");

            var userWishlistParents = wishlistParents.Where(wp => wp.UserID == userId).ToList();
            var userWishlists = wishlists.Where(w => w.UserID == userId).ToList();

            var goalSummary = userWishlistParents.Select(wp => {
                var parentWishlists = userWishlists.Where(w => w.ParentId == wp.Id).ToList();
                return new {
                    goalName = wp.Name,
                    description = wp.Description,
                    totalItems = parentWishlists.Count,
                    totalValue = parentWishlists.Sum(w => w.Price),
                    averageItemPrice = parentWishlists.Any() ? Math.Round(parentWishlists.Average(w => w.Price), 2) : 0
                };
            }).ToList();

            var totalGoalValue = goalSummary.Sum(g => g.totalValue);
            var totalGoalItems = goalSummary.Sum(g => g.totalItems);

            return new
            {
                period = new { startDate, endDate },
                summary = new { totalGoalValue, totalGoalItems, goalCount = goalSummary.Count },
                goalDetails = goalSummary.OrderByDescending(g => g.totalValue)
            };
        }
    }

    // Request DTOs
    public class CreateReportRequest
    {
        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "";

        [Required]
        [StringLength(200)]
        public string Endpoint { get; set; } = "";

        [Required]
        public string Response { get; set; } = "";
    }

    public class UpdateReportRequest
    {
        [Required]
        [StringLength(50)]
        public string Type { get; set; } = "";

        [Required]
        [StringLength(200)]
        public string Endpoint { get; set; } = "";

        [Required]
        public string Response { get; set; } = "";
    }

    public class GenerateReportRequest
    {
        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddMonths(-1);

        public DateTime EndDate { get; set; } = DateTime.UtcNow;

        public bool SaveReport { get; set; } = false;
    }
}