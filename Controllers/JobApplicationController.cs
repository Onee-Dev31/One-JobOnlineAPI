using Dapper;
using Microsoft.AspNetCore.Mvc;
using JobOnlineAPI.DAL;
using System.Data;
using JobOnlineAPI.Filters;
using JobOnlineAPI.Services;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JobApplicationController(
        DapperContext context,
        ILogger<JobApplicationController> logger,
        IEmailNotificationService emailNotificationService) : ControllerBase
    {
        private readonly DapperContext _context = context;
        private readonly ILogger<JobApplicationController> _logger = logger;
        private readonly IEmailNotificationService _emailNotificationService = emailNotificationService;

        [HttpPut("update-to-success")]
        //[TypeFilter(typeof(JwtAuthorizeAttribute))]
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

                var result = await connection.QuerySingleOrDefaultAsync<dynamic>(
                    "UpdateJobApplicationToSuccess",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                if (result == null)
                {
                    return BadRequest("อัปเดตไม่สำเร็จ กรุณาตรวจสอบข้อมูลอีกครั้ง");
                }

                string message = result.Message ?? "อัปเดตสำเร็จ";
                _logger.LogInformation("Application updated to success for ApplicationID {ApplicationId}. Email queued for sending.", applicationId);

                return Ok(new { message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating application to success for ApplicationID {ApplicationId}: {Message}", applicationId, ex.Message);
                return StatusCode(500, new { Error = "เกิดข้อผิดพลาดในการอัปเดต" });
            }
        }

        [HttpGet("applications-with-link-status")]
        //[TypeFilter(typeof(JwtAuthorizeAttribute))]
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

        [HttpGet("pending-emails")]
        [ProducesResponseType(typeof(IEnumerable<dynamic>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetPendingEmailQueue()
        {
            try
            {
                using var connection = _context.CreateConnection();

                var pendingEmails = await connection.QueryAsync<dynamic>(
                    "sp_GetPendingEmailQueue",
                    commandType: CommandType.StoredProcedure
                );

                _logger.LogInformation("Retrieved {Count} pending emails.", pendingEmails.Count());

                return Ok(pendingEmails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving pending email queue: {Message}", ex.Message);
                return StatusCode(500, new { Error = "เกิดข้อผิดพลาดในการดึงข้อมูลอีเมลที่รอส่ง" });
            }
        }

        [HttpGet("sent-emails")]
        [ProducesResponseType(typeof(IEnumerable<dynamic>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetSentEmailQueue()
        {
            try
            {
                using var connection = _context.CreateConnection();

                var sentEmails = await connection.QueryAsync<dynamic>(
                    "sp_GetSentEmailQueue",
                    commandType: CommandType.StoredProcedure
                );

                _logger.LogInformation("Retrieved {Count} sent emails.", sentEmails.Count());

                return Ok(sentEmails);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving sent email queue: {Message}", ex.Message);
                return StatusCode(500, new { Error = "เกิดข้อผิดพลาดในการดึงข้อมูลอีเมลที่ส่งแล้ว" });
            }
        }

        [HttpPut("cancel-pending-email")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CancelPendingEmail([FromQuery] int queueId)
        {
            try
            {
                if (queueId <= 0)
                {
                    _logger.LogWarning("Invalid QueueID: {QueueId}", queueId);
                    return BadRequest("Invalid QueueID.");
                }

                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@QueueID", queueId);

                var result = await connection.QuerySingleOrDefaultAsync<dynamic>(
                    "sp_CancelPendingEmail",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                if (result == null)
                {
                    return BadRequest("ยกเลิกไม่สำเร็จ กรุณาตรวจสอบข้อมูลอีกครั้ง");
                }

                string message = result.Message ?? "ยกเลิกสำเร็จ";
                _logger.LogInformation("Cancelled pending email for QueueID {QueueId}.", queueId);

                return Ok(new { message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling pending email for QueueID {QueueId}: {Message}", queueId, ex.Message);
                return StatusCode(500, new { Error = "เกิดข้อผิดพลาดในการยกเลิก" });
            }
        }
    }
}