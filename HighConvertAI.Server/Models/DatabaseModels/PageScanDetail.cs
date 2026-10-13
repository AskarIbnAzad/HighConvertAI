using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HighConvertAI.Server.Models.DatabaseModels
{
    public class PageScanDetail
    {
        public int Id { get; set; }
        public int? PageScanId { get; set; }

        [ForeignKey(nameof(PageScanId))]
        public PageScan? PageScan { get; set; }

        [MaxLength(50)]
        public string? Country { get; set; }

        [MaxLength(100)]
        public string? City { get; set; }

        [MaxLength(200)]
        public string? Browser { get; set; }


        [MaxLength(100)]
        public string? DeviceType { get; set; }

        [MaxLength(100)]
        public string? OperatingSystem { get; set; }

        public int ProcessingTimeMs { get; set; } = 0;
    }
}
