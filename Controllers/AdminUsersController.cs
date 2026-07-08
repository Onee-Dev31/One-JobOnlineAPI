using JobOnlineAPI.Filters;
using JobOnlineAPI.Models;
using JobOnlineAPI.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminUsersController(IAdminRepository adminRepository) : ControllerBase
    {
        private readonly IAdminRepository _adminRepository = adminRepository ?? throw new ArgumentNullException(nameof(adminRepository));

        [HttpGet]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<ActionResult<IEnumerable<AdminUserDetail>>> GetAllAdminUsers()
        {
            try
            {
                var adminUsers = await _adminRepository.GetAllAdminUsersAsync();
                if (adminUsers == null || !adminUsers.Any())
                    return NotFound("No admin users found.");

                return Ok(adminUsers);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server error");
            }
        }

        [HttpGet("{id:int}")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<ActionResult<AdminUserDetail>> GetAdminUserById(int id)
        {
            if (id <= 0)
                return BadRequest("Admin ID must be a positive integer.");

            try
            {
                var adminUser = await _adminRepository.GetAdminUserByIdAsync(id);
                if (adminUser == null)
                    return NotFound($"Admin user with ID {id} not found.");

                return Ok(adminUser);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server error");
            }
        }

        [HttpPost]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<ActionResult<object>> CreateAdminUser([FromBody] AdminUserCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username))
                return BadRequest("Username is required.");

            try
            {
                var existing = await _adminRepository.GetAdminUserByUsernameAsync(request.Username);
                if (existing != null)
                    return Conflict($"Username '{request.Username}' already exists.");

                var roles = await _adminRepository.GetAllRolesAsync();
                var secretaryRoleId = roles.FirstOrDefault(r => r.RoleName == "Secretary")?.RoleID;

                if (secretaryRoleId != null && request.RoleID == secretaryRoleId)
                {
                    if (string.IsNullOrWhiteSpace(request.ReportsToEmpNo))
                        return BadRequest("กรุณาระบุรหัสพนักงานของหัวหน้า (ReportsToEmpNo) สำหรับ Role Secretary");

                    var result = await _adminRepository.CreateSecretaryAdminUserAsync(request);
                    return CreatedAtAction(nameof(GetAdminUserById), new { id = result.NewAdminID }, new
                    {
                        AdminID = result.NewAdminID,
                        BossWasNewlyCreated = result.BossWasNewlyCreated,
                        ReportsToAdminID = result.BossAdminID,
                        ReportsToName = result.BossNameThai
                    });
                }

                var newId = await _adminRepository.CreateAdminUserAsync(request);
                return CreatedAtAction(nameof(GetAdminUserById), new { id = newId }, new { AdminID = newId });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server error");
            }
        }

        [HttpPut("{id:int}")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> UpdateAdminUser(int id, [FromBody] AdminUserUpdateRequest request)
        {
            if (id <= 0)
                return BadRequest("Admin ID must be a positive integer.");

            try
            {
                var existingAdminUser = await _adminRepository.GetAdminUserByIdAsync(id);
                if (existingAdminUser == null)
                    return NotFound($"Admin user with ID {id} not found.");

                var updated = await _adminRepository.UpdateAdminUserAsync(id, request);
                if (!updated)
                    return StatusCode(500, "Update failed.");

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server error");
            }
        }

        [HttpDelete("{id:int}")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> DeleteAdminUser(int id)
        {
            if (id <= 0)
                return BadRequest("Admin ID must be a positive integer.");

            try
            {
                var existingAdminUser = await _adminRepository.GetAdminUserByIdAsync(id);
                if (existingAdminUser == null)
                    return NotFound($"Admin user with ID {id} not found.");

                var deleted = await _adminRepository.DeleteAdminUserAsync(id);
                if (!deleted)
                    return StatusCode(500, "Delete failed.");

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server error");
            }
        }

        [HttpPatch("{id:int}/active")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> SetAdminUserActive(int id, [FromBody] SetAdminUserActiveRequest request)
        {
            if (id <= 0)
                return BadRequest("Admin ID must be a positive integer.");

            try
            {
                var existingAdminUser = await _adminRepository.GetAdminUserByIdAsync(id);
                if (existingAdminUser == null)
                    return NotFound($"Admin user with ID {id} not found.");

                var updated = await _adminRepository.SetAdminUserActiveAsync(id, request.IsActive);
                if (!updated)
                    return StatusCode(500, "Update active status failed.");

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server error");
            }
        }

        public class SetAdminUserActiveRequest
        {
            public bool IsActive { get; set; }
        }
    }
}
