namespace Craftmatrix.org.Model;

public class AccountDto
{
    public Guid Id { get; set; }
    public Guid UserID { get; set; }
    public String Label { get; set; }
    public String Description { get; set; }
    public decimal InitValue { get; set; }
    public decimal? Limit { get; set; }
    public Boolean isCredit { get; set; } = false;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
