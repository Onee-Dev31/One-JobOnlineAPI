using JobOnlineAPI.Models;
using JobOnlineAPI.Repositories;

namespace JobOnlineAPI.Tests.Fakes;

public class FakeAdminRepository : IAdminRepository
{
    public Task<User?> GetUserByEmailAsync(string email, int jobId)
    {
        if (email == "test@test.com")
        {
            return Task.FromResult<User?>(new User
            {
                UserId = 1,
                Email = "test@test.com",
                PasswordHash = "hashed",
                JobID = jobId
            });
        }
        return Task.FromResult<User?>(null);
    }

    public Task<int> AddAdminUserAsync(AdminUser admin) => Task.FromResult(1);
    public Task<bool> VerifyPasswordAsync(string username, string password) => Task.FromResult(true);
    public Task<AdminUser?> GetAdminUserByUsernameAsync(string username) => Task.FromResult<AdminUser?>(null);
    public Task<string?> GetConfigValueAsync(string key) => Task.FromResult<string?>(null);
    public Task<string?> GetStyleValueAsync(string key) => Task.FromResult<string?>(null);
    public bool VerifySHA256Hash(string input, string storedHash) => true;
}
