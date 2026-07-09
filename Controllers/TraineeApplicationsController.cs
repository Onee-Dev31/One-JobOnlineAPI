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
        ILogger<ApplicantNewController> logger) : ControllerBase
    {
        private readonly DapperContext _context = context;
        private readonly INetworkShareService _networkShareService = networkShareService;
        private readonly string _basePath = fileStorageConfig.Value.BasePath;
        private readonly IEmailNotificationService _emailNotificationService = emailNotificationService;
        private readonly ILogger _logger = logger;
        private readonly IOptions<FileStorageConfig> _fileStorageConfig = fileStorageConfig;
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
            var result = await conn.QueryAsync<TraineeApplication>(
                "sp_GetTraineeApplications",
                new { Status = status },
                commandType: CommandType.StoredProcedure);

            return Ok(result);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            using var conn = _context.CreateConnection();
            using var multi = await conn.QueryMultipleAsync(
                "sp_GetTraineeApplicationByID",
                new { TraineeApplicationID = id },
                commandType: CommandType.StoredProcedure);

            var application = await multi.ReadFirstOrDefaultAsync<TraineeApplication>();
            if (application == null)
                return NotFound(new { message = "ไม่พบข้อมูลใบสมัครฝึกงานนี้" });

            var files = (await multi.ReadAsync<TraineeFile>()).ToList();

            return Ok(new TraineeApplicationDetail
            {
                TraineeApplicationID = application.TraineeApplicationID,
                StartDate = application.StartDate,
                EndDate = application.EndDate,
                DesiredField1 = application.DesiredField1,
                DesiredField2 = application.DesiredField2,
                DesiredField3 = application.DesiredField3,
                Reason = application.Reason,
                ReasonOther = application.ReasonOther,
                PrefixT = application.PrefixT,
                NameFirstT = application.NameFirstT,
                NameLastT = application.NameLastT,
                NicknameT = application.NicknameT,
                PrefixE = application.PrefixE,
                NameFirstE = application.NameFirstE,
                NameLastE = application.NameLastE,
                NicknameE = application.NicknameE,
                Gender = application.Gender,
                DateOfBirth = application.DateOfBirth,
                Age = application.Age,
                PlaceOfBirth = application.PlaceOfBirth,
                Nationality = application.Nationality,
                Race = application.Race,
                Religion = application.Religion,
                Height = application.Height,
                Weight = application.Weight,
                IDCardNo = application.IDCardNo,
                IDIssuedBy = application.IDIssuedBy,
                IDExpiredDate = application.IDExpiredDate,
                Address = application.Address,
                ProvinceID = application.ProvinceID,
                DistrictID = application.DistrictID,
                SubDistrictID = application.SubDistrictID,
                PostalCode = application.PostalCode,
                Telephone = application.Telephone,
                Mobile = application.Mobile,
                Email = application.Email,
                FatherName = application.FatherName,
                FatherOccupation = application.FatherOccupation,
                FatherStatus = application.FatherStatus,
                MotherName = application.MotherName,
                MotherOccupation = application.MotherOccupation,
                MotherStatus = application.MotherStatus,
                SiblingCount = application.SiblingCount,
                SiblingOrder = application.SiblingOrder,
                EmergencyName = application.EmergencyName,
                EmergencyRelation = application.EmergencyRelation,
                EmergencyAddress = application.EmergencyAddress,
                EmergencyPhone = application.EmergencyPhone,
                School = application.School,
                Faculty = application.Faculty,
                Major = application.Major,
                Minor = application.Minor,
                YearOfStudy = application.YearOfStudy,
                AdvisorName = application.AdvisorName,
                AdvisorPhone = application.AdvisorPhone,
                Activities = application.Activities,
                InfoSources = application.InfoSources,
                InfoSourceStaffName = application.InfoSourceStaffName,
                InfoSourceDepartment = application.InfoSourceDepartment,
                InfoSourceOther = application.InfoSourceOther,
                Status = application.Status,
                CreatedAt = application.CreatedAt,
                UpdatedAt = application.UpdatedAt,
                Files = files
            });
        }

        [HttpPost("upsert")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Upsert(
            [FromForm] string jsonData,
            [FromForm] List<IFormFile>? idCardFiles,
            [FromForm] List<IFormFile>? houseRegFiles,
            [FromForm] List<IFormFile>? resumeFiles)
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

                var id = await conn.ExecuteScalarAsync<int>(
                    "usp_TraineeApplications_Upsert",
                    new
                    {
                        request.TraineeApplicationID,
                        request.StartDate,
                        request.EndDate,
                        request.DesiredField1,
                        request.DesiredField2,
                        request.DesiredField3,
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
                        request.AdvisorName,
                        request.AdvisorPhone,
                        request.Activities,
                        request.InfoSources,
                        request.InfoSourceStaffName,
                        request.InfoSourceDepartment,
                        request.InfoSourceOther,
                        request.Status,
                        request.JobID
                    },
                    commandType: CommandType.StoredProcedure);

                var fileGroups = new[]
                {
                    (Files: idCardFiles,   Section: "idCard"),
                    (Files: houseRegFiles, Section: "houseReg"),
                    (Files: resumeFiles,   Section: "resume"),
                };

                foreach (var (files, section) in fileGroups)
                {
                    if (files == null || files.Count == 0) continue;
                    await SaveFilesAsync(conn, files, id, section);
                }
                if (request.TraineeApplicationID == 0) {
                    try
                    {
                        // ฟอร์ม Email ใหม่
                        await _emailNotificationService.SendEmailsTraineeRegisterAsync(id, request.JobID);
                        await _emailNotificationService.SendEmailsNotiHrAfterApplyAsync(id, request.JobID, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Send email failed (ApplicantID: {ApplicantID})", request.TraineeApplicationID);
                    }
                }
                

                return Ok(new { TraineeApplicationID = id });
            }
            finally
            {
                _networkShareService.Disconnect();
            }
        }

        private async Task SaveFilesAsync(IDbConnection conn, List<IFormFile> files, int traineeId, string section)
        {
            var folder = Path.Combine(_basePath, $"trainee_{traineeId}");
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
                    @"INSERT INTO TraineeFiles (TraineeApplicationID, FilePath, FileName, FileSize, FileType, SectionFile)
                      VALUES (@TraineeApplicationID, @FilePath, @FileName, @FileSize, @FileType, @SectionFile)",
                    new
                    {
                        TraineeApplicationID = traineeId,
                        FilePath = filePath.Replace('\\', '/'),
                        FileName = fileName,
                        FileSize = file.Length,
                        FileType = file.ContentType,
                        SectionFile = section
                    });
            }
        }
    }
}
