namespace HighConvertAI.Server.Models.ProductExtraction.Sections
{
    public sealed class TrustSection
    {
        public bool HasTrustBadge { get; set; }
        public bool HasGuarantee { get; set; }
        public bool HasWarranty { get; set; }
        public bool HasSecurePayment { get; set; }
        public List<string> PaymentMethods { get; set; } = new();
    }
}
