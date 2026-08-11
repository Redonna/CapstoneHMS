using HospitalManagementSystem.API.Models;

namespace HospitalManagementSystem.API.Repositories.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByUsernameAnyStatusAsync(string username);
        Task<User> CreateAsync(User user);
        Task<bool> UsernameExistsAsync(string username);
        Task DeactivateByEmailAsync(string email);
        Task SetResetTokenAsync(int userId, string token, DateTime expiresAt);
        Task<User?> GetByResetTokenAsync(string token);
        Task ResetPasswordAsync(int userId, string newPasswordHash);
    }
}
