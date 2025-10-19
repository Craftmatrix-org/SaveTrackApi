using System.ComponentModel.DataAnnotations;

namespace Craftmatrix.org.Model
{
    public class ChartDataDto
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public Guid UserID { get; set; }
        
        [Required]
        [StringLength(50)]
        public string ChartType { get; set; } = string.Empty; // line, bar, pie, progress
        
        [Required]
        [StringLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string DataSet { get; set; } = string.Empty; // JSON data for the chart
        
        [StringLength(50)]
        public string Period { get; set; } = "monthly"; // daily, weekly, monthly, yearly
        
        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }
        
        [StringLength(50)]
        public string Category { get; set; } = string.Empty; // expense, income, balance, category, goal
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        // Chart configuration
        public string? ChartConfig { get; set; } // JSON configuration for chart display
        
        public bool IsCached { get; set; } = true;
        
        public DateTime? CacheExpiresAt { get; set; }
    }
}