using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using JobOnlineAPI.Models;
using System.Security.Cryptography;
using System.Text;

namespace JobOnlineAPI.Repositories
{
    public class AdminRepository(IConfiguration configuration) : IAdminRepository
    {
        private readonly string? _connectionString = configuration.GetConnectionString("DefaultConnection");

        public async Task<int> AddAdminUserAsync(AdminUser admin)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(admin.Password);
            admin.Password = hashedPassword;

            var parameters = new DynamicParameters();
            parameters.Add("@Username", admin.Username);
            parameters.Add("@Password", admin.Password);
            parameters.Add("@Role", admin.Role);
            return await db.QuerySingleAsync<int>("sp_AddAdminUser", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<bool> VerifyPasswordAsync(string username, string password)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@Username", username);
            var storedHashedPassword = await db.QueryFirstOrDefaultAsync<string>("sp_GetAdminPasswordByUsername", parameters, commandType: CommandType.StoredProcedure);

            if (storedHashedPassword == null)
                return false;

            return BCrypt.Net.BCrypt.Verify(password, storedHashedPassword);
        }

        public async Task<AdminUser?> GetAdminUserByUsernameAsync(string username)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@Username", username);

            try
            {
                return await db.QuerySingleOrDefaultAsync<AdminUser>("sp_GetAdminUserByUsername", parameters, commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAdminUserByUsernameAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<User?> GetUserByEmailAsync(string email, int JobID)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            //sp_Userlogin
            var query = "sp_UserloginNew";

            try
            {
                return await db.QuerySingleOrDefaultAsync<User>(
                    query,
                    new { Email = email, JobID, UseBypass = true },
                    commandType: CommandType.StoredProcedure
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetUserByEmailAsync: {ex.Message}");
                return null;
            }
        }

        public async Task<string?> GetConfigValueAsync(string key)
        {
            using var conn = new SqlConnection(_connectionString);
            string sql = "GetConfigValue";
            var configValue = await conn.QueryFirstOrDefaultAsync<string>(
                sql,
                new { ConfigKey = key },
                commandType: System.Data.CommandType.StoredProcedure);

            return configValue;
        }

        public async Task<string?> GetStyleValueAsync(string key)
        {
            using var conn = new SqlConnection(_connectionString);
            string sql = "GetStyleValue";
            var styleValue = await conn.QueryFirstOrDefaultAsync<string>(
                sql,
                new { SettingName = key },
                commandType: CommandType.StoredProcedure);

            return styleValue;
        }

        public bool VerifySHA256Hash(string input, string storedHash)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] hashBytes = SHA256.HashData(inputBytes);
            string hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
            return hash == storedHash;
        }

        public async Task<IEnumerable<AdminUserDetail>> GetAllAdminUsersAsync()
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            return await db.QueryAsync<AdminUserDetail>("sp_GetAllAdminUsers", commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<AdminRole>> GetAllRolesAsync()
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            return await db.QueryAsync<AdminRole>("sp_GetAllRoles", commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<RolePermissionFlat>> GetAllRolePermissionsAsync()
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            return await db.QueryAsync<RolePermissionFlat>("sp_GetAllRolePermissions", commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<string>> GetRoutesByRoleNameAsync(string roleName)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@RoleName", roleName);
            return await db.QueryAsync<string>("sp_GetRoutesByRoleName", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<RouteDetail>> GetRoutesByRoleNameWithDetailAsync(string roleName)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@RoleName", roleName);
            return await db.QueryAsync<RouteDetail>("sp_GetRoutesByRoleNameWithDetail", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task UpdateRoutesSortOrderAsync(List<RoutesSortOrderItem> items)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var json = System.Text.Json.JsonSerializer.Serialize(items);
            var parameters = new DynamicParameters();
            parameters.Add("@payload", json);
            await db.ExecuteAsync("sp_UpdateRoutesSortOrder", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<RolePermissionItem>> GetAllRolePermissionsDetailAsync()
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            return await db.QueryAsync<RolePermissionItem>("sp_GetAllRolePermissionsDetail", commandType: CommandType.StoredProcedure);
        }

        public async Task<RolePermissionItem?> GetRolePermissionByRoleAndRouteAsync(int roleId, string routePath)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@RoleID", roleId);
            parameters.Add("@RoutePath", routePath);
            return await db.QuerySingleOrDefaultAsync<RolePermissionItem>("sp_GetRolePermissionByRoleAndRoute", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> CreateRolePermissionAsync(RolePermissionCreateRequest request)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@RoleID", request.RoleID);
            parameters.Add("@RoutePath", request.RoutePath);
            parameters.Add("@Label", request.Label);
            parameters.Add("@Icon", request.Icon);
            parameters.Add("@IsVisible", request.IsVisible);
            parameters.Add("@SortOrder", request.SortOrder);
            return await db.QuerySingleAsync<int>("sp_CreateRolePermission", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<bool> UpdateRolePermissionAsync(int id, RolePermissionUpdateRequest request)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@ID", id);
            parameters.Add("@Label", request.Label);
            parameters.Add("@Icon", request.Icon);
            parameters.Add("@IsVisible", request.IsVisible);
            var rows = await db.QuerySingleAsync<int>("sp_UpdateRolePermission", parameters, commandType: CommandType.StoredProcedure);
            return rows > 0;
        }

        public async Task<bool> DeleteRolePermissionAsync(int id)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@ID", id);
            var rows = await db.QuerySingleAsync<int>("sp_DeleteRolePermission", parameters, commandType: CommandType.StoredProcedure);
            return rows > 0;
        }

        public async Task<AdminUserDetail?> GetAdminUserByIdAsync(int id)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@AdminID", id);
            return await db.QuerySingleOrDefaultAsync<AdminUserDetail>("sp_GetAdminUserById", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> CreateAdminUserAsync(AdminUserCreateRequest request)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@Username", request.Username);
            parameters.Add("@HRID", request.HRID == 0 ? null : request.HRID);
            parameters.Add("@EMAIL", string.IsNullOrEmpty(request.EMAIL) ? null : request.EMAIL);
            parameters.Add("@Department", string.IsNullOrEmpty(request.Department) ? null : request.Department);
            parameters.Add("@EmpNo", string.IsNullOrEmpty(request.EmpNo) ? null : request.EmpNo);
            parameters.Add("@NameThai", string.IsNullOrEmpty(request.NameThai) ? null : request.NameThai);
            parameters.Add("@Mobile", string.IsNullOrEmpty(request.Mobile) ? null : request.Mobile);
            parameters.Add("@Position", string.IsNullOrEmpty(request.Position) ? null : request.Position);
            parameters.Add("@CompanyName", string.IsNullOrEmpty(request.CompanyName) ? null : request.CompanyName);
            parameters.Add("@RoleID", request.RoleID);
            parameters.Add("@CanViewAllCompanies", request.CanViewAllCompanies);
            return await db.QuerySingleAsync<int>("sp_CreateAdminUser", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<SecretaryCreateResult> CreateSecretaryAdminUserAsync(AdminUserCreateRequest request)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@Username", request.Username);
            parameters.Add("@HRID", request.HRID == 0 ? null : request.HRID);
            parameters.Add("@EMAIL", string.IsNullOrEmpty(request.EMAIL) ? null : request.EMAIL);
            parameters.Add("@Department", string.IsNullOrEmpty(request.Department) ? null : request.Department);
            parameters.Add("@EmpNo", string.IsNullOrEmpty(request.EmpNo) ? null : request.EmpNo);
            parameters.Add("@NameThai", string.IsNullOrEmpty(request.NameThai) ? null : request.NameThai);
            parameters.Add("@Mobile", string.IsNullOrEmpty(request.Mobile) ? null : request.Mobile);
            parameters.Add("@Position", string.IsNullOrEmpty(request.Position) ? null : request.Position);
            parameters.Add("@CompanyName", string.IsNullOrEmpty(request.CompanyName) ? null : request.CompanyName);
            parameters.Add("@RoleID", request.RoleID);
            parameters.Add("@ReportsToEmpNo", request.ReportsToEmpNo);
            parameters.Add("@CanViewAllCompanies", request.CanViewAllCompanies);
            return await db.QuerySingleAsync<SecretaryCreateResult>("sp_CreateSecretaryAdminUser", parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<bool> UpdateAdminUserAsync(int id, AdminUserUpdateRequest request)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@AdminID", id);
            parameters.Add("@EMAIL", request.EMAIL);
            parameters.Add("@Department", request.Department);
            parameters.Add("@NameThai", request.NameThai);
            parameters.Add("@Mobile", request.Mobile);
            parameters.Add("@Position", request.Position);
            parameters.Add("@CompanyName", request.CompanyName);
            parameters.Add("@RoleID", request.RoleID);
            parameters.Add("@IsActive", request.IsActive);
            parameters.Add("@ReportsToEmpNo", string.IsNullOrWhiteSpace(request.ReportsToEmpNo) ? null : request.ReportsToEmpNo);
            parameters.Add("@CanViewAllCompanies", request.CanViewAllCompanies);
            var rows = await db.QuerySingleAsync<int>("sp_UpdateAdminUser", parameters, commandType: CommandType.StoredProcedure);
            return rows > 0;
        }

        public async Task<bool> DeleteAdminUserAsync(int id)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@AdminID", id);
            var rows = await db.QuerySingleAsync<int>("sp_DeleteAdminUser", parameters, commandType: CommandType.StoredProcedure);
            return rows > 0;
        }

        public async Task<bool> SetAdminUserActiveAsync(int id, bool isActive)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@AdminID", id);
            parameters.Add("@IsActive", isActive);
            var rows = await db.QuerySingleAsync<int>("sp_SetAdminUserActive", parameters, commandType: CommandType.StoredProcedure);
            return rows > 0;
        }

        public async Task<IEnumerable<string>> GetDependentSecretaryNamesAsync(int adminId)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@AdminID", adminId);
            return await db.QueryAsync<string>("sp_GetDependentSecretaryNames", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}