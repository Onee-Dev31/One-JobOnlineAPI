using Dapper;
using JobOnlineAPI.Models;
using JobOnlineAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobSlotsController(
        IConfiguration configuration,
        INetworkShareService networkShareService,
        IEmailNotificationService emailNotificationService,
        IJwtTokenService jwtTokenService,
        ILogger<ApplicantNewController> logger
        ) : ControllerBase
    {
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
        private readonly INetworkShareService _networkShareService = networkShareService;
        private readonly IEmailNotificationService _emailNotificationService = emailNotificationService;
        private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
        private readonly ILogger _logger = logger;
        private static readonly string[] AllowedExtensions = [".pdf", ".png", ".jpg", ".jpeg", ".doc", ".docx"];
        private static readonly string[] AllowedMimeTypes =
        [
            "application/pdf", "image/png", "image/jpeg",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        ];
        private const long MaxFileSize = 40 * 1024 * 1024;

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

            return Ok(new { SlotID = id, message = "ลบ Slot เรียบร้อยแล้ว" });
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

        [HttpGet("unassigned-trainees")]
        public async Task<IActionResult> GetUnassignedTraineeAssignments([FromQuery] string? department)
        {
            using var conn = new SqlConnection(_connectionString);
            var assignments = await conn.QueryAsync(
                "sp_GetUnassignedTraineeAssignments",
                new { Department = department },
                commandType: CommandType.StoredProcedure);

            return Ok(assignments);
        }

        [HttpPut("{id}/assign")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> AssignApplicant(
            int id,
            [FromForm] string jsonData,
            [FromForm] List<IFormFile>? resumeFiles,
            [FromForm] List<IFormFile>? transcriptFiles)
        {
            return await AssignApplicantCoreAsync(id, jsonData, resumeFiles, transcriptFiles);
        }

        // Public self-apply: a trainee applying on their own, with no batch chosen yet.
        // Same jsonData shape as AssignApplicant, minus a SlotID — creates JobApplications
        // (Status = 'pending') and a JobSlotAssignments row with SlotID still NULL.
        [HttpPost("apply")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ApplyAsTrainee(
            [FromForm] string jsonData,
            [FromForm] List<IFormFile>? resumeFiles,
            [FromForm] List<IFormFile>? transcriptFiles)
        {
            return await AssignApplicantCoreAsync(null, jsonData, resumeFiles, transcriptFiles);
        }

        private async Task<IActionResult> AssignApplicantCoreAsync(
            int? slotId,
            string jsonData,
            List<IFormFile>? resumeFiles,
            List<IFormFile>? transcriptFiles)
        {
            AssignApplicantRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<AssignApplicantRequest>(jsonData, JsonOptions);
            }
            catch
            {
                return BadRequest(new { message = "jsonData ไม่ถูกต้อง" });
            }

            if (request == null)
                return BadRequest(new { message = "jsonData ไม่ถูกต้อง" });

            var userId = await GetCandidateUserIdAsync();

            await _networkShareService.ConnectAsync();
            try
            {
                using var conn = new SqlConnection(_connectionString);
                try
                {
                    var assignmentId = await conn.ExecuteScalarAsync<int>(
                        "sp_AssignApplicantToSlot",
                        new
                        {
                            SlotID = slotId,
                            request.JobID,
                            request.ApplicationID,
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
                            ManualReasonForInterest = request.ReasonForInterest,
                            UserID = userId
                        },
                        commandType: CommandType.StoredProcedure);

                    var fileGroups = new[]
                    {
                        (Files: resumeFiles, Section: "resume"),
                        (Files: transcriptFiles, Section: "transcript"),
                    };

                    foreach (var (files, section) in fileGroups)
                    {
                        if (files == null || files.Count == 0) continue;
                        await SaveFilesAsync(conn, files, assignmentId, section);
                    }

                        try
                        {
                            // ฟอร์ม Email ใหม่
                            await _emailNotificationService.SendEmailsTraineeRegisterAsync(assignmentId, request.JobID);
                            await _emailNotificationService.SendEmailsNotiHrAfterApplyAsync(assignmentId, request.JobID, true);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Send email failed (ApplicantID: {ApplicantID})", assignmentId);
                        }

                    return Ok(new { AssignmentID = assignmentId });
                }
                catch (SqlException ex)
                {
                    return BadRequest(new { message = ex.Message });
                }
            }
            finally
            {
                _networkShareService.Disconnect();
            }
        }

        // Candidate's Users.UserId, read from the auth_token JWT/cookie if the caller is a logged-in
        // candidate (self-apply). Returns null for staff manual/walk-in entries, which have no such cookie.
        private async Task<int?> GetCandidateUserIdAsync()
        {
            var token = Request.Cookies["auth_token"];
            if (string.IsNullOrWhiteSpace(token))
                return null;

            try
            {
                var validated = await _jwtTokenService.ValidateTokenAsync(token);
                var claim = validated.Claims.FirstOrDefault(c => c.Type == "user_id");
                return claim != null && int.TryParse(claim.Value, out var userId) && userId > 0 ? userId : null;
            }
            catch
            {
                return null;
            }
        }

        private async Task SaveFilesAsync(IDbConnection conn, List<IFormFile> files, int assignmentId, string section)
        {
            var folder = Path.Combine(_networkShareService.GetBasePath(), $"jobslot_assignment_{assignmentId}");
            Directory.CreateDirectory(folder);

            foreach (var file in files)
            {
                if (file.Length == 0) continue;

                if (file.Length > MaxFileSize)
                    throw new InvalidOperationException($"ไฟล์ {file.FileName} ใหญ่เกิน 40MB");

                var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(ext))
                    throw new InvalidOperationException($"ไฟล์ {file.FileName} ไม่รองรับนามสกุล {ext}");

                if (!AllowedMimeTypes.Contains(file.ContentType))
                    throw new InvalidOperationException($"ไฟล์ {file.FileName} มี MIME type ไม่ถูกต้อง");

                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await file.CopyToAsync(stream);

                await conn.ExecuteAsync(
                    "sp_AddJobSlotAssignmentFile",
                    new
                    {
                        AssignmentID = assignmentId,
                        FilePath = filePath.Replace('\\', '/'),
                        FileName = fileName,
                        FileSize = file.Length,
                        FileType = file.ContentType,
                        SectionFile = section
                    },
                    commandType: CommandType.StoredProcedure);
            }
        }

        [HttpGet("{id}/assignments")]
        public async Task<IActionResult> GetSlotAssignments(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            using var multi = await conn.QueryMultipleAsync(
                "sp_GetSlotAssignments",
                new { SlotID = id },
                commandType: CommandType.StoredProcedure);

            var assignments = (await multi.ReadAsync<JobSlotAssignment>()).ToList();
            var files = (await multi.ReadAsync<JobSlotAssignmentFile>()).ToList();

            foreach (var assignment in assignments)
            {
                assignment.Files = [.. files.Where(f => f.AssignmentID == assignment.AssignmentID)];
            }

            return Ok(assignments);
        }

        // Read-only: lets Part2 (/apply/trainee/part2) prefill its form from the Part1
        // JobSlotAssignments row the applicant already filled out.
        [HttpGet("assignments/{assignmentId:int}/seed")]
        public async Task<IActionResult> GetAssignmentSeedData(int assignmentId)
        {
            using var conn = new SqlConnection(_connectionString);
            var seed = await conn.QueryFirstOrDefaultAsync<JobSlotAssignmentSeedData>(
                "sp_GetJobSlotAssignmentSeedData",
                new { AssignmentID = assignmentId },
                commandType: CommandType.StoredProcedure);

            if (seed == null)
                return NotFound();

            return Ok(seed);
        }

        [HttpDelete("assignments/{assignmentId}")]
        public async Task<IActionResult> UnassignApplicant(int assignmentId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.ExecuteAsync(
                "sp_UnassignApplicantFromSlot",
                new { AssignmentID = assignmentId },
                commandType: CommandType.StoredProcedure);

            return Ok(new { AssignmentID = assignmentId, message = "ยกเลิกการมอบหมายเรียบร้อยแล้ว" });
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
