using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Craftmatrix.org.Services;
using Craftmatrix.org.Model;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using System.ComponentModel.DataAnnotations;

namespace Craftmatrix.org.Controller.V2
{
    [ApiController]
    [Route("api/v2/[controller]")]
    [ApiVersion("2.0")]
    [Authorize]
    public class BillController : ControllerBase
    {
        private readonly MySQLService _db;

        public BillController(MySQLService db)
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

            throw new UnauthorizedAccessException("Unable to determine user ID from JWT token");
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<BillDto>>> GetBills()
        {
            try
            {
                var userId = await GetUserIdAsync();
                var allBills = await _db.GetDataAsync<BillDto>("Bills");
                var bills = allBills
                    .Where(b => b.UserID == userId)
                    .OrderBy(b => b.DueDate)
                    .ToList();

                return Ok(bills);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving bills", details = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BillDto>> GetBill(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { error = "Invalid bill ID. ID must be a positive integer." });
                }

                var userId = await GetUserIdAsync();
                var bills = await _db.GetDataAsync<BillDto>("Bills");
                var bill = bills.FirstOrDefault(b => b.Id == id && b.UserID == userId);

                if (bill == null)
                {
                    return NotFound(new { error = $"Bill with ID {id} not found or you don't have permission to access it." });
                }

                return Ok(bill);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving the bill", details = ex.Message });
            }
        }

        [HttpGet("upcoming")]
        public async Task<ActionResult<IEnumerable<BillDto>>> GetUpcomingBills(int days = 7)
        {
            try
            {
                if (days < 1 || days > 365)
                {
                    return BadRequest(new { error = "Days parameter must be between 1 and 365." });
                }

                var userId = await GetUserIdAsync();
                var upcomingDate = DateTime.UtcNow.AddDays(days);
                
                var allBills = await _db.GetDataAsync<BillDto>("Bills");
                var bills = allBills
                    .Where(b => b.UserID == userId && 
                               b.DueDate <= upcomingDate && 
                               b.Status != "paid")
                    .OrderBy(b => b.DueDate)
                    .ToList();

                return Ok(bills);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving upcoming bills", details = ex.Message });
            }
        }

        [HttpGet("overdue")]
        public async Task<ActionResult<IEnumerable<BillDto>>> GetOverdueBills()
        {
            try
            {
                var userId = await GetUserIdAsync();
                var today = DateTime.UtcNow.Date;
                
                var allBills = await _db.GetDataAsync<BillDto>("Bills");
                var bills = allBills
                    .Where(b => b.UserID == userId && 
                               b.DueDate < today && 
                               b.Status != "paid")
                    .OrderBy(b => b.DueDate)
                    .ToList();

                return Ok(bills);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while retrieving overdue bills", details = ex.Message });
            }
        }

        [HttpPost]
        public async Task<ActionResult<BillDto>> CreateBill(BillDto bill)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var validationResult = ValidateBillData(bill);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { error = validationResult.ErrorMessage });
                }

                var userId = await GetUserIdAsync();
                bill.UserID = userId;
                bill.CreatedAt = DateTime.UtcNow;
                bill.UpdatedAt = DateTime.UtcNow;

                // Calculate next due date for recurring bills
                if (bill.IsRecurring && !string.IsNullOrEmpty(bill.RecurrenceType))
                {
                    bill.NextDueDate = CalculateNextDueDate(bill.DueDate, bill.RecurrenceType, bill.RecurrenceInterval ?? 1);
                }

                await _db.PostDataAsync("Bills", bill);

                return CreatedAtAction(nameof(GetBill), new { id = bill.Id }, bill);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while creating the bill", details = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBill(int id, BillDto bill)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { error = "Invalid bill ID. ID must be a positive integer." });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var validationResult = ValidateBillData(bill);
                if (!validationResult.IsValid)
                {
                    return BadRequest(new { error = validationResult.ErrorMessage });
                }

                var userId = await GetUserIdAsync();
                var bills = await _db.GetDataAsync<BillDto>("Bills");
                var existingBill = bills.FirstOrDefault(b => b.Id == id && b.UserID == userId);

                if (existingBill == null)
                {
                    return NotFound(new { error = $"Bill with ID {id} not found or you don't have permission to access it." });
                }

                existingBill.Name = bill.Name;
                existingBill.Amount = bill.Amount;
                existingBill.DueDate = bill.DueDate;
                existingBill.Status = bill.Status;
                existingBill.CategoryID = bill.CategoryID;
                existingBill.AutoRemind = bill.AutoRemind;
                existingBill.Notes = bill.Notes;
                existingBill.Currency = bill.Currency;
                existingBill.IsRecurring = bill.IsRecurring;
                existingBill.RecurrenceType = bill.RecurrenceType;
                existingBill.RecurrenceInterval = bill.RecurrenceInterval;
                existingBill.UpdatedAt = DateTime.UtcNow;

                // Recalculate next due date if recurring settings changed
                if (existingBill.IsRecurring && !string.IsNullOrEmpty(existingBill.RecurrenceType))
                {
                    existingBill.NextDueDate = CalculateNextDueDate(existingBill.DueDate, existingBill.RecurrenceType, existingBill.RecurrenceInterval ?? 1);
                }

                await _db.PutDataAsync("Bills", id, existingBill);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while updating the bill", details = ex.Message });
            }
        }

        [HttpPatch("{id}/pay")]
        public async Task<IActionResult> MarkBillAsPaid(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { error = "Invalid bill ID. ID must be a positive integer." });
                }

                var userId = await GetUserIdAsync();
                var bills = await _db.GetDataAsync<BillDto>("Bills");
                var bill = bills.FirstOrDefault(b => b.Id == id && b.UserID == userId);

                if (bill == null)
                {
                    return NotFound(new { error = $"Bill with ID {id} not found or you don't have permission to access it." });
                }

                if (bill.Status == "paid")
                {
                    return BadRequest(new { error = "Bill is already marked as paid." });
                }

                bill.Status = "paid";
                bill.UpdatedAt = DateTime.UtcNow;

                // If it's a recurring bill, create the next instance
                if (bill.IsRecurring && !string.IsNullOrEmpty(bill.RecurrenceType) && bill.NextDueDate.HasValue)
                {
                    var nextBill = new BillDto
                    {
                        UserID = bill.UserID,
                        Name = bill.Name,
                        Amount = bill.Amount,
                        DueDate = bill.NextDueDate.Value,
                        Status = "pending",
                        CategoryID = bill.CategoryID,
                        AutoRemind = bill.AutoRemind,
                        Notes = bill.Notes,
                        Currency = bill.Currency,
                        IsRecurring = bill.IsRecurring,
                        RecurrenceType = bill.RecurrenceType,
                        RecurrenceInterval = bill.RecurrenceInterval,
                        NextDueDate = CalculateNextDueDate(bill.NextDueDate.Value, bill.RecurrenceType, bill.RecurrenceInterval ?? 1),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _db.PostDataAsync("Bills", nextBill);
                }

                await _db.PutDataAsync("Bills", id, bill);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while processing the payment status", details = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBill(int id)
        {
            try
            {
                if (id <= 0)
                {
                    return BadRequest(new { error = "Invalid bill ID. ID must be a positive integer." });
                }

                var userId = await GetUserIdAsync();
                var bills = await _db.GetDataAsync<BillDto>("Bills");
                var bill = bills.FirstOrDefault(b => b.Id == id && b.UserID == userId);

                if (bill == null)
                {
                    return NotFound(new { error = $"Bill with ID {id} not found or you don't have permission to access it." });
                }

                await _db.DeleteDataAsync("Bills", id);

                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "An error occurred while deleting the bill", details = ex.Message });
            }
        }

        private (bool IsValid, string ErrorMessage) ValidateBillData(BillDto bill)
        {
            if (string.IsNullOrWhiteSpace(bill.Name))
            {
                return (false, "Bill name is required and cannot be empty.");
            }

            if (bill.Name.Length > 100)
            {
                return (false, "Bill name cannot exceed 100 characters.");
            }

            if (bill.Amount <= 0)
            {
                return (false, "Bill amount must be greater than zero.");
            }

            if (bill.Amount > 999999.99m)
            {
                return (false, "Bill amount cannot exceed 999,999.99.");
            }

            if (bill.DueDate < DateTime.UtcNow.Date.AddDays(-365))
            {
                return (false, "Due date cannot be more than one year in the past.");
            }

            if (bill.DueDate > DateTime.UtcNow.Date.AddYears(10))
            {
                return (false, "Due date cannot be more than 10 years in the future.");
            }

            var validStatuses = new[] { "pending", "paid", "overdue" };
            if (!validStatuses.Contains(bill.Status?.ToLower()))
            {
                return (false, "Status must be one of: pending, paid, overdue.");
            }

            if (string.IsNullOrWhiteSpace(bill.Currency))
            {
                return (false, "Currency is required.");
            }

            if (bill.Currency.Length != 3)
            {
                return (false, "Currency must be a 3-character ISO code (e.g., USD, EUR).");
            }

            if (bill.IsRecurring)
            {
                if (string.IsNullOrWhiteSpace(bill.RecurrenceType))
                {
                    return (false, "Recurrence type is required for recurring bills.");
                }

                var validRecurrenceTypes = new[] { "daily", "weekly", "monthly", "yearly" };
                if (!validRecurrenceTypes.Contains(bill.RecurrenceType.ToLower()))
                {
                    return (false, "Recurrence type must be one of: daily, weekly, monthly, yearly.");
                }

                if (bill.RecurrenceInterval.HasValue && (bill.RecurrenceInterval < 1 || bill.RecurrenceInterval > 365))
                {
                    return (false, "Recurrence interval must be between 1 and 365.");
                }
            }

            if (!string.IsNullOrEmpty(bill.Notes) && bill.Notes.Length > 1000)
            {
                return (false, "Notes cannot exceed 1000 characters.");
            }

            return (true, string.Empty);
        }

        private DateTime CalculateNextDueDate(DateTime currentDueDate, string recurrenceType, int interval)
        {
            try
            {
                return recurrenceType.ToLower() switch
                {
                    "daily" => currentDueDate.AddDays(interval),
                    "weekly" => currentDueDate.AddDays(7 * interval),
                    "monthly" => currentDueDate.AddMonths(interval),
                    "yearly" => currentDueDate.AddYears(interval),
                    _ => currentDueDate.AddMonths(1) // Default to monthly
                };
            }
            catch (ArgumentOutOfRangeException)
            {
                // If calculation results in invalid date, return monthly increment as fallback
                return currentDueDate.AddMonths(1);
            }
        }
    }
}