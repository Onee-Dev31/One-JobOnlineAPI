using Dapper;
using JobOnlineAPI.Filters;
using JobOnlineAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EmployeeTypesController(IConfiguration configuration) : ControllerBase
    {
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
        {
            using var conn = new SqlConnection(_connectionString);
            var types = await conn.QueryAsync<EmployeeType>(
                "sp_GetEmployeeTypesAdmin",
                new { EmployeeTypeID = (int?)null, IncludeInactive = includeInactive },
                commandType: CommandType.StoredProcedure);

            return Ok(types);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            var type = await conn.QueryFirstOrDefaultAsync<EmployeeType>(
                "sp_GetEmployeeTypesAdmin",
                new { EmployeeTypeID = id, IncludeInactive = true },
                commandType: CommandType.StoredProcedure);

            if (type == null)
                return NotFound();

            return Ok(type);
        }

        [HttpPost]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> Add([FromBody] EmployeeType type)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var conn = new SqlConnection(_connectionString);
            var typeId = await conn.ExecuteScalarAsync<int>(
                "sp_AddEmployeeType",
                new { type.TypeName, type.SortOrder, type.IsActive, type.CreatedBy },
                commandType: CommandType.StoredProcedure);

            return CreatedAtAction(nameof(GetById), new { id = typeId }, new { EmployeeTypeID = typeId });
        }

        [HttpPut("{id}")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> Update(int id, [FromBody] EmployeeType type)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var conn = new SqlConnection(_connectionString);
            var updated = await conn.QueryFirstOrDefaultAsync<EmployeeType>(
                "sp_UpdateEmployeeType",
                new { EmployeeTypeID = id, type.TypeName, type.SortOrder, type.IsActive, type.UpdatedBy },
                commandType: CommandType.StoredProcedure);

            if (updated == null)
                return NotFound();

            return Ok(updated);
        }

        [HttpDelete("{id}")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> Delete(int id, [FromQuery] string? updatedBy)
        {
            using var conn = new SqlConnection(_connectionString);
            await conn.ExecuteAsync(
                "sp_DeleteEmployeeType",
                new { EmployeeTypeID = id, UpdatedBy = updatedBy },
                commandType: CommandType.StoredProcedure);

            return Ok();
        }
    }
}
