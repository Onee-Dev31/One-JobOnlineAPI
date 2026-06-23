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

        [HttpGet("by-role/{roleName}")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> GetRoutesByRole(string roleName)
        {
            try
            {
                var routes = await _adminRepository.GetRoutesByRoleNameWithDetailAsync(roleName);
                return Ok(routes);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server error");
            }
        }

        [HttpPut("reorder")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> ReorderRoutes([FromBody] List<RoutesSortOrderItem> items)
        {
            if (items == null || items.Count == 0)
                return BadRequest("Items are required.");

            try
            {
                await _adminRepository.UpdateRoutesSortOrderAsync(items);
                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server error");
            }
        }
    }
}
