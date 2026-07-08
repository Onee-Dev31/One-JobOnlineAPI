using JobOnlineAPI.Models;

namespace JobOnlineAPI.Repositories
{
    public interface IAdminRepository
    {
        Task<int> AddAdminUserAsync(AdminUser admin);
        Task<bool> VerifyPasswordAsync(string username, string password);
        Task<AdminUser?> GetAdminUserByUsernameAsync(string username);
        Task<User?> GetUserByEmailAsync(string email, int JobID);
        Task<string?> GetConfigValueAsync(string key);
        Task<string?> GetStyleValueAsync(string key);
        bool VerifySHA256Hash(string input, string storedHash);

        Task<IEnumerable<AdminUserDetail>> GetAllAdminUsersAsync();
        Task<IEnumerable<AdminRole>> GetAllRolesAsync();
        Task<IEnumerable<RolePermissionFlat>> GetAllRolePermissionsAsync();
        Task<IEnumerable<string>> GetRoutesByRoleNameAsync(string roleName);
        Task<IEnumerable<RouteDetail>> GetRoutesByRoleNameWithDetailAsync(string roleName);
        Task UpdateRoutesSortOrderAsync(List<RoutesSortOrderItem> items);
        Task<AdminUserDetail?> GetAdminUserByIdAsync(int id);
        Task<int> CreateAdminUserAsync(AdminUserCreateRequest request);
        Task<SecretaryCreateResult> CreateSecretaryAdminUserAsync(AdminUserCreateRequest request);
        Task<bool> UpdateAdminUserAsync(int id, AdminUserUpdateRequest request);
        Task<bool> DeleteAdminUserAsync(int id);
        Task<IEnumerable<string>> GetDependentSecretaryNamesAsync(int adminId);
        Task<bool> SetAdminUserActiveAsync(int id, bool isActive);
    }
}