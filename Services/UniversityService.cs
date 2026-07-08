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
    }
}
