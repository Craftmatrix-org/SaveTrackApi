namespace Craftmatrix.org.Model
{
    public class ReportDto
    {
        public Guid Id { get; set; }
        public Guid UserID { get; set; }
        public string Type { get; set; }
        public string Endpoint { get; set; }
        public string Response { get; set; }
        public string TimeStamp { get; set; }
    }
}
