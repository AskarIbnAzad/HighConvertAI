using HighConvertAI.Server.Models.CatalogExtraction;
using HighConvertAI.Server.Models.CatalogExtraction.Sections;
using HighConvertAI.Server.Services.CatalogExtraction.Abstractions;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace HighConvertAI.Server.Services.CatalogExtraction.Extractors
{
    public sealed class CatalogPaginationExtractor : ICatalogSectionExtractor
    {
        public int Order => 50;

        public void Extract(CatalogExtractionContext context)
        {
            var doc = context.Document;

            var paginationNav =
                doc.DocumentNode.SelectSingleNode("//nav[contains(translate(@aria-label,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'pagination')]") ??
                doc.DocumentNode.SelectSingleNode("//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'pagination')]");

            var loadMoreButton =
                doc.DocumentNode.SelectSingleNode("//button[contains(translate(text(),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'load more')]") ??
                doc.DocumentNode.SelectSingleNode("//a[contains(translate(text(),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'load more')]") ??
                doc.DocumentNode.SelectSingleNode("//button[contains(translate(text(),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'show more')]");

            var infiniteScrollHint =
                doc.DocumentNode.SelectSingleNode("//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'infinite')]") ??
                doc.DocumentNode.SelectSingleNode("//*[@data-infinite-scroll]");

            var currentPageText =
                GetNodeText(doc.DocumentNode.SelectSingleNode("//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'current')]")) ??
                GetNodeText(doc.DocumentNode.SelectSingleNode("//nav//*[self::span or self::a][@aria-current='page']"));

            context.Pagination = new CatalogPaginationSection
            {
                HasPagination = paginationNav != null,
                HasLoadMore = loadMoreButton != null,
                HasInfiniteScrollHint = infiniteScrollHint != null,
                CurrentPageText = currentPageText
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
