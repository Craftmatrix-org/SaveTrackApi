namespace Craftmatrix.org.Model
{
    public class ChatDto
    {
        public Guid Id { get; set; }
        public Guid UserID { get; set; }
        public string Type { get; set; }
        public string Message { get; set; }
        public string Prompt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
