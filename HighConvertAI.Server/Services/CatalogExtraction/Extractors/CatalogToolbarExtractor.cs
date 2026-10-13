using HighConvertAI.Server.Models.CatalogExtraction;
using HighConvertAI.Server.Models.CatalogExtraction.Sections;
using HighConvertAI.Server.Services.CatalogExtraction.Abstractions;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace HighConvertAI.Server.Services.CatalogExtraction.Extractors
{
    public sealed class CatalogToolbarExtractor : ICatalogSectionExtractor
    {
        public int Order => 20;

        public void Extract(CatalogExtractionContext context)
        {
            var doc = context.Document;

            var sortSelect = doc.DocumentNode.SelectSingleNode("//select[contains(@name,'sort_by')]");
            var selectedSort =
                GetNodeText(sortSelect?.SelectSingleNode(".//option[@selected]")) ??
                GetNodeText(sortSelect?.SelectSingleNode(".//option"));

            var sortButton = doc.DocumentNode.SelectSingleNode("//*[contains(translate(@aria-label,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'sort')]")
                             ?? doc.DocumentNode.SelectSingleNode("//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'sort')]");

            var resultCountNode =
                doc.DocumentNode.SelectSingleNode("//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'result-count')]") ??
                doc.DocumentNode.SelectSingleNode("//*[@data-result-count]");

            var searchNode =
                doc.DocumentNode.SelectSingleNode("//main//input[@type='search']") ??
                doc.DocumentNode.SelectSingleNode("//main//input[@name='q']") ??
                doc.DocumentNode.SelectSingleNode("//*[@data-collection-search]");

            context.Toolbar = new CatalogToolbarSection
            {
                HasSortControl = sortSelect != null || sortButton != null,
                SelectedSortText = selectedSort,
                HasResultCount = resultCountNode != null,
                ResultCountText = GetNodeText(resultCountNode),
                HasSearchWithinCollection = searchNode != null
            };
        }

        private static string? GetNodeText(HtmlNode? node)
        {
            if (node == null)
                return null;

            var raw = HtmlEntity.DeEntitize(node.InnerText);
            raw = Regex.Replace(raw, @"\s+", " ").Trim();

            return string.IsNullOrWhiteSpace(raw) ? null : raw;
        }
    }
}
