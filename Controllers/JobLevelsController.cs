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
    public class JobLevelsController(IConfiguration configuration) : ControllerBase
    {
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] bool includeInactive = false)
        {
            using var conn = new SqlConnection(_connectionString);
            var levels = await conn.QueryAsync<JobLevel>(
                "sp_GetJobLevelsAdmin",
                new { JobLevelID = (int?)null, IncludeInactive = includeInactive },
                commandType: CommandType.StoredProcedure);

            return Ok(levels);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            using var conn = new SqlConnection(_connectionString);
            var level = await conn.QueryFirstOrDefaultAsync<JobLevel>(
                "sp_GetJobLevelsAdmin",
                new { JobLevelID = id, IncludeInactive = true },
                commandType: CommandType.StoredProcedure);

            if (level == null)
                return NotFound();

            return Ok(level);
        }

        [HttpPost]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> Add([FromBody] JobLevel level)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var conn = new SqlConnection(_connectionString);
            var levelId = await conn.ExecuteScalarAsync<int>(
                "sp_AddJobLevel",
                new { level.LevelName, level.SortOrder, level.IsActive, level.CreatedBy },
                commandType: CommandType.StoredProcedure);

            return CreatedAtAction(nameof(GetById), new { id = levelId }, new { JobLevelID = levelId });
        }

        [HttpPut("{id}")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> Update(int id, [FromBody] JobLevel level)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            using var conn = new SqlConnection(_connectionString);
            var updated = await conn.QueryFirstOrDefaultAsync<JobLevel>(
                "sp_UpdateJobLevel",
                new { JobLevelID = id, level.LevelName, level.SortOrder, level.IsActive, level.UpdatedBy },
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
                "sp_DeleteJobLevel",
                new { JobLevelID = id, UpdatedBy = updatedBy },
                commandType: CommandType.StoredProcedure);

            return Ok();
        }
    }
}
