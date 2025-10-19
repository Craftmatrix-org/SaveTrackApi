using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Craftmatrix.org.Data;
using Craftmatrix.org.Model;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace Craftmatrix.org.Controller.V2
{
    [ApiController]
    [Route("api/v2/[controller]")]
    [ApiVersion("2.0")]
    [Authorize]
    public class BillController : ControllerBase
    {
        private readonly AppDbContext _context;

        public BillController(AppDbContext context)
        {
            _context = context;
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
            var userId = await GetUserIdAsync();
            var bills = await _context.Bills
                .Where(b => b.UserID == userId)
                .OrderBy(b => b.DueDate)
                .ToListAsync();

            return Ok(bills);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<BillDto>> GetBill(int id)
        {
            var userId = await GetUserIdAsync();
            var bill = await _context.Bills
                .FirstOrDefaultAsync(b => b.Id == id && b.UserID == userId);

            if (bill == null)
            {
                return NotFound();
            }

            return Ok(bill);
        }

        [HttpGet("upcoming")]
        public async Task<ActionResult<IEnumerable<BillDto>>> GetUpcomingBills(int days = 7)
        {
            var userId = await GetUserIdAsync();
            var upcomingDate = DateTime.UtcNow.AddDays(days);
            
            var bills = await _context.Bills
                .Where(b => b.UserID == userId && 
                           b.DueDate <= upcomingDate && 
                           b.Status != "paid")
                .OrderBy(b => b.DueDate)
                .ToListAsync();

            return Ok(bills);
        }

        [HttpGet("overdue")]
        public async Task<ActionResult<IEnumerable<BillDto>>> GetOverdueBills()
        {
            var userId = await GetUserIdAsync();
            var today = DateTime.UtcNow.Date;
            
            var bills = await _context.Bills
                .Where(b => b.UserID == userId && 
                           b.DueDate < today && 
                           b.Status != "paid")
                .OrderBy(b => b.DueDate)
                .ToListAsync();

            return Ok(bills);
        }

        [HttpPost]
        public async Task<ActionResult<BillDto>> CreateBill(BillDto bill)
        {
            var userId = await GetUserIdAsync();
            bill.UserID = userId;
            bill.CreatedAt = DateTime.UtcNow;
            bill.UpdatedAt = DateTime.UtcNow;

            // Calculate next due date for recurring bills
            if (bill.IsRecurring && !string.IsNullOrEmpty(bill.RecurrenceType))
            {
                bill.NextDueDate = CalculateNextDueDate(bill.DueDate, bill.RecurrenceType, bill.RecurrenceInterval ?? 1);
            }

            _context.Bills.Add(bill);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetBill), new { id = bill.Id }, bill);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateBill(int id, BillDto bill)
        {
            var userId = await GetUserIdAsync();
            var existingBill = await _context.Bills
                .FirstOrDefaultAsync(b => b.Id == id && b.UserID == userId);

            if (existingBill == null)
            {
                return NotFound();
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

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpPatch("{id}/pay")]
        public async Task<IActionResult> MarkBillAsPaid(int id)
        {
            var userId = await GetUserIdAsync();
            var bill = await _context.Bills
                .FirstOrDefaultAsync(b => b.Id == id && b.UserID == userId);

            if (bill == null)
            {
                return NotFound();
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

                _context.Bills.Add(nextBill);
            }

            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteBill(int id)
        {
            var userId = await GetUserIdAsync();
            var bill = await _context.Bills
                .FirstOrDefaultAsync(b => b.Id == id && b.UserID == userId);

            if (bill == null)
            {
                return NotFound();
            }

            _context.Bills.Remove(bill);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private DateTime CalculateNextDueDate(DateTime currentDueDate, string recurrenceType, int interval)
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
    }
}