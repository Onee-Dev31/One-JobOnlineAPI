using JobOnlineAPI.Filters;
using JobOnlineAPI.Models;
using JobOnlineAPI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Data.SqlClient;

namespace JobOnlineAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UniversitiesController(IUniversityService universityService) : ControllerBase
    {
        private readonly IUniversityService _universityService = universityService;

        [HttpGet]
        public async Task<IActionResult> GetUniversities()
        {
            var universities = await _universityService.GetUniversitiesAsync();
            return Ok(universities);
        }

        [HttpGet("admin")]
        public async Task<IActionResult> GetAllAdmin([FromQuery] bool includeInactive = true)
        {
            var universities = await _universityService.GetUniversitiesAdminAsync(includeInactive);
            return Ok(universities);
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var university = await _universityService.GetUniversityByIdAsync(id);
            if (university == null)
                return NotFound();

            return Ok(university);
        }

        [HttpPost]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> Add([FromBody] University university)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _universityService.GetUniversitiesAdminAsync(true);
            if (existing.Any(u => u.UniversityNameThai == university.UniversityNameThai))
                return Conflict($"University '{university.UniversityNameThai}' already exists.");

            try
            {
                var newId = await _universityService.AddUniversityAsync(university);
                return CreatedAtAction(nameof(GetById), new { id = newId }, new { UniversityID = newId });
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id:int}")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> Update(int id, [FromBody] University university)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existing = await _universityService.GetUniversitiesAdminAsync(true);
            if (existing.Any(u => u.UniversityID != id && u.UniversityNameThai == university.UniversityNameThai))
                return Conflict($"University '{university.UniversityNameThai}' already exists.");

            try
            {
                var updated = await _universityService.UpdateUniversityAsync(id, university);
                if (updated == null)
                    return NotFound();

                return Ok(updated);
            }
            catch (SqlException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id:int}")]
        [TypeFilter(typeof(JwtAuthorizeAttribute))]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _universityService.GetUniversityByIdAsync(id);
            if (existing == null)
                return NotFound();

            await _universityService.DeleteUniversityAsync(id);
            return Ok();
        }
    }
}
