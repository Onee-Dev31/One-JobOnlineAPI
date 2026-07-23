using JobOnlineAPI.Filters;
using JobOnlineAPI.Models;
using JobOnlineAPI.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeesController(IEmployeeRepository employeeRepository) : ControllerBase
    {
        private readonly IEmployeeRepository _employeeRepository = employeeRepository;

        /// <summary>
        /// Search all employees from HRMS by company/department/free-text, for picking an employee
        /// on the admin-creation page. Unlike GetDataOpenFor, this is not restricted to job-opener permission.
        /// </summary>
        [HttpGet]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        [ProducesResponseType(typeof(EmployeeSearchResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> SearchEmployees(
            [FromQuery] string? companyCode,
            [FromQuery] string? department,
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (page < 1)
                return BadRequest("page must be at least 1.");

            if (pageSize < 1 || pageSize > 200)
                return BadRequest("pageSize must be between 1 and 200.");

            try
            {
                var result = await _employeeRepository.SearchEmployeesAsync(companyCode, department, search, page, pageSize);
                return Ok(result);
            }
            catch (Exception)
            {
                return StatusCode(500, "Internal Server error");
            }
        }
    }
}
