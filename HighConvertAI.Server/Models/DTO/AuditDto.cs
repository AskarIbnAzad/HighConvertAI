namespace HighConvertAI.Server.Models.DTO
{
    public class AuditDto
    {
        public int Id { get; set; }
        public string? UserName { get; set; }
        public string? Role { get; set; }
        public string Url { get; set; } = "";
        public string PageType { get; set; } = "";
        public decimal Score { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public object? ResultJson { get; set; }
    }

}
