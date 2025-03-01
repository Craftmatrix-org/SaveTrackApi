namespace Craftmatrix.org.Model
{
    public class TransferDto
    {
        public Guid Id { get; set; }
        // public string Name { get; set; }
        public string? Description { get; set; }
        public Guid UserID { get; set; }
        public decimal Amount { get; set; }
        public Guid AccountID_A { get; set; }
        public Guid AccountID_B { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
