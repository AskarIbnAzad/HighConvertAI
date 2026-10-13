using HighConvertAI.Server.Data;
using HighConvertAI.Server.Models.DatabaseModels;
using HighConvertAI.Server.Models.DTO;
using HighConvertAI.Server.Services.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;


namespace HighConvertAI.Server.Services
{
    public sealed class AuditService : IAuditService
    {
        private readonly ApplicationDbContext _db;
        public AuditService(ApplicationDbContext db) 
        {
            _db = db;   
        }
        public async Task<List<AuditDto>> GetAllAudits()
        {
            var scans = await _db.PageScans
                .Include(p => p.User)
                    .ThenInclude(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                .ToListAsync();

            var result = new List<AuditDto>();

            foreach (var scan in scans)
            {
                result.Add(new AuditDto
                {
                    Id = scan.Id,
                    UserName = scan.User?.FullName ?? "Guest User",
                    Role = scan.User?.UserRoles
                                .Select(r => r.Role.Name)
                                .FirstOrDefault() ?? "Guest",
                    Url = scan.Url,
                    PageType = scan.PageType,
                    Score = scan.Score,
                    CreatedAtUtc = scan.CreatedAtUtc
                });
            }

            return result.OrderByDescending(p => p.Id).ToList();
        }
       
        public async Task<List<AuditDto>> GetAuditsByUserId(Guid userID)
        {
            var scans = await _db.PageScans.Where(x => x.UserId == userID)
                 .Include(p => p.User)
                     .ThenInclude(u => u.UserRoles)
                         .ThenInclude(ur => ur.Role)
                 .ToListAsync();

            var result = new List<AuditDto>();
            foreach (var scan in scans)
            {
                result.Add(new AuditDto
                {
                    Id = scan.Id,
                    UserName = scan.User!.FullName,
                    Role = scan.User?.UserRoles
                                .Select(r => r.Role.Name)
                                .FirstOrDefault() ?? "",
                    Url = scan.Url,
                    PageType = scan.PageType,
                    Score = scan.Score,
                    CreatedAtUtc = scan.CreatedAtUtc
                });
            }
            return result.OrderByDescending(p=>p.Id).ToList();
        }

        public async Task<AuditDto?> GetSecuredAuditById(int id, Guid? userId)
        {

            var pageScan = await _db.PageScans.FirstOrDefaultAsync(x => x.Id == id);
            if (pageScan == null)
                return null;

            var jsonToDeserialize = userId.HasValue ? pageScan.ResultJson : pageScan.ResultJsonUnAuthorised;
            if (string.IsNullOrEmpty(jsonToDeserialize))
                return null;

            var resultJson = JsonSerializer.Deserialize<object>(jsonToDeserialize);
            return new AuditDto
            {
                Id = pageScan.Id,
                Url = pageScan.Url,
                PageType = pageScan.PageType,
                Score = pageScan.Score,
                CreatedAtUtc = pageScan.CreatedAtUtc,
                ResultJson = resultJson!
            };
        }
        public async Task<PageScan> GetAuditById(int id)
        {

            var pageScan = await _db.PageScans.FirstOrDefaultAsync(x => x.Id == id);
            return pageScan!;

        }
    }
}