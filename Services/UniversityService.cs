using Dapper;
using JobOnlineAPI.Models;
using System.Data;
using System.Data.SqlClient;

namespace JobOnlineAPI.Services
{
    public class UniversityService(IConfiguration configuration) : IUniversityService
    {
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
                               ?? throw new InvalidOperationException("DefaultConnection is not configured in appsettings.json.");

        public async Task<IEnumerable<University>> GetUniversitiesAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<University>("sp_GetUniversities", commandType: CommandType.StoredProcedure);
        }

        public async Task<IEnumerable<University>> GetUniversitiesAdminAsync(bool includeInactive)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<University>(
                "sp_GetUniversitiesAdmin",
                new { UniversityID = (int?)null, IncludeInactive = includeInactive },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<University?> GetUniversityByIdAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<University>(
                "sp_GetUniversitiesAdmin",
                new { UniversityID = id, IncludeInactive = true },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<int> AddUniversityAsync(University university)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>(
                "sp_AddUniversity",
                new { university.UniversityNameThai, university.UniversityNameEng, university.UniversityType },
                commandType: CommandType.StoredProcedure);
        }

        public async Task<University?> UpdateUniversityAsync(int id, University university)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<University>(
                "sp_UpdateUniversity",
                new
                {
                    UniversityID = id,
                    university.UniversityNameThai,
                    university.UniversityNameEng,
                    university.UniversityType,
                    university.IsActive
                },
                commandType: CommandType.StoredProcedure);
        }

        public async Task DeleteUniversityAsync(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "sp_DeleteUniversity",
                new { UniversityID = id },
                commandType: CommandType.StoredProcedure);
        }
    }
}
