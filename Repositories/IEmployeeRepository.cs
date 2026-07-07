using JobOnlineAPI.Models;

namespace JobOnlineAPI.Repositories
{
    public interface IEmployeeRepository
    {
        Task<EmployeeSearchResponse> SearchEmployeesAsync(string? companyCode, string? department, string? search, int page, int pageSize);
    }
}
