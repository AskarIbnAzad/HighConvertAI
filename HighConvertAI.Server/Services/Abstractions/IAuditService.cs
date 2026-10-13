using HighConvertAI.Server.Models.ApiResponse;
using HighConvertAI.Server.Models.DatabaseModels;
using HighConvertAI.Server.Models.DTO;
using HighConvertAI.Server.Models.Scanning;

namespace HighConvertAI.Server.Services.Abstractions
{
    public interface IAuditService
    {
        Task<List<AuditDto>> GetAllAudits();
        Task<List<AuditDto>> GetAuditsByUserId(Guid userId);
        Task<AuditDto?> GetSecuredAuditById(int id, Guid? userId);
        Task<PageScan> GetAuditById(int id);

    }
}
