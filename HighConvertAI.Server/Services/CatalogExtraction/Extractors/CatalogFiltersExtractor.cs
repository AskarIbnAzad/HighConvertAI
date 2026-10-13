using HighConvertAI.Server.Models.CatalogExtraction;
using HighConvertAI.Server.Models.CatalogExtraction.Sections;
using HighConvertAI.Server.Services.CatalogExtraction.Abstractions;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace HighConvertAI.Server.Services.CatalogExtraction.Extractors
{
    public sealed class CatalogFiltersExtractor : ICatalogSectionExtractor
    {
        public int Order => 30;

        public void Extract(CatalogExtractionContext context)
        {
            var doc = context.Document;

            var filterRoot =
                doc.DocumentNode.SelectSingleNode("//*[@data-facets]") ??
                doc.DocumentNode.SelectSingleNode("//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'facets')]") ??
                doc.DocumentNode.SelectSingleNode("//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'filter')]") ??
                doc.DocumentNode.SelectSingleNode("//form[contains(@action,'filter')]");

            var groups = new List<CatalogFilterGroup>();

            if (filterRoot != null)
            {
                var groupNodes = filterRoot.SelectNodes(".//details | .//fieldset | .//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'filter-group')]");

                if (groupNodes != null)
                {
                    foreach (var groupNode in groupNodes)
                    {
                        var name =
                            GetNodeText(groupNode.SelectSingleNode(".//summary")) ??
                            GetNodeText(groupNode.SelectSingleNode(".//legend")) ??
                            GetNodeText(groupNode.SelectSingleNode(".//h3")) ??
                            GetNodeText(groupNode.SelectSingleNode(".//h4"));

                        var optionCount =
                            (groupNode.SelectNodes(".//input | .//option | .//label")?.Count) ?? 0;

                        if (!string.IsNullOrWhiteSpace(name) || optionCount > 0)
                        {
                            groups.Add(new CatalogFilterGroup
                            {
                                Name = name,
                                OptionCount = optionCount
                            });
                        }
                    }
                }
            }

            var allGroupNames = string.Join(" | ", groups.Select(x => x.Name ?? string.Empty)).ToLowerInvariant();

            var activeFilterNodes =
                doc.DocumentNode.SelectNodes("//*[@data-active-facets]//*[self::a or self::button or self::span]") ??
                doc.DocumentNode.SelectNodes("//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'active-facets')]//*[self::a or self::button or self::span]") ??
                doc.DocumentNode.SelectNodes("//*[contains(translate(@class,'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'filter-chip')]");

            var clearFiltersNode =
                doc.DocumentNode.SelectSingleNode("//a[contains(translate(text(),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'clear')]") ??
                doc.DocumentNode.SelectSingleNode("//button[contains(translate(text(),'ABCDEFGHIJKLMNOPQRSTUVWXYZ','abcdefghijklmnopqrstuvwxyz'),'clear')]") ??
                doc.DocumentNode.SelectSingleNode("//*[@data-clear-filters]");

            context.Filters = new CatalogFiltersSection
            {
                HasFilters = filterRoot != null,
                FilterGroupCount = groups.Count,
                HasPriceFilter = allGroupNames.Contains("price"),
                HasAvailabilityFilter = allGroupNames.Contains("availability") || allGroupNames.Contains("stock"),
                HasVariantFilters = allGroupNames.Contains("size")
                                    || allGroupNames.Contains("color")
                                    || allGroupNames.Contains("colour")
                                    || allGroupNames.Contains("material")
                                    || allGroupNames.Contains("brand"),
                ActiveFilterCount = activeFilterNodes?.Count ?? 0,
                HasClearFiltersAction = clearFiltersNode != null,
                Groups = groups
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
