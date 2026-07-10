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

                var items = (await _adminRepository.GetRoutesByRoleNameWithDetailAsync(roleName)).ToList();
                return Ok(new MyPermissionResponse
                {
                    Routes = items.Select(i => i.RoutePath).ToList(),
                    Items = items
                });
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

        [HttpGet("manage")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> GetAllRolePermissionsDetail()
        {
            try
            {
                var items = await _adminRepository.GetAllRolePermissionsDetailAsync();
                return Ok(items);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server error");
            }
        }

        [HttpPost]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> CreateRolePermission([FromBody] RolePermissionCreateRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.RoutePath))
                return BadRequest("RoutePath is required.");

            try
            {
                var existing = await _adminRepository.GetRolePermissionByRoleAndRouteAsync(request.RoleID, request.RoutePath);
                if (existing != null)
                    return Conflict($"Route '{request.RoutePath}' is already assigned to this role.");

                var newId = await _adminRepository.CreateRolePermissionAsync(request);
                return StatusCode(StatusCodes.Status201Created, new { ID = newId });
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server error");
            }
        }

        [HttpPut("{id:int}")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> UpdateRolePermission(int id, [FromBody] RolePermissionUpdateRequest request)
        {
            if (id <= 0)
                return BadRequest("ID must be a positive integer.");

            try
            {
                var updated = await _adminRepository.UpdateRolePermissionAsync(id, request);
                if (!updated)
                    return NotFound($"Role permission with ID {id} not found.");

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server error");
            }
        }

        [HttpDelete("{id:int}")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> DeleteRolePermission(int id)
        {
            if (id <= 0)
                return BadRequest("ID must be a positive integer.");

            try
            {
                var deleted = await _adminRepository.DeleteRolePermissionAsync(id);
                if (!deleted)
                    return NotFound($"Role permission with ID {id} not found.");

                return NoContent();
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server error");
            }
        }
    }
}
