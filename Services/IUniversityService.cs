using JobOnlineAPI.Models;

namespace JobOnlineAPI.Services
{
    public interface IUniversityService
    {
        Task<IEnumerable<University>> GetUniversitiesAsync();
    }
}
