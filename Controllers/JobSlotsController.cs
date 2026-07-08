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

        [HttpGet("department")]
        public async Task<IActionResult> GetSlotsByDepartment([FromQuery] string? department, [FromQuery] string? company, [FromQuery] int? month, [FromQuery] int? year)
        {
            var effectiveYear = year ?? DateTime.Now.Year;

            using var conn = new SqlConnection(_connectionString);
            var slots = await conn.QueryAsync<JobSlot>(
                "sp_GetJobSlotsByDepartment",
                new { Department = department, Company = company, Month = month, Year = effectiveYear },
                commandType: CommandType.StoredProcedure);

            return Ok(slots);
        }

        [HttpPost]
        public async Task<IActionResult> AddSlot([FromBody] JobSlot slot)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var conn = new SqlConnection(_connectionString);
            try
            {
                var slotId = await conn.ExecuteScalarAsync<int>(
                    "sp_AddJobSlot",
                    new { slot.Department, slot.NumberOfPositions, slot.StartDate, slot.EndDate, slot.CreatedByAdminID, slot.RequestedByName },
                    commandType: CommandType.StoredProcedure);

                return Ok(new { SlotID = slotId });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSlot(int id, [FromBody] JobSlot slot)
        {
            using var conn = new SqlConnection(_connectionString);
            try
            {
                var updated = await conn.QueryFirstOrDefaultAsync<JobSlot>(
                    "sp_UpdateJobSlot",
                    new { SlotID = id, slot.NumberOfPositions, slot.StartDate, slot.EndDate, slot.Status, slot.RequestedByName, slot.ModifiedByAdminID },
                    commandType: CommandType.StoredProcedure);

                if (updated == null)
                    return NotFound();

                return Ok(updated);
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
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
            try
            {
                await conn.ExecuteAsync(
                    "sp_AssignApplicantToSlot",
                    new
                    {
                        SlotID = id,
                        request.ApplicantID,
                        ManualTitle = request.Title,
                        ManualFirstNameThai = request.FirstNameThai,
                        ManualLastNameThai = request.LastNameThai,
                        ManualNickname = request.Nickname,
                        ManualAge = request.Age,
                        ManualYear = request.Year,
                        ManualGPA = request.GPA,
                        ManualMajor = request.Major,
                        ManualFaculty = request.Faculty,
                        ManualUniversity = request.University,
                        ManualInternshipType = request.InternshipType,
                        ManualInternStartDate = request.InternStartDate,
                        ManualInternEndDate = request.InternEndDate,
                        ManualDurationMonths = request.DurationMonths,
                        ManualPreferredPosition = request.PreferredPosition,
                        ManualPreferredPositionBackup = request.PreferredPositionBackup,
                        ManualMobilePhone = request.MobilePhone,
                        ManualEmail = request.Email,
                        ManualCanCommute = request.CanCommute,
                        ManualCanTravelOutside = request.CanTravelOutside,
                        ManualFlexibleWork = request.FlexibleWork,
                        ManualTranscriptUrl = request.TranscriptUrl,
                        ManualResumeLink = request.ResumeLink,
                        ManualPortfolioLink = request.PortfolioLink,
                        ManualReasonForInterest = request.ReasonForInterest
                    },
                    commandType: CommandType.StoredProcedure);

                return Ok();
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("{id}/assignments")]
        public async Task<IActionResult> GetSlotAssignments(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            var assignments = await conn.QueryAsync<JobSlotAssignment>(
                "sp_GetSlotAssignments",
                new { SlotID = id },
                commandType: CommandType.StoredProcedure);

            return Ok(assignments);
        }

        [HttpDelete("assignments/{assignmentId}")]
        public async Task<IActionResult> UnassignApplicant(int assignmentId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.ExecuteAsync(
                "sp_UnassignApplicantFromSlot",
                new { AssignmentID = assignmentId },
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
