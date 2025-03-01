namespace Craftmatrix.org.Model
{
    public class UserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string? Role { get; set; } = "regular";
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
