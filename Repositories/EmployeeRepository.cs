using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using JobOnlineAPI.Models;

namespace JobOnlineAPI.Repositories
{
    public class EmployeeRepository(IConfiguration configuration) : IEmployeeRepository
    {
        private readonly string? _connectionString = configuration.GetConnectionString("DefaultConnection");

        public async Task<EmployeeSearchResponse> SearchEmployeesAsync(string? companyCode, string? department, string? search, int page, int pageSize)
        {
            using IDbConnection db = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@CompanyCode", companyCode);
            parameters.Add("@Department", department);
            parameters.Add("@Search", search);
            parameters.Add("@Page", page);
            parameters.Add("@PageSize", pageSize);

            var rows = (await db.QueryAsync<EmployeeSearchRow>(
                "sp_SearchEmployees", parameters, commandType: CommandType.StoredProcedure)).ToList();

            return new EmployeeSearchResponse
            {
                Items = rows.Select(r => new EmployeeSearchResult
                {
                    EmpNo = r.EmpNo,
                    AdUser = r.AdUser,
                    NameThai = r.NameThai,
                    Nickname = r.NickName,
                    Email = r.Email,
                    Mobile = r.Mobile,
                    Telephone = r.Telephone,
                    DepartmentCode = r.DepartmentCode,
                    DepartmentName = r.DepartmentName,
                    Position = r.Position,
                    CompanyCode = r.CompanyCode,
                    CompanyName = r.CompanyName,
                    IsAdmin = r.IsAdmin
                }),
                Page = page,
                PageSize = pageSize,
                Total = rows.Count > 0 ? rows[0].TotalCount : 0
            };
        }
    }
}
