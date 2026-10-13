using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighConvertAI.Server.Models.DatabaseModels
{
    public class IssueSeverity
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public int AffectedScore { get; set; } = 0;
        public int AffectedRevenueOpportunity { get; set; } = 0;
        public int SortingOrder { get; set; } = 0;

    }
}
