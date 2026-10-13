using System.Text.RegularExpressions;
using HtmlAgilityPack;
using HighConvertAI.Server.Models.HomepageExtraction;
using HighConvertAI.Server.Models.HomepageExtraction.Sections;
using HighConvertAI.Server.Services.HomepageExtraction.Abstractions;
using HighConvertAI.Server.Services.HomepageExtraction.Helpers;

namespace HighConvertAI.Server.Services.HomepageExtraction.Extractors
{
    public sealed class HomepageCroSignalsExtractor : IHomepageSectionExtractor
    {
        public int Order => 40;

        private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "the", "a", "an", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by",
            "from", "is", "are", "was", "be", "been", "have", "has", "do", "does", "did", "will",
            "would", "could", "should", "may", "might", "must", "can", "your", "our", "this", "that",
            "these", "those", "it", "its", "as", "which", "who", "where", "when", "why", "how"
        };

        private static readonly string[] StrongCtaVerbs = { "buy", "start", "get", "try", "book", "join", "subscribe", "shop", "order", "download" };
        private static readonly string[] WeakCtaVerbs = { "click", "submit", "learn more", "read more", "discover", "find out", "explore" };
        private static readonly string[] AudienceKeywords = { "teams", "founders", "developers", "parents", "students", "companies", "businesses", "enterprises", "agencies", "freelancers" };

        public void Extract(HomepageExtractionContext context)
        {
            var doc = context.Document;
            var cro = new HomepageCroSignalsSection();

            ExtractMessageClarity(doc, context, cro);
            ExtractPrimaryConversionPath(doc, context, cro);
            ExtractTrustSignals(doc, context, cro);
            ExtractFrictionAndUx(doc, context, cro);
            ExtractOfferClarity(doc, context, cro);
            ExtractContentCompleteness(doc, context, cro);

            if (context.Features == null || context.Features.Features.Count == 0)
            {
                var extracted = ExtractFeatureBullets(doc);
                if (extracted.Count > 0)
                {
                    context.Features = new FeatureSection { Features = extracted };
                }
            }

            context.CroSignals = cro;
            AddEvidenceEntries(context, cro);
        }

        private void ExtractMessageClarity(HtmlDocument doc, HomepageExtractionContext context, HomepageCroSignalsSection cro)
        {
            cro.HeroHeadline = context.Hero?.Headline;
            cro.HeroSubheadline = context.Hero?.Subheadline;

            var h1 = HomepageHtmlHelper.GetH1Text(doc);
            var desc = MetaTagHelper.GetDescription(doc);
            var firstPara = doc.DocumentNode.SelectSingleNode("//main//p | //p")?.InnerText ?? "";

            var combinedText = $"{h1} {desc} {firstPara}";
            var keywords = ExtractKeywords(combinedText, 5);
            cro.ValuePropKeywords = keywords;

            var audienceHint = DetectAudienceHint(combinedText);
            cro.AudienceMatchHint = audienceHint;

            cro.VagueCopy = ContainsVagueLanguage(combinedText) ? "true" : "false";
            cro.JargonDetected = ContainsJargon(combinedText) ? "true" : "false";
            cro.MissingBenefit = ContainsBenefit(combinedText) ? "false" : "true";
        }

        private void ExtractPrimaryConversionPath(HtmlDocument doc, HomepageExtractionContext context, HomepageCroSignalsSection cro)
        {
            cro.PrimaryCtaText = context.Hero?.PrimaryCtaText;
            cro.PrimaryCtaHref = context.Hero?.PrimaryCtaHref;

            var aboveFoldCtaButtons = doc.DocumentNode.SelectNodes(
                "//button[contains(@class,'cta') or contains(@class,'btn') or contains(@class,'primary')]")?.Cast<HtmlNode>().ToList() ?? new List<HtmlNode>();
            var aboveFoldCtaLinks = doc.DocumentNode.SelectNodes(
                "//a[contains(@class,'cta') or contains(@class,'btn') or contains(@class,'primary')]")?.Cast<HtmlNode>().ToList() ?? new List<HtmlNode>();

            var totalCtasAboveFold = aboveFoldCtaButtons.Count + aboveFoldCtaLinks.Count;
            cro.PrimaryCtaCountAboveFold = Math.Min(totalCtasAboveFold, 1);

            var distinctCtaTexts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var node in aboveFoldCtaButtons.Union(aboveFoldCtaLinks))
            {
                var text = HomepageHtmlHelper.CleanText(node.InnerText);
                if (!string.IsNullOrWhiteSpace(text))
                {
                    distinctCtaTexts.Add(text);
                }
            }
            cro.ConflictingCtaCountAboveFold = Math.Max(0, distinctCtaTexts.Count - 1);

            if (!string.IsNullOrWhiteSpace(cro.PrimaryCtaText))
            {
                cro.CtaVerbStrength = DetermineCTAVerbStrength(cro.PrimaryCtaText);
            }

            var ctaContext = doc.DocumentNode.SelectSingleNode(
                "//*[contains(@class,'cta') or contains(@class,'hero')]")?.InnerText ?? "";
            cro.HasRiskReversalNearCta = ContainsRiskReversal(ctaContext);
        }

        private void ExtractTrustSignals(HtmlDocument doc, HomepageExtractionContext context, HomepageCroSignalsSection cro)
        {
            cro.HasTestimonials = context.Testimonials?.HasTestimonials ?? false;
            cro.TestimonialCount = context.Testimonials?.TestimonialCount ?? 0;

            var pageText = doc.DocumentNode.InnerText;
            var ratingMatch = Regex.Match(pageText, @"(\d+\.?\d*)\s*(?:\/|out of)\s*5", RegexOptions.IgnoreCase);
            if (ratingMatch.Success && double.TryParse(ratingMatch.Groups[1].Value, out var rating))
            {
                cro.HasRatings = true;
                cro.AvgRatingIfFound = rating.ToString("F1");
            }

            cro.HasLogosOrPressMentions = ContainsPattern(pageText, @"(trusted by|as seen in|featured in|logo)", RegexOptions.IgnoreCase);
            cro.HasSecurityOrPaymentBadges = ContainsPattern(pageText, 
                @"(secure checkout|ssl|visa|mastercard|paypal|stripe|shopify|amex)", RegexOptions.IgnoreCase);
            cro.HasClearContactInfo = ContainsContactInfo(doc);
        }

        private void ExtractFrictionAndUx(HtmlDocument doc, HomepageExtractionContext context, HomepageCroSignalsSection cro)
        {
            cro.NavLinkCount = context.Nav?.LinkCount ?? 0;
            cro.HasSearch = context.Nav?.HasSearch ?? false;

            var modalPatterns = doc.DocumentNode.SelectNodes("//*[contains(@class,'modal') or contains(@class,'popup') or contains(@class,'overlay')]")?.Cast<HtmlNode>().ToList() ?? new List<HtmlNode>();
            cro.PopupOrModalOnLoad = modalPatterns.Count > 0;

            var firstForm = doc.DocumentNode.SelectSingleNode("//form");
            if (firstForm != null)
            {
                var inputs = firstForm.SelectNodes(".//input | .//textarea | .//select")?.Cast<HtmlNode>().ToList() ?? new List<HtmlNode>();
                cro.FormFieldCountOnFirstStep = inputs.Count;
            }

            cro.BrokenOrEmptyCtaLinks = CountBrokenCtaLinks(doc);
            cro.MobileViewportOk = doc.DocumentNode.SelectSingleNode("//meta[@name='viewport']") != null;
            cro.ImageAltCoveragePct = CalculateImageAltCoveragePct(doc);

            var intrusiveCount = (modalPatterns.Count > 0 ? 1 : 0)
                + (doc.DocumentNode.SelectSingleNode("//*[contains(@class,'banner')]") != null ? 1 : 0)
                + (doc.DocumentNode.SelectSingleNode("//*[contains(@class,'cookie')]") != null ? 1 : 0);
            cro.IntrusiveElementsCount = intrusiveCount;
        }

        private void ExtractOfferClarity(HtmlDocument doc, HomepageExtractionContext context, HomepageCroSignalsSection cro)
        {
            var pageText = doc.DocumentNode.InnerText;

            cro.PricingVisibleOnHome = ContainsPattern(pageText, @"\$\d+|price", RegexOptions.IgnoreCase);
            cro.StartsAtPricePresent = ContainsPattern(pageText, @"starts at|from \$|starting at", RegexOptions.IgnoreCase);
            cro.DiscountOrPromoPresent = ContainsPattern(pageText, @"discount|promo|offer|% off|sale", RegexOptions.IgnoreCase);
            cro.ShippingOrReturnsHighlighted = ContainsPattern(pageText, @"free shipping|returns|exchange", RegexOptions.IgnoreCase);

            var hasUrgency = ContainsPattern(pageText, @"limited time|today only|expires|act now|hurry|while supplies last", RegexOptions.IgnoreCase);
            cro.UrgencyElementsPresent = hasUrgency;

            if (hasUrgency)
            {
                var hasRealisticUrgency = ContainsPattern(pageText, @"\d{1,2}/\d{1,2}|\d{1,2}:\d{2}|[A-Z][a-z]{2}\s+\d{1,2}", RegexOptions.IgnoreCase);
                cro.UrgencyQuality = hasRealisticUrgency ? "realistic" : "manipulative";
            }
            else
            {
                cro.UrgencyQuality = "none";
            }
        }

        private void ExtractContentCompleteness(HtmlDocument doc, HomepageExtractionContext context, HomepageCroSignalsSection cro)
        {
            var features = ExtractFeatureBullets(doc);
            cro.FeatureBulletsCount = Math.Min(features.Count, 8);
            cro.FeatureSpecificityScore = DetermineSpecificityScore(features);

            var pageText = doc.DocumentNode.InnerText;
            cro.FaqPresent = ContainsPattern(pageText, @"faq|frequently asked|how it works", RegexOptions.IgnoreCase);
            cro.ObjectionHandlingPresent = ContainsPattern(pageText, @"why|because|benefit|advantage|objection", RegexOptions.IgnoreCase);
        }

        private List<string> ExtractFeatureBullets(HtmlDocument doc)
        {
            var features = new List<string>();

            var ulElements = doc.DocumentNode.SelectNodes("//ul | //ol")?.Cast<HtmlNode>().ToList() ?? new List<HtmlNode>();
            foreach (var ul in ulElements)
            {
                var lis = ul.SelectNodes(".//li")?.Cast<HtmlNode>().ToList() ?? new List<HtmlNode>();
                foreach (var li in lis)
                {
                    if (features.Count >= 8) break;
                    var text = HomepageHtmlHelper.CleanText(li.InnerText);
                    if (!string.IsNullOrWhiteSpace(text) && text.Length > 5)
                    {
                        var truncated = Truncate(text, 300);
                        if (!features.Contains(truncated))
                        {
                            features.Add(truncated);
                        }
                    }
                }
                if (features.Count >= 8) break;
            }

            return features;
        }

        private List<string> ExtractKeywords(string text, int maxCount)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            var tokens = Regex.Split(text.ToLowerInvariant(), @"\W+")
                .Where(t => !string.IsNullOrWhiteSpace(t) && t.Length > 2 && !StopWords.Contains(t))
                .ToList();

            var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var token in tokens)
            {
                if (freq.ContainsKey(token))
                    freq[token]++;
                else
                    freq[token] = 1;
            }

            return freq.OrderByDescending(x => x.Value).Take(maxCount).Select(x => x.Key).ToList();
        }

        private string? DetectAudienceHint(string text)
        {
            var lowerText = text.ToLowerInvariant();
            foreach (var keyword in AudienceKeywords)
            {
                if (lowerText.Contains(keyword))
                    return keyword;
            }

            var forMatch = Regex.Match(text, @"for\s+(\w+)", RegexOptions.IgnoreCase);
            if (forMatch.Success)
                return forMatch.Groups[1].Value;

            return null;
        }

        private bool ContainsVagueLanguage(string text)
        {
            return ContainsPattern(text, @"(amazing|wonderful|great|nice|good|best|awesome|incredible)", RegexOptions.IgnoreCase);
        }

        private bool ContainsJargon(string text)
        {
            return ContainsPattern(text, @"(leverage|synergy|paradigm|blockchain|ai-powered|machine learning|cutting-edge)", RegexOptions.IgnoreCase);
        }

        private bool ContainsBenefit(string text)
        {
            return ContainsPattern(text, @"(save|increase|reduce|improve|faster|easier|more|better|benefit|advantage)", RegexOptions.IgnoreCase);
        }

        private bool ContainsRiskReversal(string text)
        {
            return ContainsPattern(text, @"(trial|guarantee|money.back|cancel anytime|no credit card|free|risk.free|satisfaction)", RegexOptions.IgnoreCase);
        }

        private bool ContainsContactInfo(HtmlDocument doc)
        {
            var pageHtml = doc.DocumentNode.OuterHtml;
            return ContainsPattern(pageHtml, @"(mailto:|tel:|contact|email|phone|\+\d{1,3})", RegexOptions.IgnoreCase);
        }

        private bool ContainsPattern(string text, string pattern, RegexOptions options)
        {
            return !string.IsNullOrWhiteSpace(text) && Regex.IsMatch(text, pattern, options);
        }

        private string DetermineCTAVerbStrength(string ctaText)
        {
            var lower = ctaText.ToLowerInvariant();
            if (StrongCtaVerbs.Any(v => lower.Contains(v)))
                return "strong";
            if (WeakCtaVerbs.Any(v => lower.Contains(v)))
                return "weak";
            return "ok";
        }

        private int CountBrokenCtaLinks(HtmlDocument doc)
        {
            var broken = 0;
            var ctaLinks = doc.DocumentNode.SelectNodes("//a[@href=''] | //a[@href='#'] | //a[@href='javascript:void(0)']")?.Cast<HtmlNode>().ToList() ?? new List<HtmlNode>();
            broken += ctaLinks.Count;

            var noHref = doc.DocumentNode.SelectNodes("//a[not(@href)]")?.Cast<HtmlNode>().ToList() ?? new List<HtmlNode>();
            broken += noHref.Count;

            return Math.Min(broken, 100);
        }

        private int CalculateImageAltCoveragePct(HtmlDocument doc)
        {
            var allImages = doc.DocumentNode.SelectNodes("//img")?.Cast<HtmlNode>().ToList() ?? new List<HtmlNode>();
            if (allImages.Count == 0)
                return 100;

            var withAlt = allImages.Count(img => !string.IsNullOrWhiteSpace(img.GetAttributeValue("alt", "")));
            return (int)((double)withAlt / allImages.Count * 100);
        }

        private string DetermineSpecificityScore(List<string> features)
        {
            if (features.Count == 0)
                return "generic";

            var concreteness = 0;
            var genericness = 0;

            foreach (var feature in features)
            {
                var lower = feature.ToLowerInvariant();
                if (Regex.IsMatch(lower, @"\d+(%|x|%|ms|gb|core)") || 
                    Regex.IsMatch(lower, @"(\d+\.\d+|[a-z]+\d+[a-z]*)", RegexOptions.IgnoreCase))
                {
                    concreteness++;
                }
                if (Regex.IsMatch(lower, @"(amazing|great|best|easy|simple|powerful|advanced)"))
                {
                    genericness++;
                }
            }

            var concreteRatio = concreteness / (double)features.Count;
            if (concreteRatio > 0.5)
                return "concrete";
            if (genericness / (double)features.Count > 0.5)
                return "generic";
            return "mixed";
        }

        private void AddEvidenceEntries(HomepageExtractionContext context, HomepageCroSignalsSection cro)
        {
            if (!string.IsNullOrWhiteSpace(cro.HeroHeadline))
            {
                context.Evidence.Add(new HighConvertAI.Server.Models.ProductExtraction.ExtractionEvidence
                {
                    Section = "CroSignals",
                    SourceType = "dom",
                    SelectorOrPath = "h1 / hero headline",
                    ExtractedValue = Truncate(cro.HeroHeadline, 200)
                });
            }

            if (!string.IsNullOrWhiteSpace(cro.PrimaryCtaText))
            {
                context.Evidence.Add(new HighConvertAI.Server.Models.ProductExtraction.ExtractionEvidence
                {
                    Section = "CroSignals",
                    SourceType = "dom",
                    SelectorOrPath = "primary cta",
                    ExtractedValue = $"text={cro.PrimaryCtaText}, strength={cro.CtaVerbStrength}, riskReversal={cro.HasRiskReversalNearCta}"
                });
            }

            if (cro.ValuePropKeywords.Count > 0)
            {
                context.Evidence.Add(new HighConvertAI.Server.Models.ProductExtraction.ExtractionEvidence
                {
                    Section = "CroSignals",
                    SourceType = "text",
                    SelectorOrPath = "value prop keywords",
                    ExtractedValue = string.Join(", ", cro.ValuePropKeywords)
                });
            }

            if (!string.IsNullOrWhiteSpace(cro.AudienceMatchHint))
            {
                context.Evidence.Add(new HighConvertAI.Server.Models.ProductExtraction.ExtractionEvidence
                {
                    Section = "CroSignals",
                    SourceType = "text",
                    SelectorOrPath = "audience hint",
                    ExtractedValue = cro.AudienceMatchHint
                });
            }
        }

        private string Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;
            if (value.Length <= maxLength)
                return value;
            return value.Substring(0, maxLength) + "...";
        }
    }
}
