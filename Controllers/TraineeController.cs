using Dapper;
using JobOnlineAPI.Models;
using JobOnlineAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

namespace JobOnlineAPI.Controllers
{
    // Department-fixed-quota trainee intake system (v2), running alongside the existing
    // JobSlots/JobSlotAssignments (time-boxed batch) system in JobSlotsController — neither that
    // controller nor the tables/procs it uses are touched here. See
    // SQL/setup_TraineeManagementV2.sql for the schema this controller depends on.
    [ApiController]
    [Route("api/[controller]")]
    public class TraineeController(
        IConfiguration configuration,
        INetworkShareService networkShareService,
        IEmailNotificationService emailNotificationService,
        IJwtTokenService jwtTokenService,
        IEmailService emailService,
        ILogger<ApplicantNewController> logger
        ) : ControllerBase
    {
        
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
        private readonly INetworkShareService _networkShareService = networkShareService;
        private readonly IEmailNotificationService _emailNotificationService = emailNotificationService;
        private readonly IJwtTokenService _jwtTokenService = jwtTokenService;
        private readonly IEmailService _emailService = emailService;
        private readonly ILogger _logger = logger;
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
        private static readonly string[] AllowedExtensions = [".pdf", ".png", ".jpg", ".jpeg", ".doc", ".docx"];
        private static readonly string[] AllowedMimeTypes =
        [
            "application/pdf", "image/png", "image/jpeg",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        ];
        private const long MaxFileSize = 40 * 1024 * 1024;

        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] int? year)
        {
            using var conn = new SqlConnection(_connectionString);
            using var multi = await conn.QueryMultipleAsync(
                "sp_GetTraineeOverview",
                new { Year = year },
                commandType: CommandType.StoredProcedure);

            var departments = (await multi.ReadAsync<TraineeOverviewDepartment>()).ToList();
            var assignments = (await multi.ReadAsync<TraineeAssignment>()).ToList();

            foreach (var dept in departments)
            {
                dept.Assignments = [.. assignments.Where(a =>
                    a.CompanyCode == dept.CompanyCode && a.DepartmentCode == dept.DepartmentCode)];
            }

            var companies = departments
                .GroupBy(d => new { d.CompanyCode, d.CompanyName })
                .Select(g => new
                {
                    CompanyCode = g.Key.CompanyCode,
                    CompanyName = g.Key.CompanyName,
                    Departments = g.ToList()
                });

            return Ok(companies);
        }

        // DEPRECATED: Quota/IsAcceptingTrainees on GET /overview no longer come from TraineeQuota —
        // they're derived from live Job postings (see sp_GetTraineeOverview). These three quota
        // endpoints still read/write TraineeQuota directly but no longer affect what either
        // overview endpoint returns. Left in place, not removed.
        [HttpGet("quota")]
        public async Task<IActionResult> GetQuota([FromQuery] string? companyCode)
        {
            using var conn = new SqlConnection(_connectionString);
            var quota = await conn.QueryAsync<TraineeQuota>(
                "sp_GetTraineeQuota",
                new { CompanyCode = companyCode },
                commandType: CommandType.StoredProcedure);

            return Ok(quota);
        }

        [HttpPut("quota/{companyCode}/{departmentCode}")]
        public async Task<IActionResult> UpsertQuota(string companyCode, string departmentCode, [FromBody] UpsertTraineeQuotaRequest request)
        {
            using var conn = new SqlConnection(_connectionString);
            try
            {
                var quota = await conn.QueryFirstAsync<TraineeQuota>(
                    "sp_UpsertTraineeQuota",
                    new
                    {
                        CompanyCode = companyCode,
                        DepartmentCode = departmentCode,
                        request.DepartmentName,
                        request.Quota,
                        request.IsAcceptingTrainees,
                        request.Notes,
                        request.AdminID
                    },
                    commandType: CommandType.StoredProcedure);

                return Ok(quota);
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("quota/{companyCode}/{departmentCode}")]
        public async Task<IActionResult> DeleteQuota(string companyCode, string departmentCode)
        {
            using var conn = new SqlConnection(_connectionString);
            try
            {
                await conn.ExecuteAsync(
                    "sp_DeleteTraineeQuota",
                    new { CompanyCode = companyCode, DepartmentCode = departmentCode },
                    commandType: CommandType.StoredProcedure);

                return Ok(new { message = "ลบโควตาเรียบร้อยแล้ว" });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // Admin-driven: create (or place an existing pending) assignment directly into a department.
        [HttpPost("assignments")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreateAssignment(
            [FromForm] string jsonData,
            [FromForm] List<IFormFile>? resumeFiles,
            [FromForm] List<IFormFile>? transcriptFiles)
        {
            return await CreateOrPlaceAssignmentCoreAsync(jsonData, resumeFiles, transcriptFiles, allowDepartment: true);
        }

        // Public self-apply: a trainee applying on their own, with no department chosen yet.
        // Same jsonData shape as CreateAssignment, CompanyCode/DepartmentCode are ignored even if
        // sent — creates JobApplications (Status = 'pending') and a TraineeAssignments row with
        // CompanyCode/DepartmentCode still NULL, same as the old SlotID-NULL self-apply flow.
        [HttpPost("apply")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ApplyAsTrainee(
            [FromForm] string jsonData,
            [FromForm] List<IFormFile>? resumeFiles,
            [FromForm] List<IFormFile>? transcriptFiles)
        {
            return await CreateOrPlaceAssignmentCoreAsync(jsonData, resumeFiles, transcriptFiles, allowDepartment: false);
        }

        private async Task<IActionResult> CreateOrPlaceAssignmentCoreAsync(
            string jsonData,
            List<IFormFile>? resumeFiles,
            List<IFormFile>? transcriptFiles,
            bool allowDepartment)
        {
            CreateTraineeAssignmentRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<CreateTraineeAssignmentRequest>(jsonData, JsonOptions);
            }
            catch
            {
                return BadRequest(new { message = "jsonData ไม่ถูกต้อง" });
            }

            if (request == null)
                return BadRequest(new { message = "jsonData ไม่ถูกต้อง" });

            var companyCode = allowDepartment ? request.CompanyCode : null;
            var departmentCode = allowDepartment ? request.DepartmentCode : null;

            var tokenUserId = await GetCandidateUserIdAsync();
            var userId = tokenUserId ?? request.UserID;

            if (!userId.HasValue || userId.Value <= 0)
            {
                return Unauthorized(new
                {
                    message = "ไม่พบ UserID จาก Token หรือข้อมูลที่ส่งมา"
                });
            }

            await _networkShareService.ConnectAsync();
            try
            {
                using var conn = new SqlConnection(_connectionString);
                try
                {
                    var result = await conn.QueryFirstAsync<CreateTraineeAssignmentResult>(
                        "sp_CreateOrPlaceTraineeAssignment",
                        new
                        {
                            CompanyCode = companyCode,
                            DepartmentCode = departmentCode,
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
                            UserID = userId,
                            request.ForceOverQuota
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
                        await SaveFilesAsync(conn, files, result.AssignmentID, section);
                    }
                    try
                    {
                        // ฟอร์ม Email ใหม่
                        await _emailNotificationService.SendEmailsTraineeRegisterAsync(result.AssignmentID, request.JobID);
                        await _emailNotificationService.SendEmailsNotiHrAfterApplyAsync(result.AssignmentID, request.JobID, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Send email failed (ApplicantID: {ApplicantID})", result.AssignmentID);
                    }
                    // Note: unlike JobSlotsController's AssignApplicantCoreAsync, this does not send
                    // any notification emails yet — IEmailNotificationService's trainee methods key
                    // off JobSlotAssignments.AssignmentID, which is a different table/ID space from
                    // TraineeAssignments. Wiring emails here needs that service updated first.
                    return Ok(result);
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
            // "trainee_assignment_" prefix (not "jobslot_assignment_") — TraineeAssignments has its
            // own IDENTITY sequence starting from 1, so its AssignmentIDs numerically collide with
            // JobSlotAssignments' even though they're unrelated rows; the folder names must not.
            var folder = Path.Combine(_networkShareService.GetBasePath(), $"trainee_assignment_{assignmentId}");
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
                    "sp_AddTraineeAssignmentFile",
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

        [HttpGet("assignments/unassigned")]
        public async Task<IActionResult> GetUnassigned([FromQuery] string? department)
        {
            using var conn = new SqlConnection(_connectionString);
            var assignments = await conn.QueryAsync(
                "sp_GetUnassignedTraineeAssignments",
                new { Department = department },
                commandType: CommandType.StoredProcedure);

            return Ok(assignments);
        }

        // Read-only: lets a Part2-style continuation form prefill from the assignment the applicant
        // already filled out. Not wired to usp_TraineeApplicant_Upsert yet — see the note in
        // SQL/setup_TraineeManagementV2.sql above sp_GetTraineeAssignmentSeedData.
        [HttpGet("assignments/{assignmentId:int}/seed")]
        public async Task<IActionResult> GetSeedData(int assignmentId)
        {
            using var conn = new SqlConnection(_connectionString);
            using var multi = await conn.QueryMultipleAsync(
                "sp_GetTraineeAssignmentSeedData",
                new { AssignmentID = assignmentId },
                commandType: CommandType.StoredProcedure);

            var seed = await multi.ReadFirstOrDefaultAsync<TraineeAssignmentSeedData>();
            if (seed == null)
                return NotFound();

            seed.Files = (await multi.ReadAsync<TraineeAssignmentFile>()).ToList();

            return Ok(seed);
        }

        [HttpDelete("assignments/{assignmentId}")]
        public async Task<IActionResult> Unassign(int assignmentId)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.ExecuteAsync(
                "sp_UnassignTraineeAssignment",
                new { AssignmentID = assignmentId },
                commandType: CommandType.StoredProcedure);

            return Ok(new { AssignmentID = assignmentId, message = "ยกเลิกการมอบหมายเรียบร้อยแล้ว" });
        }
    }
}
