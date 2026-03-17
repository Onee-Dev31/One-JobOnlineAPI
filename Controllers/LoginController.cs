using Microsoft.AspNetCore.Mvc;
using JobOnlineAPI.Filters;
using JobOnlineAPI.Services;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Dapper;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using System.Data;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoginController(IUserService userService, IJwtTokenService jwtTokenService, IConfiguration configuration) : ControllerBase
    {
        private readonly IUserService _userService = userService ?? throw new ArgumentNullException(nameof(userService));
        private readonly IJwtTokenService _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        [HttpPost]
        [EnableRateLimiting("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest loginRequest)
        {
            try
            {
                var adminUser = await _userService.AuthenticateAsync(loginRequest.Username, loginRequest.Password, loginRequest.JobID);

                if (adminUser != null)
                {
                    if (!adminUser.UserId.HasValue)
                    {
                        return BadRequest(new { message = "UserId is missing for the authenticated user." });
                    }

                    var userModel = new UserModel
                    {
                        Username = adminUser.Username,
                        Role = adminUser.Role,
                        ConfirmConsent = adminUser.ConfirmConsent ?? string.Empty,
                        UserId = adminUser.UserId.Value,
                        ApplicantID = adminUser.ApplicantID,
                        JobID = adminUser.JobID,
                        Status = adminUser.Status
                    };

                    var token = _jwtTokenService.GenerateJwtToken(userModel);

                    var refreshToken = _jwtTokenService.GenerateRefreshToken(userModel.Username, userModel.Role);
                    Response.Cookies.Append("refreshToken", refreshToken, new CookieOptions
                    {
                        HttpOnly = true,
                        Secure = true,
                        SameSite = SameSiteMode.Strict,
                        Expires = DateTimeOffset.UtcNow.AddDays(1),
                        Path = "/api/Auth/refresh"
                    });

                    return Ok(new { Token = token, userModel.Username, userModel.Role, userModel.ConfirmConsent, userModel.UserId, userModel.ApplicantID, userModel.JobID, userModel.Status });
                }

                return Unauthorized("Invalid username or password.");
            }
            catch (Exception)
            {
                return StatusCode(500, "An unexpected error occurred. Please try again later.");
            }
        }

        [HttpPost("change-password")]
        [JwtAuthorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                var emailFromToken = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                    ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrWhiteSpace(emailFromToken))
                    return Unauthorized(new { message = "Invalid token." });

                if (string.IsNullOrWhiteSpace(request.NewPassword))
                {
                    return BadRequest(new { message = "New password is required." });
                }

                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

                using var connection = new SqlConnection(_connectionString);
                var parameters = new DynamicParameters();
                parameters.Add("@Email", emailFromToken);
                parameters.Add("@PasswordHash", hashedPassword);
                parameters.Add("@Status", dbType: DbType.Int32, direction: ParameterDirection.Output);
                parameters.Add("@Message", dbType: DbType.String, size: 100, direction: ParameterDirection.Output);

                await connection.ExecuteAsync(
                    "sp_ChangeUserPassword",
                    parameters,
                    commandType: CommandType.StoredProcedure);

                int status = parameters.Get<int>("@Status");
                string message = parameters.Get<string>("@Message");

                if (status == 1)
                {
                    return Ok(new { message });
                }
                else if (message.Contains("User not found"))
                {
                    return NotFound(new { message });
                }
                else
                {
                    // return StatusCode(500, new { message });
                    return StatusCode(500, "Internal Server error");
                }
            }
            catch (Exception)
            {
                // return StatusCode(500, "An unexpected error occurred. Please try again later.");
                return StatusCode(500, "Internal Server error");
            }
        }
    }

    public class LoginRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
        public required int JobID { get; set; }
        
    }

    public class ChangePasswordRequest
    {
        [Required]
        [MinLength(8)]
        public required string NewPassword { get; set; }
    }

    public class UserModel
    {
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string ConfirmConsent { get; set; } = string.Empty;
        public int? ApplicantID { get; set; }
        public int? JobID { get; set; }
        public string? Status { get; set; } = string.Empty;
    }
}