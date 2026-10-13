    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    namespace HighConvertAI.Server.Models.DatabaseModels
    {
        public class PageScan
        {
            public int Id { get; set; }
            public Guid? UserId { get; set; }

            [ForeignKey(nameof(UserId))]
            public User? User { get; set; }
            public string? IpAddress { get; set; }
            public string Url { get; set; } = string.Empty;
            public string? HtmlHash { get; set; }
            public string PageType { get; set; } = string.Empty;
            public decimal Score { get; set; } = 0;
            public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

            [Column(TypeName = "nvarchar(max)")]
            public string? ResultJson { get; set; }

            [Column(TypeName = "nvarchar(max)")]
            public string? ResultJsonUnAuthorised { get; set; }

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
            public decimal AffectedRevenueOpportunity { get; set; }


            [NotMapped]
            public ICollection<IssueGroup>? IssueGroup { get; set; }

            public PageScanDetail? PageScanDetail { get; set; }
        }
    }
