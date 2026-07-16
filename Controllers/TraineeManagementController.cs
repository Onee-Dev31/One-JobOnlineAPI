using Dapper;
using JobOnlineAPI.Models;
using JobOnlineAPI.Filters;
using JobOnlineAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace JobOnlineAPI.Controllers
{
    // Backs the /admin/trainee-management page exactly: same TraineeQuota/TraineeAssignments
    // tables as TraineeController, but shaped to the frontend's CompanyGroup[]/DepartmentGroup/
    // Trainee contract instead of the more generic /api/Trainee surface. See
    // SQL/setup_TraineeManagementV2.sql for the stored procedures this calls.
    [ApiController]
    [Route("api/[controller]")]
    public class TraineeManagementController(
        IConfiguration configuration,
        IManualTraineeService manualTraineeService,
        ILogger<TraineeManagementController> logger) : ControllerBase
    {
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");
        // ManualTraineeRequest inherits `required` members (Mobile, Email, etc.) from
        // TraineeApplication. System.Text.Json enforces those at deserialize time with an opaque
        // exception, which pre-empts ValidateManualRequest's friendlier per-field Thai messages
        // below. Disabling the check here (only for this controller's options instance) lets
        // ValidateManualRequest be the single source of truth for required-field errors.
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { static typeInfo =>
                {
                    if (typeInfo.Type != typeof(ManualTraineeRequest)) return;
                    foreach (var property in typeInfo.Properties) property.IsRequired = false;
                } }
            }
        };

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

        // DEPRECATED: required/isOpen in GET /overview are now derived from live Job postings
        // (JobGroup "นักศึกษาฝึกงาน" x NumberOfPositions — see vw_TraineeDepartmentRequired /
        // sp_GetTraineeManagementOverview), not from the TraineeQuota row this endpoint writes.
        // Calling this no longer affects what /overview returns. Left in place (not removed) in
        // case something else still depends on it; candidate for removal once confirmed unused.
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

        [HttpPost("trainees/manual")]
        [Consumes("multipart/form-data")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> CreateManualTrainee(
            [FromForm] string jsonData,
            [FromForm] List<IFormFile>? idCardFiles,
            [FromForm] List<IFormFile>? houseRegFiles,
            [FromForm] List<IFormFile>? resumeFiles,
            [FromForm] List<IFormFile>? transcriptFiles,
            CancellationToken cancellationToken)
        {
            if (!User.IsInRole("Admin"))
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "ไม่มีสิทธิ์เพิ่มนักศึกษาฝึกงาน" });

            ManualTraineeRequest? request;
            try
            {
                request = JsonSerializer.Deserialize<ManualTraineeRequest>(jsonData, JsonOptions);
            }
            catch (JsonException ex)
            {
                logger.LogWarning(ex, "jsonData failed to parse. Length={Length}, Raw={Raw}",
                    jsonData?.Length ?? -1, jsonData ?? "(null)");
                return ValidationProblemResult(new() { ["jsonData"] = ["jsonData ไม่ใช่ JSON ที่ถูกต้อง"] });
            }

            var errors = ValidateManualRequest(request);
            if (errors.Count > 0) return ValidationProblemResult(errors);

            var files = new Dictionary<string, IReadOnlyList<IFormFile>>
            {
                ["idCard"] = idCardFiles ?? [],
                ["houseReg"] = houseRegFiles ?? [],
                ["resume"] = resumeFiles ?? [],
                ["transcript"] = transcriptFiles ?? []
            };

            try
            {
                int? adminId = int.TryParse(User.FindFirst("admin_id")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var value)
                    ? value : null;
                return Ok(await manualTraineeService.CreateAsync(request!, files, adminId, cancellationToken));
            }
            catch (ArgumentException ex)
            {
                return ValidationProblemResult(new() { ["files"] = [ex.Message] });
            }
            catch (ManualTraineeConflictException ex)
            {
                return Conflict(new { message = ex.Message });
            }
            catch (SqlException ex) when (ex.Number is >= 50000 and < 51000)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error creating manual trainee. RequestId: {RequestId}", HttpContext.TraceIdentifier);
                return StatusCode(500, new { message = "เกิดข้อผิดพลาดภายในระบบ", requestId = HttpContext.TraceIdentifier });
            }
        }

        private ObjectResult ValidationProblemResult(Dictionary<string, string[]> errors) => new(new
        {
            message = "ข้อมูลนักศึกษาฝึกงานไม่ถูกต้อง",
            errors
        }) { StatusCode = StatusCodes.Status422UnprocessableEntity };

        private static Dictionary<string, string[]> ValidateManualRequest(ManualTraineeRequest? request)
        {
            var errors = new Dictionary<string, string[]>();
            if (request == null) { errors["jsonData"] = ["กรุณาระบุข้อมูลนักศึกษาฝึกงาน"]; return errors; }
            void Required(string key, string? value, string message) { if (string.IsNullOrWhiteSpace(value)) errors[key] = [message]; }
            Required(nameof(request.CompanyCode), request.CompanyCode, "กรุณาระบุบริษัท");
            Required(nameof(request.DepartmentCode), request.DepartmentCode, "กรุณาระบุแผนก");
            Required(nameof(request.NameFirstT), request.NameFirstT, "กรุณาระบุชื่อ");
            Required(nameof(request.NameLastT), request.NameLastT, "กรุณาระบุนามสกุล");
            Required(nameof(request.Mobile), request.Mobile, "กรุณาระบุเบอร์มือถือ");
            Required(nameof(request.Email), request.Email, "กรุณาระบุอีเมล");
            Required(nameof(request.School), request.School, "กรุณาระบุสถานศึกษา");
            if (request.StartDate == null) errors[nameof(request.StartDate)] = ["กรุณาระบุวันที่เริ่มฝึกงาน"];
            if (request.EndDate == null) errors[nameof(request.EndDate)] = ["กรุณาระบุวันที่สิ้นสุดฝึกงาน"];
            if (request.StartDate != null && request.EndDate != null && request.EndDate < request.StartDate)
                errors[nameof(request.EndDate)] = ["วันที่สิ้นสุดต้องไม่น้อยกว่าวันที่เริ่ม"];
            if (!string.IsNullOrWhiteSpace(request.Email) && !System.Net.Mail.MailAddress.TryCreate(request.Email, out _))
                errors[nameof(request.Email)] = ["รูปแบบอีเมลไม่ถูกต้อง"];
            return errors;
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
