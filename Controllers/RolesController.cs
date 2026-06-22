using JobOnlineAPI.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolesController(IAdminRepository adminRepository) : ControllerBase
    {
        private readonly IAdminRepository _adminRepository = adminRepository;

        [HttpGet]
        public async Task<IActionResult> GetAllRoles()
        {
            try
            {
                var roles = await _adminRepository.GetAllRolesAsync();
                return Ok(roles);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server error");
            }
        }
    }
}
