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
    public class JobGroupsController(IConfiguration configuration) : ControllerBase
    {
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
        {
            using var conn = new SqlConnection(_connectionString);
            var groups = await conn.QueryAsync<JobGroup>(
                "sp_GetJobGroupsAdmin",
                new { JobGroupID = (int?)null, IncludeInactive = includeInactive },
                commandType: CommandType.StoredProcedure);

            return Ok(groups);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            var group = await conn.QueryFirstOrDefaultAsync<JobGroup>(
                "sp_GetJobGroupsAdmin",
                new { JobGroupID = id, IncludeInactive = true },
                commandType: CommandType.StoredProcedure);

            if (group == null)
                return NotFound();

            return Ok(group);
        }

        [HttpPost]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> Add([FromBody] JobGroup group)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var conn = new SqlConnection(_connectionString);
            var groupId = await conn.ExecuteScalarAsync<int>(
                "sp_AddJobGroup",
                new { group.GroupName, group.SortOrder, group.IsActive, group.CreatedBy },
                commandType: CommandType.StoredProcedure);

            return CreatedAtAction(nameof(GetById), new { id = groupId }, new { JobGroupID = groupId });
        }

        [HttpPut("{id}")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> Update(int id, [FromBody] JobGroup group)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var conn = new SqlConnection(_connectionString);
            var updated = await conn.QueryFirstOrDefaultAsync<JobGroup>(
                "sp_UpdateJobGroup",
                new { JobGroupID = id, group.GroupName, group.SortOrder, group.IsActive, group.UpdatedBy },
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
                "sp_DeleteJobGroup",
                new { JobGroupID = id, UpdatedBy = updatedBy },
                commandType: CommandType.StoredProcedure);

            return Ok();
        }
    }
}
