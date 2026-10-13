using HighConvertAI.Server.Data;
using HighConvertAI.Server.Models.DTO;
using HighConvertAI.Server.Services.Abstractions;
using Microsoft.EntityFrameworkCore;



namespace HighConvertAI.Server.Services
{
    public sealed class UserService : IUserService
    {
        private readonly ApplicationDbContext _db;

        public UserService(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<List<UserDto>> GetUsers()
        {
            return await _db.Users
                .Include(x => x.UserRoles)
                    .ThenInclude(x => x.Role)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    FullName = u.FullName,
                    Email = u.Email,
                    IsEmailVerified = u.IsEmailVerified,
                    Roles = u.UserRoles
                        .Select(x => x.Role.Name)
                        .ToList()
                })
                .ToListAsync();
        }
    }
}