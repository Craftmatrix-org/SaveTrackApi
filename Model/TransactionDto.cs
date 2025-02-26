namespace Craftmatrix.org.Model
{
    public class TransactionDto
    {
        public Guid Id { get; set; }
        // public string Name { get; set; }
        public string? Description { get; set; }
        public Guid UserID { get; set; }
        public decimal Amount { get; set; }
        public Guid AccountID { get; set; }
        public Guid CategoryID { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
