using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UiSettingsController(IConfiguration configuration) : ControllerBase
    {
        private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("DefaultConnection is not configured.");

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            using var conn = new SqlConnection(_connectionString);
            var rows = await conn.QueryAsync<(string Key, string Value)>(
                "sp_GetUiSettings",
                commandType: CommandType.StoredProcedure);

            var result = rows.ToDictionary(r => r.Key, r => r.Value);
            return Ok(result);
        }

        [HttpGet("fonts")]
        public async Task<IActionResult> GetFontOptions()
        {
            using var conn = new SqlConnection(_connectionString);
            var fonts = await conn.QueryAsync(
                "sp_GetFontOptions",
                commandType: CommandType.StoredProcedure);
            return Ok(fonts);
        }

        [HttpPut]
        public async Task<IActionResult> Upsert([FromBody] Dictionary<string, string> settings)
        {
            if (settings == null || settings.Count == 0)
                return BadRequest("No settings provided.");

            using var conn = new SqlConnection(_connectionString);
            foreach (var (key, value) in settings)
            {
                await conn.ExecuteAsync(
                    "sp_UpsertUiSettings",
                    new { SettingKey = key, SettingValue = value },
                    commandType: CommandType.StoredProcedure);
            }

            return Ok(new { updated = settings.Count });
        }
    }
}
