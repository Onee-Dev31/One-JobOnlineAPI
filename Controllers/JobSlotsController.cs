using Dapper;
using JobOnlineAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobSlotsController(IConfiguration configuration) : ControllerBase
    {
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        [HttpGet("department/{department}")]
        public async Task<IActionResult> GetSlotsByDepartment(string department)
        {
            using var conn = new SqlConnection(_connectionString);
            var slots = await conn.QueryAsync<JobSlot>(
                "sp_GetJobSlotsByDepartment",
                new { Department = department },
                commandType: CommandType.StoredProcedure);

            return Ok(slots);
        }

        [HttpPost]
        public async Task<IActionResult> AddSlot([FromBody] JobSlot slot)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var conn = new SqlConnection(_connectionString);
            var slotId = await conn.ExecuteScalarAsync<int>(
                "sp_AddJobSlot",
                new { slot.Department, slot.SlotNumber, slot.StartDate, slot.EndDate },
                commandType: CommandType.StoredProcedure);

            return Ok(new { SlotID = slotId });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSlot(int id, [FromBody] JobSlot slot)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.ExecuteAsync(
                "sp_UpdateJobSlot",
                new { SlotID = id, slot.StartDate, slot.EndDate, slot.Status },
                commandType: CommandType.StoredProcedure);

            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSlot(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.ExecuteAsync(
                "sp_DeleteJobSlot",
                new { SlotID = id },
                commandType: CommandType.StoredProcedure);

            return Ok();
        }

        [HttpGet("job/{jobId}/candidates")]
        public async Task<IActionResult> GetCandidatesForAssignment(int jobId, [FromQuery] string? department)
        {
            using var conn = new SqlConnection(_connectionString);

            var candidates = await conn.QueryAsync(
                "sp_GetCandidateAllForJobsV2",
                new { Department = department, JobID = jobId },
                commandType: CommandType.StoredProcedure);

            var slots = string.IsNullOrEmpty(department)
                ? Enumerable.Empty<JobSlot>()
                : await conn.QueryAsync<JobSlot>(
                    "sp_GetJobSlotsByDepartment",
                    new { Department = department },
                    commandType: CommandType.StoredProcedure);

            return Ok(new { Candidates = candidates, Slots = slots });
        }

        [HttpPut("{id}/assign")]
        public async Task<IActionResult> AssignApplicant(int id, [FromBody] AssignApplicantRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var conn = new SqlConnection(_connectionString);
            await conn.ExecuteAsync(
                "sp_AssignApplicantToSlot",
                new { SlotID = id, request.ApplicantID },
                commandType: CommandType.StoredProcedure);

            return Ok();
        }

        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDepartmentDashboard([FromQuery] string? department)
        {
            using var conn = new SqlConnection(_connectionString);
            using var multi = await conn.QueryMultipleAsync(
                "sp_GetDepartmentDashboard",
                new { Department = department },
                commandType: CommandType.StoredProcedure);

            var jobs = (await multi.ReadAsync()).ToList();
            var slots = (await multi.ReadAsync<JobSlot>()).ToList();

            var result = jobs.Select(job =>
            {
                string jobDepartment = (string)job.Department;
                return new
                {
                    Job = job,
                    Slots = slots.Where(s => s.Department == jobDepartment)
                };
            });

            return Ok(result);
        }
    }
}
