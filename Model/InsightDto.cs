using System.ComponentModel.DataAnnotations;

namespace Craftmatrix.org.Model
{
    public class InsightDto
    {
        [Key]
        public int Id { get; set; }
        
        [Required]
        public Guid UserID { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Type { get; set; } = string.Empty; // warning, suggestion, tip, analysis
        
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty;
        
        [StringLength(20)]
        public string Priority { get; set; } = "medium"; // low, medium, high
        
        [StringLength(50)]
        public string Category { get; set; } = string.Empty; // spending, saving, budget, goal
        
        public bool IsRead { get; set; } = false;
        
        public bool IsActionable { get; set; } = false;
        
        public string? ActionData { get; set; } // JSON data for actionable insights
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? ExpiresAt { get; set; }
        
        // AI metadata
        public string? AIProvider { get; set; } = "Google Gemini";
        
        public string? AIModelVersion { get; set; }
        
        public decimal? ConfidenceScore { get; set; }
    }

    public class GenerateInsightRequest
    {
        public string? Category { get; set; } // spending, budget, goal/savings, general
        public string? Question { get; set; } // User's specific question
    }
}