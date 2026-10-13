using HtmlAgilityPack;

namespace HighConvertAI.Server.Services.HomepageExtraction.Helpers
{
    internal static class MetaTagHelper
    {
        public static string? GetPageTitle(HtmlDocument doc)
        {
            var titleNode = doc.DocumentNode.SelectSingleNode("//title");
            var title = titleNode?.InnerText;

            if (!string.IsNullOrWhiteSpace(title))
                return HomepageHtmlHelper.CleanText(title);

            var og = HomepageHtmlHelper.GetMetaContent(doc, "property", "og:title");
            if (!string.IsNullOrWhiteSpace(og))
                return og;

            return null;
        }

        public static string? GetDescription(HtmlDocument doc)
        {
            var desc = HomepageHtmlHelper.GetMetaContent(doc, "name", "description");
            if (!string.IsNullOrWhiteSpace(desc))
                return desc;

            return HomepageHtmlHelper.GetMetaContent(doc, "property", "og:description");
        }
    }
}
