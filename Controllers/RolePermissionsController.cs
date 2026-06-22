using JobOnlineAPI.Filters;
using JobOnlineAPI.Models;
using JobOnlineAPI.Repositories;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RolePermissionsController(IAdminRepository adminRepository) : ControllerBase
    {
        private readonly IAdminRepository _adminRepository = adminRepository;

        [HttpGet]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> GetAllRolePermissions()
        {
            try
            {
                var flat = await _adminRepository.GetAllRolePermissionsAsync();
                var grouped = flat
                    .GroupBy(x => new { x.RoleID, x.RoleName })
                    .Select(g => new RolePermissionResponse
                    {
                        RoleID = g.Key.RoleID,
                        RoleName = g.Key.RoleName,
                        Routes = g.Select(x => x.RoutePath).ToList()
                    })
                    .ToList();

                return Ok(grouped);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server error");
            }
        }

        [HttpGet("my")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> GetMyPermissions()
        {
            try
            {
                var roleName = User.FindFirst(ClaimTypes.Role)?.Value;
                if (string.IsNullOrEmpty(roleName))
                    return Unauthorized("Role not found in token.");

                var routes = await _adminRepository.GetRoutesByRoleNameAsync(roleName);
                return Ok(new MyPermissionResponse { Routes = routes.ToList() });
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server error");
            }
        }
    }
}
