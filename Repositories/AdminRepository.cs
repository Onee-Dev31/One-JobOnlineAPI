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

            string sql = @"
                    INSERT INTO AdminUsers (Username, Password, Role)
                    VALUES (@Username, @Password, @Role);
                    SELECT CAST(SCOPE_IDENTITY() as int)";
            var id = await db.QuerySingleAsync<int>(sql, admin);
            return id;
        }

        public async Task<bool> VerifyPasswordAsync(string username, string password)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            string sql = "SELECT Password FROM AdminUsers WHERE Username = @Username";
            var storedHashedPassword = await db.QueryFirstOrDefaultAsync<string>(sql, new { Username = username });

            if (storedHashedPassword == null)
                return false;

            return BCrypt.Net.BCrypt.Verify(password, storedHashedPassword);
        }

        public async Task<AdminUser?> GetAdminUserByUsernameAsync(string username)
        {
            using IDbConnection db = new SqlConnection(_connectionString);
            var query = "SELECT * FROM AdminUsers WHERE Username = @Username";

            try
            {
                return await db.QuerySingleOrDefaultAsync<AdminUser>(query, new { Username = username });
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
            parameters.Add("@HRID", request.HRID);
            parameters.Add("@EMAIL", request.EMAIL);
            parameters.Add("@Department", request.Department);
            parameters.Add("@EmpNo", request.EmpNo);
            parameters.Add("@NameThai", request.NameThai);
            parameters.Add("@Mobile", request.Mobile);
            parameters.Add("@Position", request.Position);
            parameters.Add("@CompanyName", request.CompanyName);
            parameters.Add("@RoleID", request.RoleID);
            return await db.QuerySingleAsync<int>("sp_CreateAdminUser", parameters, commandType: CommandType.StoredProcedure);
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
    }
}