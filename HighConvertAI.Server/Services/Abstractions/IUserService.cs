using HighConvertAI.Server.Models.DTO;

namespace HighConvertAI.Server.Services.Abstractions
{
    public interface IUserService
    {
        Task<List<UserDto>> GetUsers();
    }
}
