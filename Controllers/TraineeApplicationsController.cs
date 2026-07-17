using System;
using System.Data;
using System.Text.Json;
using Dapper;
using JobOnlineAPI.DAL;
using JobOnlineAPI.Models;
using JobOnlineAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Org.BouncyCastle.Ocsp;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TraineeApplicationsController(
        DapperContext context,
        INetworkShareService networkShareService,
        IOptions<FileStorageConfig> fileStorageConfig,
        IEmailNotificationService emailNotificationService,
        ILogger<TraineeApplicationsController> logger) : ControllerBase
    {
        private readonly DapperContext _context = context;
        private readonly INetworkShareService _networkShareService = networkShareService;
        private readonly IEmailNotificationService _emailNotificationService = emailNotificationService;
        private readonly ILogger<TraineeApplicationsController> _logger = logger;
        private readonly string _applicationFormUri = fileStorageConfig.Value.ApplicationFormUri
        ?? throw new InvalidOperationException("Application form URI is not configured.");
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
        private static readonly string[] AllowedExtensions = [".pdf", ".png", ".jpg", ".jpeg", ".doc", ".docx"];
        private static readonly string[] AllowedMimeTypes =
        [
            "application/pdf", "image/png", "image/jpeg",
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        ];
        private const long MaxFileSize = 40 * 1024 * 1024;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] string? status)
        {
            using var conn = _context.CreateConnection();
            var result = await conn.QueryAsync<dynamic>(
                "sp_GetTraineeApplications",
                new { Status = status },
                commandType: CommandType.StoredProcedure);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int? assignmentID, int? applicationId, int? applicantId)
        {
            using var conn = _context.CreateConnection();
            using var multi = await conn.QueryMultipleAsync(
                "sp_GetTraineeApplicationByID",
                new
                {
                    AssignmentID = Normalize(assignmentID),
                    ApplicationID = Normalize(applicationId),
                    ApplicantID = Normalize(applicantId)
                },
                commandType: CommandType.StoredProcedure);

            var application = await multi.ReadFirstOrDefaultAsync<dynamic>();
            if (application == null)
                return NotFound(new { message = "ไม่พบข้อมูลใบสมัครฝึกงานนี้" });

            var files = await multi.ReadAsync<dynamic>();

            return Ok(new { Application = application, Files = files });
        }

        // Query params default to 0 when omitted from the URL; treat that (and any non-positive
        // value) as "not provided" so the SP's IS NULL checks work instead of matching ID 0.
        private static int? Normalize(int? value) => value is null or <= 0 ? null : value;

        [HttpPost("upsert")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upsert(
            [FromForm] string jsonData,
            [FromForm] List<IFormFile>? idCardFiles,
            [FromForm] List<IFormFile>? houseRegFiles,
            [FromForm] List<IFormFile>? resumeFiles,
            [FromForm] List<IFormFile>? transcriptFiles)
        {
            TraineeApplication? request;
            try
            {
                request = JsonSerializer.Deserialize<TraineeApplication>(jsonData, JsonOptions);
            }
            catch
            {
                return BadRequest(new { message = "jsonData ไม่ถูกต้อง" });
            }

            if (request == null)
                return BadRequest(new { message = "jsonData ไม่ถูกต้อง" });

            await _networkShareService.ConnectAsync();
            try
            {
                using var conn = _context.CreateConnection();

                var upsertResult = await conn.QueryFirstAsync<dynamic>(
                    "usp_TraineeApplicant_Upsert",
                    new
                    {
                        request.AssignmentID,
                        request.StartDate,
                        request.EndDate,
                        request.DesiredField1,
                        request.DesiredField2,
                        request.DesiredField3,
                        request.InternshipType,
                        request.DurationMonths,
                        request.Reason,
                        request.ReasonOther,
                        request.PrefixT,
                        request.NameFirstT,
                        request.NameLastT,
                        request.NicknameT,
                        request.PrefixE,
                        request.NameFirstE,
                        request.NameLastE,
                        request.NicknameE,
                        request.Gender,
                        request.DateOfBirth,
                        request.Age,
                        request.PlaceOfBirth,
                        request.Nationality,
                        request.Race,
                        request.Religion,
                        request.Height,
                        request.Weight,
                        request.IDCardNo,
                        request.IDIssuedBy,
                        request.IDExpiredDate,
                        request.Address,
                        request.ProvinceID,
                        request.DistrictID,
                        request.SubDistrictID,
                        request.PostalCode,
                        request.Telephone,
                        request.Mobile,
                        request.Email,
                        request.FatherName,
                        request.FatherOccupation,
                        request.FatherStatus,
                        request.MotherName,
                        request.MotherOccupation,
                        request.MotherStatus,
                        request.SiblingCount,
                        request.SiblingOrder,
                        request.EmergencyName,
                        request.EmergencyRelation,
                        request.EmergencyAddress,
                        request.EmergencyPhone,
                        request.School,
                        request.Faculty,
                        request.Major,
                        request.Minor,
                        request.YearOfStudy,
                        request.GPA,
                        request.AdvisorName,
                        request.AdvisorPhone,
                        request.Activities,
                        request.InfoSources,
                        request.InfoSourceStaffName,
                        request.InfoSourceDepartment,
                        request.InfoSourceOther,
                        request.Status,
                        request.JobID,
                        request.UserID
                    },
                    commandType: CommandType.StoredProcedure);

                int applicantId = upsertResult.ApplicantID;
                int applicationId = upsertResult.ApplicationID;

                var fileGroups = new[]
                {
                    (Files: idCardFiles,   Section: "idCard"),
                    (Files: houseRegFiles, Section: "houseReg"),
                    (Files: resumeFiles,   Section: "resume"),
                    (Files: transcriptFiles, Section: "transcript"),
                };

                foreach (var (files, section) in fileGroups)
                {
                    if (files == null || files.Count == 0) continue;
                    await SaveFilesAsync(conn, files, applicantId, applicationId, section);
                }

                return Ok(new { ApplicantID = applicantId, ApplicationID = applicationId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Trainee application upsert failed. RequestId: {RequestId}, HasIdCard: {HasIdCard}, HasHouseReg: {HasHouseReg}, HasResume: {HasResume}, HasTranscript: {HasTranscript}",
                    HttpContext.TraceIdentifier,
                    idCardFiles?.Count > 0,
                    houseRegFiles?.Count > 0,
                    resumeFiles?.Count > 0,
                    transcriptFiles?.Count > 0);
                throw;
            }
            finally
            {
                _networkShareService.Disconnect();
            }
        }

        private async Task SaveFilesAsync(IDbConnection conn, List<IFormFile> files, int applicantId, int applicationId, string section)
        {
            var folder = Path.Combine(_networkShareService.GetBasePath(), $"applicant_{applicantId}");
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
                    "usp_ApplicantFile_Insert",
                    new
                    {
                        ApplicantID = applicantId,
                        ApplicationID = applicationId,
                        FilePath = filePath.Replace('\\', '/'),
                        FileName = fileName,
                        FileSize = file.Length,
                        FileType = file.ContentType,
                        SectionFile = section
                    },
                    commandType: CommandType.StoredProcedure);
            }
        }
    }
}
