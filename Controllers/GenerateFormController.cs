using JobOnlineAPI.Views.Register;

using Microsoft.AspNetCore.Mvc;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using System.Data;
using System.Data.SqlClient;
using Dapper;
using System.Text.Json;
using JobOnlineAPI.Filters;


namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GenerateFormController(IWebHostEnvironment env, IConfiguration config) : ControllerBase
    {
        private readonly IWebHostEnvironment _env = env ?? throw new ArgumentNullException(nameof(env));
        private readonly IConfiguration _config = config;

        // [HttpPost("GenerateRegisterFormPDF")]
        // [TypeFilter(typeof(JwtAuthorizeAttribute))]
        // public IActionResult GenerateRegisterFormPDF([FromBody] JsonElement request)
        // {
        //     int applicantId = request.GetProperty("ApplicantID").GetInt32();
        //     int jobId = request.GetProperty("JobID").GetInt32();

        //     using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
        //     var form = connection.QueryFirstOrDefault<dynamic>(
        //         "sp_GetDataGenRegisFormPDF",
        //         new { ApplicantID = applicantId, JobID = jobId },
        //         commandType: CommandType.StoredProcedure);

        //     if (form == null)
        //         return NotFound("ไม่พบข้อมูลผู้สมัคร");

        //     var dict = (IDictionary<string, object>)form;

        //     QuestPDF.Settings.License = LicenseType.Community;
        //     var pdf = new PersonalDetailsForm(form).GeneratePdf();

        //     return File(pdf, "application/pdf", $"form_{applicantId}.pdf");
        // }
        [HttpPost("GenerateRegisterFormPDF")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public IActionResult GenerateRegisterFormPDF([FromBody] JsonElement request)
        {
            try
            {
                if (!request.TryGetProperty("ApplicantID", out var applicantIdEl) ||
                    !request.TryGetProperty("JobID", out var jobIdEl))
                {
                    return BadRequest("ApplicantID หรือ JobID ไม่ถูกต้อง");
                }

                int applicantId = applicantIdEl.GetInt32();
                int jobId = jobIdEl.GetInt32();

                using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                var form = connection.QueryFirstOrDefault<dynamic>(
                    "sp_GetDataGenRegisFormPDF",
                    new { ApplicantID = applicantId, JobID = jobId },
                    commandType: CommandType.StoredProcedure);

                if (form == null)
                    return NotFound("Data not found");

                var dict = form as IDictionary<string, object>;
                if (dict == null)
                    return StatusCode(500, "Internal Server error");

                QuestPDF.Settings.License = LicenseType.Community;
                var pdf = new PersonalDetailsForm(dict).GeneratePdf();

                return File(pdf, "application/pdf", $"form_{applicantId}.pdf");
            }
            catch (Exception)
            {
                // return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
                return StatusCode(500, "Internal Server error");
            }
        }

        [HttpPost("GenerateRegisterFormPDFV2")]
        // [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public IActionResult GenerateRegisterFormPDFV2([FromBody] JsonElement request)
        {
            try
            {
                if (!request.TryGetProperty("ApplicantID", out var applicantIdEl) ||
                    !request.TryGetProperty("JobID", out var jobIdEl))
                {
                    return BadRequest("Bad Request");
                }

                int applicantId = applicantIdEl.GetInt32();
                int jobId = jobIdEl.GetInt32();

                using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                var form = connection.QueryFirstOrDefault<dynamic>(
                    "sp_GetDataGenRegisFormPDF",
                    new { ApplicantID = applicantId, JobID = jobId },
                    commandType: CommandType.StoredProcedure);

                if (form == null)
                    return NotFound("Data not found");

                var dict = form as IDictionary<string, object>;
                if (dict == null)
                    return StatusCode(500, "Internal Server error");

                QuestPDF.Settings.License = LicenseType.Community;
                var pdf = new PersonalDetailsV2Form(dict).GeneratePdf();

                return File(pdf, "application/pdf", $"form_{applicantId}.pdf");
            }
            catch (Exception)
            {
                // return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
                return StatusCode(500, "Internal Server error");
            }
        }

        [HttpPost("GenerateRegisterFormPDFV3")]
        // [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public IActionResult GenerateRegisterFormPDFV3([FromBody] JsonElement request)
        {
            try
            {
                if (!request.TryGetProperty("ApplicantID", out var applicantIdEl) ||
                    !request.TryGetProperty("JobID", out var jobIdEl))
                {
                    return BadRequest("Bad Request");
                }

                int applicantId = applicantIdEl.GetInt32();
                int jobId = jobIdEl.GetInt32();

                using var connection = new SqlConnection(_config.GetConnectionString("DefaultConnection"));
                var form = connection.QueryFirstOrDefault<dynamic>(
                    "sp_GetDataGenRegisFormPDF",
                    new { ApplicantID = applicantId, JobID = jobId },
                    commandType: CommandType.StoredProcedure);

                if (form == null)
                    return NotFound("Data no found");

                var dict = form as IDictionary<string, object>;
                if (dict == null)
                    return StatusCode(500, "Internal Server error");

                QuestPDF.Settings.License = LicenseType.Community;
                var pdf = new PersonalDetailsV3Form(dict).GeneratePdf();

                return File(pdf, "application/pdf", $"form_{applicantId}.pdf");
            }
            catch (Exception)
            {
                // return StatusCode(500, new { error = ex.Message, stack = ex.StackTrace });
                return StatusCode(500, "Internal Server error");
            }
        }

        [HttpPost("GenerateTraineeFormPDF")]
        public IActionResult GenerateTraineeFormPDF([FromBody] JsonElement request)
        {
            try
            {
                if (!request.TryGetProperty("ApplicantID", out var applicantIdEl) ||
                    !request.TryGetProperty("JobID", out var jobIdEl))
                    return BadRequest("ApplicantID หรือ JobID ไม่ถูกต้อง");

                int applicantId = applicantIdEl.GetInt32();
                int jobId = jobIdEl.GetInt32();

                using var connection = new SqlConnection(
                    _config.GetConnectionString("DefaultConnection"));

                var form = connection.QueryFirstOrDefault<dynamic>(
                    "sp_GetDataTraineeFormPDF",
                    new { ApplicantID = applicantId, JobID = jobId },
                    commandType: CommandType.StoredProcedure);

                if (form == null)
                    return NotFound("Data not found");

                var dict = form as IDictionary<string, object>;
                if (dict == null)
                    return StatusCode(500, "Data conversion error");

                QuestPDF.Settings.License = LicenseType.Community;
                var pdf = new TraineeRegisterForm(dict).GeneratePdf();

                var fullName = dict.TryGetValue("FullNameThai", out var fullNameValue)
                    ? fullNameValue?.ToString()
                    : null;

                if (string.IsNullOrWhiteSpace(fullName))
                {
                    dict.TryGetValue("PrefixT", out var prefixValue);
                    dict.TryGetValue("NameFirstT", out var firstNameValue);
                    dict.TryGetValue("NameLastT", out var lastNameValue);
                    fullName = string.Join(" ", new[]
                    {
                        prefixValue?.ToString(),
                        firstNameValue?.ToString(),
                        lastNameValue?.ToString()
                    }.Where(value => !string.IsNullOrWhiteSpace(value)));
                }

                if (string.IsNullOrWhiteSpace(fullName))
                    fullName = "Unknown";

                foreach (var c in Path.GetInvalidFileNameChars())
                    fullName = fullName.Replace(c, '_');

                return File(pdf, "application/pdf", $"ใบสมัครฝึกงาน_{fullName}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace });
            }
        }

        [HttpPost("GenerateTraineeFormPDFPart2")]
        public IActionResult GenerateTraineeFormPDFPart2([FromBody] JsonElement request)
        {
            try
            {
                if (!request.TryGetProperty("ApplicantID", out var applicantIdEl) ||
                    !request.TryGetProperty("JobID", out var jobIdEl))
                    return BadRequest("ApplicantID หรือ JobID ไม่ถูกต้อง");

                int applicantId = applicantIdEl.GetInt32();
                int jobId = jobIdEl.GetInt32();

                using var connection = new SqlConnection(
                    _config.GetConnectionString("DefaultConnection"));

                var form = connection.QueryFirstOrDefault<dynamic>(
                    "sp_GetDataTraineeFormPDF",
                    new { ApplicantID = applicantId, JobID = jobId },
                    commandType: CommandType.StoredProcedure);

                if (form == null)
                    return NotFound("Data not found");

                var dict = form as IDictionary<string, object>;
                if (dict == null)
                    return StatusCode(500, "Data conversion error");

                QuestPDF.Settings.License = LicenseType.Community;
                var pdf = new TraineeRegisterFormPart2(dict).GeneratePdf();

                dict.TryGetValue("FirstNameThai", out var fnObj);
                dict.TryGetValue("LastNameThai", out var lnObj);
                var firstName = fnObj?.ToString() ?? "";
                var lastName = lnObj?.ToString() ?? "";
                var fullName = $"{firstName} {lastName}".Trim();
                if (string.IsNullOrWhiteSpace(fullName)) fullName = "Unknown";
                foreach (var ch in Path.GetInvalidFileNameChars())
                    fullName = fullName.Replace(ch, '_');

                return File(pdf, "application/pdf", $"ใบสมัครฝึกงาน_{fullName}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace });
            }
        }

        [HttpPost("GenerateRegisterFormPart1")]
        public IActionResult GenerateRegisterFormPart1([FromBody] JsonElement request)
        {
            try
            {
                if (!request.TryGetProperty("ApplicantID", out var applicantIdEl) ||
                    !request.TryGetProperty("JobID", out var jobIdEl))
                    return BadRequest("Bad Request");

                int applicantId = applicantIdEl.GetInt32();
                int jobId = jobIdEl.GetInt32();

                using var connection = new SqlConnection(
                    _config.GetConnectionString("DefaultConnection"));

                var form = connection.QueryFirstOrDefault<dynamic>(
                    "sp_GetDataRegisterPart1ForPDF",
                    new { ApplicantID = applicantId, JobID = jobId },
                    commandType: CommandType.StoredProcedure);

                if (form == null)
                    return NotFound("Data not found");

                var dict = form as IDictionary<string, object>;

                if (dict == null)
                    return StatusCode(500, "Data conversion error");

                QuestPDF.Settings.License = LicenseType.Community;

                var pdf = new RegisterFormPart1(dict).GeneratePdf();

                var fullName = dict.ContainsKey("FullNameThai")
                    ? dict["FullNameThai"]?.ToString()
                    : "Unknown";

                if (string.IsNullOrWhiteSpace(fullName))
                    fullName = "Unknown";

                foreach (var c in Path.GetInvalidFileNameChars())
                {
                    fullName = fullName.Replace(c, '_');
                }

                var fileName = $"ใบสมัครงาน_{fullName}.pdf";

                return File(pdf, "application/pdf", fileName);
            }
            // catch
            // {
            //     return StatusCode(500, "Internal Server error");
            // }
            catch (Exception ex)
            {
                return StatusCode(500, new {
                    message = ex.Message,
                    stack = ex.StackTrace
                });
            }
        }

        [HttpPost("GenerateTraineeCertificatePDF")]
        public IActionResult GenerateTraineeCertificatePDF([FromBody] JsonElement request)
        {
            try
            {
                if (!request.TryGetProperty("ApplicantID", out var applicantIdEl) ||
                    !request.TryGetProperty("JobID", out var jobIdEl))
                    return BadRequest("ApplicantID หรือ JobID ไม่ถูกต้อง");

                int applicantId = applicantIdEl.GetInt32();
                int jobId = jobIdEl.GetInt32();

                using var connection = new SqlConnection(
                    _config.GetConnectionString("DefaultConnection"));

                var form = connection.QueryFirstOrDefault<dynamic>(
                    "sp_GetInternshipCertificateData",
                    new { ApplicantID = applicantId, JobID = jobId },
                    commandType: CommandType.StoredProcedure);

                if (form == null)
                    return NotFound("Data not found");

                if (form is not IDictionary<string, object> dict)
                    return StatusCode(500, "Data conversion error");

                QuestPDF.Settings.License = LicenseType.Community;
                var pdf = new TraineeCertificateForm(dict).GeneratePdf();

                dict.TryGetValue("FirstNameThai", out var fnObj);
                dict.TryGetValue("LastNameThai", out var lnObj);
                var fullName = $"{fnObj} {lnObj}".Trim();
                if (string.IsNullOrWhiteSpace(fullName)) fullName = "Unknown";
                foreach (var c in Path.GetInvalidFileNameChars())
                    fullName = fullName.Replace(c, '_');

                return File(pdf, "application/pdf", $"หนังสือรับรองการฝึกงาน_{fullName}.pdf");
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message, stack = ex.StackTrace });
            }
        }

    }
}
