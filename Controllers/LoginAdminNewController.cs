using Dapper;
using Microsoft.AspNetCore.Mvc;
using JobOnlineAPI.DAL;
using System.Text.Json;
using System.Text;
using System.Net.Http;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginAdminNewController(DapperContext context, IConfiguration configuration) : ControllerBase
    {
        private readonly DapperContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));

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
            if (string.IsNullOrWhiteSpace(request.Username) && string.IsNullOrWhiteSpace(request.Password))
                return BadRequest("Username or Password is incorrect and cannot be empty or whitespace.");

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

                userDict["accessToken"] = accessToken;
                userDict["refreshToken"] = refreshToken;
                Response.Cookies.Append("token", accessToken, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
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
        private async Task<(string accessToken, string refreshToken)> LoginWithADAsync(string username, string password)
        {
            var env = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>();
            var handler = new HttpClientHandler();
            if (env.IsDevelopment())
                handler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

            using var httpClient = new HttpClient(handler);

            var payload = new
            {
                username,
                password
            };

            var response = await httpClient.PostAsync(
                "https://10.10.0.28:7054/api/auth/token",
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
}