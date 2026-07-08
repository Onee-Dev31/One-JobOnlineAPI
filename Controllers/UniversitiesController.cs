using JobOnlineAPI.Services;
using Microsoft.AspNetCore.Mvc;

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
    }
}
