using Dapper;
using JobOnlineAPI.DAL;
using JobOnlineAPI.Filters;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [JwtAuthorize]
    public class AuditLogController(DapperContext context, ILogger<AuditLogController> logger) : ControllerBase
    {
        private readonly DapperContext _context = context;
        private readonly ILogger<AuditLogController> _logger = logger;

        [HttpPost]
        public async Task<IActionResult> Log([FromBody] AuditLogRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Action))
                return BadRequest(new { Error = "Action is required." });

            var email = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                ?? "unknown";

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

            try
            {
                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@Email", email);
                parameters.Add("@Action", request.Action);
                parameters.Add("@Detail", request.Detail);
                parameters.Add("@IPAddress", ipAddress);

                await connection.ExecuteAsync(
                    "[dbo].[usp_InsertAuditLog]",
                    parameters,
                    commandType: System.Data.CommandType.StoredProcedure);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AuditLog: failed to insert log for Email: {Email}, Action: {Action}", email, request.Action);
                return Ok(); // fire-and-forget: never block user flow
            }
        }
    }

    public class AuditLogRequest
    {
        public required string Action { get; set; }
        public string? Detail { get; set; }
    }
}
