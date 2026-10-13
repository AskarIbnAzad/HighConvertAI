using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighConvertAI.Server.Models.DatabaseModels
{
    public class IssueGroup
    {
        public int Id { get; set; }
        public required string Name { get; set; }
        public string Detail { get; set; } = string.Empty;
        public int SortingOrder { get; set; } = 0;
        [NotMapped]
        public int AffectedRevenueOpportunity { get; set; } = 0;

        [NotMapped]
        public int AffectedScore { get; set; } = 0;

        [NotMapped]
        public int IssueCount { get; set; }
        [NotMapped]
        public int HighCount { get; set; }
        [NotMapped]
        public int MediumCount { get; set; }
        [NotMapped]
        public int LowCount { get; set; }

        [NotMapped]
        public int? IssueSeverityId { get; set; }

        [NotMapped]
        public IssueSeverity? IssueSeverity { get; set; }

        [NotMapped]
        public ICollection<Issue>? Issues { get; set; }
    }
}
