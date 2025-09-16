namespace Craftmatrix.org.Model
{
    public class WishListDto
    {
        public Guid Id { get; set; }
        public Guid UserID { get; set; }
        public Guid ParentId { get; set; }
        public string Label { get; set; }
        public String Description { get; set; }
        public string Url { get; set; }
        public decimal Price { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
