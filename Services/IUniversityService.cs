using JobOnlineAPI.Models;

namespace JobOnlineAPI.Services
{
    public interface IUniversityService
    {
        Task<IEnumerable<University>> GetUniversitiesAsync();
        Task<IEnumerable<University>> GetUniversitiesAdminAsync(bool includeInactive);
        Task<University?> GetUniversityByIdAsync(int id);
        Task<int> AddUniversityAsync(University university);
        Task<University?> UpdateUniversityAsync(int id, University university);
        Task DeleteUniversityAsync(int id);
    }
}
