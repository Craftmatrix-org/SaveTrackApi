namespace Craftmatrix.org.Model;

public class AccountDto
{
    public Guid Id { get; set; }
    public Guid UserID { get; set; }
    public String Label { get; set; }
    public String Description { get; set; }
    public int InitValue { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
