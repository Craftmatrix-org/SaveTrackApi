namespace Craftmatrix.org.Model
{
    public class CategoryDto
    {
        public Guid Id { get; set; }
        public Guid UserID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public bool isPositive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
