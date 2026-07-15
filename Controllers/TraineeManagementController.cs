using Dapper;
using JobOnlineAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace JobOnlineAPI.Controllers
{
    // Backs the /admin/trainee-management page exactly: same TraineeQuota/TraineeAssignments
    // tables as TraineeController, but shaped to the frontend's CompanyGroup[]/DepartmentGroup/
    // Trainee contract instead of the more generic /api/Trainee surface. See
    // SQL/setup_TraineeManagementV2.sql for the stored procedures this calls.
    [ApiController]
    [Route("api/[controller]")]
    public class TraineeManagementController(IConfiguration configuration) : ControllerBase
    {
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview(
            [FromQuery] int? year,
            [FromQuery] string? company,
            [FromQuery] string? department)
        {
            using var conn = new SqlConnection(_connectionString);
            using var multi = await conn.QueryMultipleAsync(
                "sp_GetTraineeManagementOverview",
                new { Year = year, CompanyCode = company, DepartmentCode = department },
                commandType: CommandType.StoredProcedure);

            var departments = (await multi.ReadAsync<TraineeManagementDepartment>()).ToList();
            var trainees = (await multi.ReadAsync<TraineeManagementTrainee>()).ToList();

            foreach (var dept in departments)
            {
                dept.Trainees = [.. trainees.Where(t =>
                    t.CompanyCode == dept.CompanyCode && t.DepartmentCode == dept.DepartmentCode)];
            }

            var companies = departments
                .GroupBy(d => d.CompanyCode)
                .Select(g => new TraineeManagementCompany
                {
                    CompanyCode = g.Key,
                    CompanyName = g.First().CompanyName,
                    Departments = g.ToList()
                });

            return Ok(companies);
        }

        [HttpPut("departments/{departmentCode}/quota")]
        public async Task<IActionResult> UpdateDepartmentQuota(string departmentCode, [FromBody] UpdateDepartmentQuotaRequest request)
        {
            using var conn = new SqlConnection(_connectionString);
            try
            {
                var quota = await conn.QueryFirstAsync<TraineeQuota>(
                    "sp_UpsertTraineeQuotaByDepartment",
                    new
                    {
                        DepartmentCode = departmentCode,
                        Quota = request.Required,
                        IsAcceptingTrainees = request.IsOpen
                    },
                    commandType: CommandType.StoredProcedure);

                return Ok(new
                {
                    departmentCode = quota.DepartmentCode,
                    departmentName = quota.DepartmentName,
                    required = quota.Quota,
                    isOpen = quota.IsAcceptingTrainees
                });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("trainees")]
        public async Task<IActionResult> CreateTrainee([FromBody] CreateTraineeManagementTraineeRequest request)
        {
            using var conn = new SqlConnection(_connectionString);
            try
            {
                var result = await conn.QueryFirstAsync<CreateTraineeManagementTraineeResult>(
                    "sp_CreateTraineeManagementAssignment",
                    new
                    {
                        request.CompanyCode,
                        request.DepartmentCode,
                        request.ApplicantID,
                        FirstNameThai = request.FirstName,
                        LastNameThai = request.LastName,
                        request.Nickname,
                        request.University,
                        request.StartDate,
                        request.EndDate
                    },
                    commandType: CommandType.StoredProcedure);

                return Ok(result);
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("trainees/{id:int}")]
        public async Task<IActionResult> DeleteTrainee(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.ExecuteAsync(
                "sp_DeleteTraineeManagementAssignment",
                new { AssignmentID = id },
                commandType: CommandType.StoredProcedure);

            return Ok(new { id, message = "ลบนักศึกษาฝึกงานออกจากแผนกเรียบร้อยแล้ว" });
        }
    }
}
