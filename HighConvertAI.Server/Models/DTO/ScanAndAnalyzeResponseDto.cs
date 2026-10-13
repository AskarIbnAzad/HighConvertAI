namespace HighConvertAI.Server.Models.DTO
{
    public class ScanAndAnalyzeResponseDto
    {
        public int AuditId { get; set; }

        public string Url { get; set; } = string.Empty;

        public string PageType { get; set; } = string.Empty;

        public Decimal Score { get; set; }
    }
}
