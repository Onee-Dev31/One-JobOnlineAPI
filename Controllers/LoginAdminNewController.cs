using Dapper;
using Microsoft.AspNetCore.Mvc;
using JobOnlineAPI.DAL;
using JobOnlineAPI.Services;
using System.Text.Json;
using System.Text;
using System.Net.Http;
using System.Data;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginAdminNewController(DapperContext context, IConfiguration configuration, IJwtTokenService jwtTokenService, IOtpService otpService) : ControllerBase
    {
        private readonly DapperContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        private readonly IJwtTokenService _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        private readonly IOtpService _otpService = otpService ?? throw new ArgumentNullException(nameof(otpService));

        public class LoginRequestAdmin
        {
            public required string Username { get; set; }
            public required string Password { get; set; }
        }

        [HttpPost("LoginAD")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> LoginAdminAD([FromBody] LoginRequestAdmin request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username and Password are required.");

            try
            {
                var (accessToken, refreshToken) = await LoginWithADAsync(request.Username, request.Password);
                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@Username", request.Username);

                var user = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "EXEC sp_GetAdminUsersWithRoleV2 @Username", parameters);

                if (user == null)
                    return Unauthorized("Invalid username.");

                var userDict = (IDictionary<string, object>)user;

                var roleName = userDict.TryGetValue("ROLE_NAME", out var roleObj) ? roleObj?.ToString() ?? "" : "";
                var localToken = _jwtTokenService.GenerateJwtToken(new UserModel
                {
                    Username = request.Username,
                    Role = roleName,
                    UserId = 0
                });

                userDict["accessToken"] = localToken;
                userDict["refreshToken"] = refreshToken;
                userDict["essToken"] = accessToken;
                await AttachFullCompanyDepartmentListsIfSuperUserAsync(connection, userDict);
                Response.Cookies.Append("token", localToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddHours(2)
                });
                Response.Cookies.Append("admin_token", localToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddHours(2)
                });
                Response.Cookies.Append("admin_refresh", refreshToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddHours(4)
                });
                Response.Cookies.Append("admin_role_srv", roleName ?? "", new CookieOptions
                {
                    HttpOnly = true,
                    Secure = false,
                    SameSite = SameSiteMode.Lax,
                    Expires = DateTime.UtcNow.AddHours(2)
                });
                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Internal Server error",
                    detail = ex.Message
                });
            }
        }
        
        [HttpPost("request-otp")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> RequestOtp([FromBody] RequestOtpAdminRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                return BadRequest(new { message = "กรุณาระบุ Username" });

            try
            {
                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@Username", request.Username);

                var user = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "EXEC sp_GetAdminUsersWithRoleV2 @Username", parameters);

                if (user == null)
                    return NotFound(new { message = "ไม่พบ Username นี้ในระบบ" });

                // ดึง email จาก HRMS via linked server
                var emailResult = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT TOP 1 e.EMAIL FROM [HRMS_LINKED_SERVER].HRMS.Dbo.T_EMPLOYEE_SSO e WHERE LOWER(e.AD_USER) = LOWER(@Username)",
                    new { Username = request.Username });

                string? adminEmail = emailResult?.EMAIL?.ToString();
                if (string.IsNullOrWhiteSpace(adminEmail))
                    return BadRequest(new { message = "ไม่พบอีเมลของ user นี้ในระบบ" });

                var otpResult = await _otpService.RequestOtpAsync(request.Username, adminEmail, "ADMIN_LOGIN");
                if (!otpResult.Item1)
                    return BadRequest(new { message = otpResult.Item2 });

                // mask email: abc***@domain.com
                var atIndex = adminEmail.IndexOf('@');
                var maskedEmail = atIndex > 2
                    ? adminEmail.Substring(0, 2) + "***" + adminEmail.Substring(atIndex)
                    : "***" + adminEmail.Substring(atIndex);

                return Ok(new { message = $"ส่ง OTP ไปที่ {maskedEmail} แล้ว" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal Server error", detail = ex.Message });
            }
        }

        [HttpPost("verify-otp")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpAdminRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.OTP))
                return BadRequest(new { message = "กรุณาระบุ Username และ OTP" });

            try
            {
                var isValid = await _otpService.VerifyOtpAsync(request.Username, request.OTP, "ADMIN_LOGIN");
                if (!isValid)
                    return Unauthorized(new { message = "OTP ไม่ถูกต้องหรือหมดอายุแล้ว" });

                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@Username", request.Username);

                var user = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "EXEC sp_GetAdminUsersWithRoleV2 @Username", parameters);

                if (user == null)
                    return Unauthorized(new { message = "ไม่พบ Username นี้ในระบบ" });

                var userDict = (IDictionary<string, object>)user;
                var roleName = userDict.TryGetValue("ROLE_NAME", out var roleObj) ? roleObj?.ToString() ?? "" : "";

                var localToken = _jwtTokenService.GenerateJwtToken(new UserModel
                {
                    Username = request.Username,
                    Role = roleName,
                    UserId = 0
                });

                foreach (var (name, value, hours) in new[]
                {
                    ("token", localToken, 2),
                    ("admin_token", localToken, 2),
                    ("admin_role_srv", roleName, 2),
                })
                {
                    Response.Cookies.Append(name, value, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = false,
                        SameSite = SameSiteMode.Lax,
                        Expires = DateTime.UtcNow.AddHours(hours)
                    });
                }

                userDict["accessToken"] = localToken;
                userDict["refreshToken"] = "";
                userDict["roleName"] = roleName;
                await AttachFullCompanyDepartmentListsIfSuperUserAsync(connection, userDict);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Internal Server error", detail = ex.Message });
            }
        }

        private static async Task AttachFullCompanyDepartmentListsIfSuperUserAsync(IDbConnection connection, IDictionary<string, object> userDict)
        {
            var canViewAll = userDict.TryGetValue("CanViewAllCompanies", out var flagObj)
                && flagObj is bool flag && flag;
            if (!canViewAll)
                return;

            var companies = await connection.QueryAsync(
                "sp_GetCompanyInfo",
                commandType: CommandType.StoredProcedure);

            var departments = await connection.QueryAsync(
                "sp_GetDepartmentByComCodeNew",
                new { comCode = (string?)null },
                commandType: CommandType.StoredProcedure);

            userDict["Companies"] = companies;
            userDict["Departments"] = departments;
        }

        private async Task<(string accessToken, string refreshToken)> LoginWithADAsync(string username, string password)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            using var httpClient = new HttpClient(handler);

            var payload = new
            {
                username,
                password
            };

            var response = await httpClient.PostAsync(
                "https://10.10.0.28:7060/api/auth/bypass-token",
                new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                )
            );

            if (!response.IsSuccessStatusCode)
                throw new Exception("Login AD ไม่สำเร็จ");

            var raw = await response.Content.ReadAsStringAsync();
            var json = JsonSerializer.Deserialize<JsonElement>(raw);

            var accessToken = json.GetProperty("accessToken").GetString();
            var refreshToken = json.GetProperty("refreshToken").GetString();

            return (accessToken!, refreshToken!);
        }
    }

    public class RequestOtpAdminRequest
    {
        public required string Username { get; set; }
    }

    public class VerifyOtpAdminRequest
    {
        public required string Username { get; set; }
        public required string OTP { get; set; }
    }
}