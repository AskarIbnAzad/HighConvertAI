namespace HighConvertAI.Server.Models.ProductExtraction.Sections
{
    public sealed class ShippingSection
    {
        public bool HasShippingInfo { get; set; }
        public bool HasFreeShipping { get; set; }
        public bool HasReturnPolicy { get; set; }
        public bool HasCashOnDelivery { get; set; }
        public List<string> ShippingTexts { get; set; } = new();
    }
}
