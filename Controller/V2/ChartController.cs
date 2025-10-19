using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Craftmatrix.org.Services;
using Craftmatrix.org.Model;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Globalization;

namespace Craftmatrix.org.Controller.V2
{
    [ApiController]
    [Route("api/v2/[controller]")]
    [ApiVersion("2.0")]
    [Authorize]
    public class ChartController : ControllerBase
    {
        private readonly MySQLService _db;

        public ChartController(MySQLService db)
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
        /// Get all charts for the authenticated user
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<ChartDataDto>>> GetCharts([FromQuery] string? chartType = null, [FromQuery] string? category = null)
        {
            try
            {
                var userId = await GetUserIdAsync();
                
                var allCharts = await _db.GetDataAsync<ChartDataDto>("ChartData");
                var userCharts = allCharts.Where(c => c.UserID == userId);

                if (!string.IsNullOrEmpty(chartType))
                {
                    userCharts = userCharts.Where(c => c.ChartType.ToLower() == chartType.ToLower());
                }

                if (!string.IsNullOrEmpty(category))
                {
                    userCharts = userCharts.Where(c => c.Category.ToLower() == category.ToLower());
                }

                var charts = userCharts
                    .OrderByDescending(c => c.UpdatedAt)
                    .ToList();

                return Ok(charts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving charts", error = ex.Message });
            }
        }

        /// <summary>
        /// Get a specific chart by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ChartDataDto>> GetChart(int id)
        {
            try
            {
                var userId = await GetUserIdAsync();
                
                var allCharts = await _db.GetDataAsync<ChartDataDto>("ChartData");
                var chart = allCharts.FirstOrDefault(c => c.Id == id && c.UserID == userId);

                if (chart == null)
                {
                    return NotFound(new { message = "Chart not found" });
                }

                return Ok(chart);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the chart", error = ex.Message });
            }
        }

        /// <summary>
        /// Create a new chart
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<ChartDataDto>> CreateChart([FromBody] CreateChartRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = await GetUserIdAsync();

                // Validate JSON format for DataSet
                if (!string.IsNullOrEmpty(request.DataSet))
                {
                    try
                    {
                        JsonSerializer.Deserialize<object>(request.DataSet);
                    }
                    catch (JsonException)
                    {
                        return BadRequest(new { message = "DataSet must be valid JSON format" });
                    }
                }

                // Validate JSON format for ChartConfig if provided
                if (!string.IsNullOrEmpty(request.ChartConfig))
                {
                    try
                    {
                        JsonSerializer.Deserialize<object>(request.ChartConfig);
                    }
                    catch (JsonException)
                    {
                        return BadRequest(new { message = "ChartConfig must be valid JSON format" });
                    }
                }

                var chart = new ChartDataDto
                {
                    UserID = userId,
                    ChartType = request.ChartType.ToLower(),
                    Title = request.Title.Trim(),
                    DataSet = request.DataSet,
                    Period = request.Period?.ToLower() ?? "monthly",
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Category = request.Category?.ToLower() ?? "",
                    ChartConfig = request.ChartConfig,
                    IsCached = request.IsCached,
                    CacheExpiresAt = request.IsCached ? DateTime.UtcNow.AddHours(24) : null,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _db.PostDataAsync("ChartData", chart);

                return CreatedAtAction(nameof(GetChart), new { id = chart.Id }, chart);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while creating the chart", error = ex.Message });
            }
        }

        /// <summary>
        /// Update an existing chart
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<ChartDataDto>> UpdateChart(int id, [FromBody] UpdateChartRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = await GetUserIdAsync();
                
                var allCharts = await _db.GetDataAsync<ChartDataDto>("ChartData");
                var chart = allCharts.FirstOrDefault(c => c.Id == id && c.UserID == userId);

                if (chart == null)
                {
                    return NotFound(new { message = "Chart not found" });
                }

                // Validate JSON format for DataSet
                if (!string.IsNullOrEmpty(request.DataSet))
                {
                    try
                    {
                        JsonSerializer.Deserialize<object>(request.DataSet);
                    }
                    catch (JsonException)
                    {
                        return BadRequest(new { message = "DataSet must be valid JSON format" });
                    }
                }

                // Validate JSON format for ChartConfig if provided
                if (!string.IsNullOrEmpty(request.ChartConfig))
                {
                    try
                    {
                        JsonSerializer.Deserialize<object>(request.ChartConfig);
                    }
                    catch (JsonException)
                    {
                        return BadRequest(new { message = "ChartConfig must be valid JSON format" });
                    }
                }

                chart.ChartType = request.ChartType.ToLower();
                chart.Title = request.Title.Trim();
                chart.DataSet = request.DataSet;
                chart.Period = request.Period?.ToLower() ?? "monthly";
                chart.StartDate = request.StartDate;
                chart.EndDate = request.EndDate;
                chart.Category = request.Category?.ToLower() ?? "";
                chart.ChartConfig = request.ChartConfig;
                chart.IsCached = request.IsCached;
                chart.CacheExpiresAt = request.IsCached ? DateTime.UtcNow.AddHours(24) : null;
                chart.UpdatedAt = DateTime.UtcNow;

                await _db.PutDataAsync("ChartData", chart.Id, chart);

                return Ok(chart);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while updating the chart", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a chart
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteChart(int id)
        {
            try
            {
                var userId = await GetUserIdAsync();
                
                var allCharts = await _db.GetDataAsync<ChartDataDto>("ChartData");
                var chart = allCharts.FirstOrDefault(c => c.Id == id && c.UserID == userId);

                if (chart == null)
                {
                    return NotFound(new { message = "Chart not found" });
                }

                await _db.DeleteDataAsync("ChartData", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the chart", error = ex.Message });
            }
        }

        /// <summary>
        /// Generate chart data based on user's financial data
        /// </summary>
        [HttpPost("generate")]
        public async Task<ActionResult<ChartDataDto>> GenerateChart([FromBody] GenerateChartRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = await GetUserIdAsync();

                // Generate chart data based on the request
                var chartData = await GenerateChartData(userId, request);

                if (chartData == null)
                {
                    return BadRequest(new { message = "Unable to generate chart data with the provided parameters" });
                }

                // Save the generated chart if requested
                if (request.SaveChart)
                {
                    var chart = new ChartDataDto
                    {
                        UserID = userId,
                        ChartType = request.ChartType.ToLower(),
                        Title = request.Title ?? $"{request.Category} {request.ChartType} Chart",
                        DataSet = JsonSerializer.Serialize(chartData),
                        Period = request.Period?.ToLower() ?? "monthly",
                        StartDate = request.StartDate,
                        EndDate = request.EndDate,
                        Category = request.Category?.ToLower() ?? "",
                        IsCached = true,
                        CacheExpiresAt = DateTime.UtcNow.AddHours(24),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _db.PostDataAsync("ChartData", chart);

                    return CreatedAtAction(nameof(GetChart), new { id = chart.Id }, chart);
                }

                // Return the generated data without saving
                return Ok(new { data = chartData, generated = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while generating the chart", error = ex.Message });
            }
        }

        /// <summary>
        /// Clear expired cache entries
        /// </summary>
        [HttpPost("clear-cache")]
        public async Task<ActionResult> ClearExpiredCache()
        {
            try
            {
                var userId = await GetUserIdAsync();
                
                var allCharts = await _db.GetDataAsync<ChartDataDto>("ChartData");
                var expiredCharts = allCharts
                    .Where(c => c.UserID == userId && c.IsCached && c.CacheExpiresAt < DateTime.UtcNow)
                    .ToList();

                if (expiredCharts.Any())
                {
                    foreach (var expiredChart in expiredCharts)
                    {
                        await _db.DeleteDataAsync("ChartData", expiredChart.Id);
                    }
                }

                return Ok(new { message = $"Cleared {expiredCharts.Count} expired cache entries" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "An error occurred while clearing cache", error = ex.Message });
            }
        }

        private async Task<object?> GenerateChartData(Guid userId, GenerateChartRequest request)
        {
            try
            {
                switch (request.Category?.ToLower())
                {
                    case "expense":
                        return await GenerateExpenseChartData(userId, request);
                    case "income":
                        return await GenerateIncomeChartData(userId, request);
                    case "balance":
                        return await GenerateBalanceChartData(userId, request);
                    case "category":
                        return await GenerateCategoryChartData(userId, request);
                    case "goal":
                        return await GenerateGoalChartData(userId, request);
                    default:
                        return await GenerateOverviewChartData(userId, request);
                }
            }
            catch
            {
                return null;
            }
        }

        private async Task<object> GenerateExpenseChartData(Guid userId, GenerateChartRequest request)
        {
            var transactions = await _db.GetDataAsync<TransactionDto>("Transactions");
            var categories = await _db.GetDataAsync<CategoryDto>("Categories");
            
            var userTransactions = transactions
                .Where(t => t.UserID == userId && 
                           t.Amount < 0 && 
                           t.CreatedAt >= request.StartDate && 
                           t.CreatedAt <= request.EndDate)
                .ToList();

            return request.Period?.ToLower() switch
            {
                "daily" => userTransactions.GroupBy(t => t.CreatedAt.Date)
                    .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), amount = Math.Abs(g.Sum(t => t.Amount)) })
                    .OrderBy(x => x.date),
                "weekly" => userTransactions.GroupBy(t => CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(t.CreatedAt, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                    .Select(g => new { week = g.Key, amount = Math.Abs(g.Sum(t => t.Amount)) })
                    .OrderBy(x => x.week),
                "yearly" => userTransactions.GroupBy(t => t.CreatedAt.Year)
                    .Select(g => new { year = g.Key, amount = Math.Abs(g.Sum(t => t.Amount)) })
                    .OrderBy(x => x.year),
                _ => userTransactions.GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
                    .Select(g => new { month = $"{g.Key.Year}-{g.Key.Month:D2}", amount = Math.Abs(g.Sum(t => t.Amount)) })
                    .OrderBy(x => x.month)
            };
        }

        private async Task<object> GenerateIncomeChartData(Guid userId, GenerateChartRequest request)
        {
            var transactions = await _db.GetDataAsync<TransactionDto>("Transactions");
            var categories = await _db.GetDataAsync<CategoryDto>("Categories");
            
            var userTransactions = transactions
                .Where(t => t.UserID == userId && 
                           t.Amount > 0 && 
                           t.CreatedAt >= request.StartDate && 
                           t.CreatedAt <= request.EndDate)
                .ToList();

            return request.Period?.ToLower() switch
            {
                "daily" => userTransactions.GroupBy(t => t.CreatedAt.Date)
                    .Select(g => new { date = g.Key.ToString("yyyy-MM-dd"), amount = g.Sum(t => t.Amount) })
                    .OrderBy(x => x.date),
                "weekly" => userTransactions.GroupBy(t => CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(t.CreatedAt, CalendarWeekRule.FirstDay, DayOfWeek.Monday))
                    .Select(g => new { week = g.Key, amount = g.Sum(t => t.Amount) })
                    .OrderBy(x => x.week),
                "yearly" => userTransactions.GroupBy(t => t.CreatedAt.Year)
                    .Select(g => new { year = g.Key, amount = g.Sum(t => t.Amount) })
                    .OrderBy(x => x.year),
                _ => userTransactions.GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
                    .Select(g => new { month = $"{g.Key.Year}-{g.Key.Month:D2}", amount = g.Sum(t => t.Amount) })
                    .OrderBy(x => x.month)
            };
        }

        private async Task<object> GenerateBalanceChartData(Guid userId, GenerateChartRequest request)
        {
            var accounts = await _db.GetDataAsync<AccountDto>("Accounts");
            
            var userAccounts = accounts.Where(a => a.UserID == userId).ToList();

            return userAccounts.Select(a => new { 
                account = a.Label, 
                balance = a.InitValue,
                type = a.isCredit 
            });
        }

        private async Task<object> GenerateCategoryChartData(Guid userId, GenerateChartRequest request)
        {
            var transactions = await _db.GetDataAsync<TransactionDto>("Transactions");
            var categories = await _db.GetDataAsync<CategoryDto>("Categories");
            
            var userTransactions = transactions
                .Where(t => t.UserID == userId && 
                           t.CreatedAt >= request.StartDate && 
                           t.CreatedAt <= request.EndDate)
                .ToList();

            return userTransactions.GroupBy(t => categories.FirstOrDefault(c => c.Id == t.CategoryID)?.Name ?? "Uncategorized")
                .Select(g => new { 
                    category = g.Key, 
                    amount = Math.Abs(g.Sum(t => t.Amount)),
                    count = g.Count()
                })
                .OrderByDescending(x => x.amount);
        }

        private async Task<object> GenerateGoalChartData(Guid userId, GenerateChartRequest request)
        {
            var wishlistParents = await _db.GetDataAsync<WishListParentDto>("WishListParents");
            var userWishlistParents = wishlistParents.Where(wp => wp.UserID == userId).ToList();

            var wishlists = await _db.GetDataAsync<WishListDto>("WishLists");
            var userWishlists = wishlists.Where(w => w.UserID == userId).ToList();

            return userWishlistParents.Select(wp => {
                var parentWishlists = userWishlists.Where(w => w.ParentId == wp.Id).ToList();
                return new { 
                    goal = wp.Name, 
                    totalPrice = parentWishlists.Sum(w => w.Price),
                    itemCount = parentWishlists.Count
                };
            }).OrderByDescending(x => x.totalPrice);
        }

        private async Task<object> GenerateOverviewChartData(Guid userId, GenerateChartRequest request)
        {
            var transactions = await _db.GetDataAsync<TransactionDto>("Transactions");
            
            var userTransactions = transactions
                .Where(t => t.UserID == userId && 
                           t.CreatedAt >= request.StartDate && 
                           t.CreatedAt <= request.EndDate)
                .ToList();

            var totalIncome = userTransactions.Where(t => t.Amount > 0).Sum(t => t.Amount);
            var totalExpenses = Math.Abs(userTransactions.Where(t => t.Amount < 0).Sum(t => t.Amount));
            var netIncome = totalIncome - totalExpenses;

            return new { 
                totalIncome, 
                totalExpenses, 
                netIncome,
                transactionCount = userTransactions.Count
            };
        }
    }

    // Request DTOs
    public class CreateChartRequest
    {
        [Required]
        [StringLength(50)]
        public string ChartType { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = "";

        [Required]
        public string DataSet { get; set; } = "";

        [StringLength(50)]
        public string? Period { get; set; }

        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddMonths(-1);

        public DateTime EndDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string? Category { get; set; }

        public string? ChartConfig { get; set; }

        public bool IsCached { get; set; } = true;
    }

    public class UpdateChartRequest
    {
        [Required]
        [StringLength(50)]
        public string ChartType { get; set; } = "";

        [Required]
        [StringLength(100)]
        public string Title { get; set; } = "";

        [Required]
        public string DataSet { get; set; } = "";

        [StringLength(50)]
        public string? Period { get; set; }

        public DateTime StartDate { get; set; }

        public DateTime EndDate { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        public string? ChartConfig { get; set; }

        public bool IsCached { get; set; } = true;
    }

    public class GenerateChartRequest
    {
        [Required]
        [StringLength(50)]
        public string ChartType { get; set; } = "";

        public string? Title { get; set; }

        [StringLength(50)]
        public string? Period { get; set; } = "monthly";

        public DateTime StartDate { get; set; } = DateTime.UtcNow.AddMonths(-6);

        public DateTime EndDate { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        public string? Category { get; set; } = "overview";

        public bool SaveChart { get; set; } = false;
    }
}