namespace HighConvertAI.Server.Models.HomepageExtraction.Sections
{
    public sealed class HomepageCroSignalsSection
    {
        // Message clarity
        public string? HeroHeadline { get; set; }
        public string? HeroSubheadline { get; set; }
        public List<string> ValuePropKeywords { get; set; } = new();
        public string? AudienceMatchHint { get; set; }
        public string? VagueCopy { get; set; } // "true"/"false"
        public string? JargonDetected { get; set; } // "true"/"false"
        public string? MissingBenefit { get; set; } // "true"/"false"

        // Primary conversion path
        public string? PrimaryCtaText { get; set; }
        public string? PrimaryCtaHref { get; set; }
        public int PrimaryCtaCountAboveFold { get; set; }
        public int ConflictingCtaCountAboveFold { get; set; }
        public string? CtaVerbStrength { get; set; } // weak, ok, strong
        public bool HasRiskReversalNearCta { get; set; }

        // Trust and proof
        public bool HasTestimonials { get; set; }
        public int TestimonialCount { get; set; }
        public bool HasRatings { get; set; }
        public string? AvgRatingIfFound { get; set; }
        public bool HasLogosOrPressMentions { get; set; }
        public bool HasSecurityOrPaymentBadges { get; set; }
        public bool HasClearContactInfo { get; set; }

        // Friction and UX
        public int NavLinkCount { get; set; }
        public bool HasSearch { get; set; }
        public bool PopupOrModalOnLoad { get; set; }
        public int FormFieldCountOnFirstStep { get; set; }
        public int BrokenOrEmptyCtaLinks { get; set; }
        public bool MobileViewportOk { get; set; }
        public int ImageAltCoveragePct { get; set; }
        public int IntrusiveElementsCount { get; set; }

        // Offer and commercial clarity
        public bool PricingVisibleOnHome { get; set; }
        public bool StartsAtPricePresent { get; set; }
        public bool DiscountOrPromoPresent { get; set; }
        public bool ShippingOrReturnsHighlighted { get; set; }
        public bool UrgencyElementsPresent { get; set; }
        public string? UrgencyQuality { get; set; } // realistic, manipulative, none

        // Content completeness
        public int FeatureBulletsCount { get; set; }
        public string? FeatureSpecificityScore { get; set; } // generic, mixed, concrete
        public bool FaqPresent { get; set; }
        public bool ObjectionHandlingPresent { get; set; }

        // Evidence snippets
        public List<string> EvidenceSnippets { get; set; } = new();
    }
}
