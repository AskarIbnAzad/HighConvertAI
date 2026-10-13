namespace HighConvertAI.Server.Models.CatalogExtraction.Sections
{
   
        public sealed class CatalogFiltersSection
        {
            public bool HasFilters { get; set; }
            public int FilterGroupCount { get; set; }

            public bool HasPriceFilter { get; set; }
            public bool HasAvailabilityFilter { get; set; }
            public bool HasVariantFilters { get; set; }

            public int ActiveFilterCount { get; set; }
            public bool HasClearFiltersAction { get; set; }

            public IReadOnlyList<CatalogFilterGroup> Groups { get; set; } = [];
        }
    
}
