using HtmlAgilityPack;
using System.Text.RegularExpressions;
using System.Web;

namespace HighConvertAI.Server.Services.HomepageExtraction.Helpers
{
    internal static partial class HomepageHtmlHelper
    {
        public static string? CleanText(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var cleaned = HttpUtility.HtmlDecode(value);
            cleaned = WhitespaceRegex().Replace(cleaned, " ").Trim();

            return string.IsNullOrWhiteSpace(cleaned) ? null : cleaned;
        }

        public static string? GetMetaContent(HtmlDocument doc, string attributeName, string attributeValue)
        {
            var xpath =
                $"//meta[translate(@{attributeName}, 'ABCDEFGHIJKLMNOPQRSTUVWXYZ', 'abcdefghijklmnopqrstuvwxyz')='{attributeValue.ToLowerInvariant()}']";

            var node = doc.DocumentNode.SelectSingleNode(xpath);
            return CleanText(node?.GetAttributeValue("content", null));
        }

        public static string? GetH1Text(HtmlDocument doc)
        {
            var node = doc.DocumentNode.SelectSingleNode("//main//h1") ??
                       doc.DocumentNode.SelectSingleNode("//h1");

            return CleanText(node?.InnerText);
        }

        public static string? GetTextByClassContains(HtmlDocument doc, string classPart)
        {
            var xpath =
                $"//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'{classPart.ToLowerInvariant()}')]";

            var node = doc.DocumentNode.SelectSingleNode(xpath);
            return CleanText(node?.InnerText);
        }

        [GeneratedRegex(@"\s+")]
        private static partial Regex WhitespaceRegex();
    }
}
