using Dapper;
using Microsoft.AspNetCore.Mvc;
using JobOnlineAPI.DAL;
using System.Data;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobApplicationController : ControllerBase
    {
        private readonly DapperContext _context;
        private readonly ILogger<JobApplicationController> _logger;

        public JobApplicationController(
            DapperContext context,
            ILogger<JobApplicationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPut("update-to-success")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateApplicationToSuccess([FromQuery] int applicationId)
        {
            try
            {
                if (applicationId <= 0)
                {
                    _logger.LogWarning("Invalid ApplicationID: {ApplicationId}", applicationId);
                    return BadRequest("Invalid ApplicationID.");
                }

                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@ApplicationID", applicationId);

                var result = await connection.QuerySingleOrDefaultAsync<string>(
                    "UpdateJobApplicationToSuccess",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                if (result == null)
                {
                    return BadRequest("อัปเดตไม่สำเร็จ กรุณาตรวจสอบข้อมูลอีกครั้ง");
                }

                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application to success for ApplicationID {ApplicationId}: {Message}", applicationId, ex.Message);
                return StatusCode(500, new { Error = "เกิดข้อผิดพลาดในการอัปเดต" });
            }
        }

        [HttpGet("applications-with-link-status")]
        [ProducesResponseType(typeof(IEnumerable<dynamic>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetJobApplicationsWithLinkStatus()
        {
            try
            {
                using var connection = _context.CreateConnection();

                var applications = await connection.QueryAsync<dynamic>(
                    "spGetJobApplicationsWithLinkStatus",
                    commandType: CommandType.StoredProcedure
                );

                return Ok(applications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job applications with link status: {Message}", ex.Message);
                return StatusCode(500, new { Error = "เกิดข้อผิดพลาดในการดึงข้อมูล" });
            }
        }
    }
}