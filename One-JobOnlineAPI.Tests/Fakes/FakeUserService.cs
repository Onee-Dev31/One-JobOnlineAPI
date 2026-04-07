using JobOnlineAPI.Models;
using JobOnlineAPI.Services;

namespace JobOnlineAPI.Tests.Fakes;

public class FakeUserService : IUserService
{
    public Task<AdminUser?> AuthenticateAsync(string username, string password, int jobId)
    {
        if (username == "test@test.com" && password == "password123")
        {
            return Task.FromResult<AdminUser?>(new AdminUser
            {
                Username = "test@test.com",
                Password = "hashed",
                Role = "applicant",
                UserId = 1,
                JobID = jobId
            });
        }
        return Task.FromResult<AdminUser?>(null);
    }

    public Task<string?> GetConfigValueAsync(string key) => Task.FromResult<string?>(null);
    public Task<string?> GetStyleValueAsync(string key) => Task.FromResult<string?>(null);
}
