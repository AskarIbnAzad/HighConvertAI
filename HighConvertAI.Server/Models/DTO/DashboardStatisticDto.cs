using System.Runtime.ConstrainedExecution;

namespace HighConvertAI.Server.Models.DTO
{
    public class DashboardStatisticDto
    {
        public int TotalUser { get; set; }
        public int TotalAudits { get; set; }
        public int TotalAnonymousScans { get; set; }
        public double AvgProcessingTimeMs { get; set; }
        public int ScansThisWeek { get; set; }
        public int NewUsersThisWeek { get; set; }
        public List<WeeklyStat> ScansPerWeek { get; set; }   // trend, last 8 weeks
        public List<WeeklyStat> NewUsersPerWeek { get; set; } // trend, last 8 weeks
        public List<LocationStatistic> LocationData { get; set; }
    }

    public class WeeklyStat
    {
        public DateTime WeekStart { get; set; }
        public int Count { get; set; }
    }

    public class LocationStatistic
    {
        public string Country { get; set; }
        public string City { get; set; }
        public int Count { get; set; }
    }

}
