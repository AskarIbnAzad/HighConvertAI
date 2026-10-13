using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighConvertAI.Server.Models.DatabaseModels
{
    public class Issue
    {
        public int Id { get; set; }
        public int? PageScanId { get; set; }

        [ForeignKey(nameof(PageScanId))]
        public PageScan? PageScan { get; set; }

        public int? IssueGroupId { get; set; }
                    
        [ForeignKey(nameof(IssueGroupId))]
        public IssueGroup? IssueGroup { get; set; }

        public int? IssueSeverityId { get; set; }

        [ForeignKey(nameof(IssueSeverityId))]
        public IssueSeverity? IssueSeverity { get; set; }

        public string Title { get; set; } = string.Empty;

        public string WhatWasRight { get; set; } = string.Empty;
        public string WhatToImprove { get; set; } = string.Empty;
        public string Recommendation { get; set; } = string.Empty;

    }
}
