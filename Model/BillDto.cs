using System.ComponentModel.DataAnnotations;

namespace Craftmatrix.org.Model
{
    public class BillDto
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public Guid UserID { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        public decimal Amount { get; set; }
        
        [Required]
        public DateTime DueDate { get; set; }
        
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "pending"; // pending, paid, overdue
        
        public Guid? CategoryID { get; set; }
        
        public bool AutoRemind { get; set; } = true;
        
        public string? Notes { get; set; }
        
        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "USD";
        
        // Recurring bill properties
        public bool IsRecurring { get; set; } = false;
        
        [StringLength(20)]
        public string? RecurrenceType { get; set; } // monthly, weekly, yearly, custom
        
        public int? RecurrenceInterval { get; set; } // 1 for monthly, 2 for bi-monthly, etc.
        
        public DateTime? NextDueDate { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}