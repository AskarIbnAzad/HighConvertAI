using HighConvertAI.Server.Models.DTO;

namespace HighConvertAI.Server.Services.Abstractions
{
    public interface IDashboardService
    {
        Task<DashboardStatisticDto> GetAdminOverview();

    }
}
