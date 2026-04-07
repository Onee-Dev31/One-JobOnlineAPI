using JobOnlineAPI.Models;
using JobOnlineAPI.Repositories;

namespace JobOnlineAPI.Tests.Fakes;

public class FakeJobRepository : IJobRepository
{
    public List<Job> Jobs { get; set; } = new();

    public Task<IEnumerable<Job>> GetAllJobsAsync() =>
        Task.FromResult<IEnumerable<Job>>(Jobs);

    public Task<Job> GetJobByIdAsync(int id) =>
        Task.FromResult(Jobs.FirstOrDefault(j => j.JobID == id)!);

    public Task<int> AddJobAsync(Job job)
    {
        int newId = Jobs.Count > 0 ? (Jobs.Max(j => j.JobID) ?? 0) + 1 : 1;
        job.JobID = newId;
        Jobs.Add(job);
        return Task.FromResult(newId);
    }

    public Task<int> UpdateJobAsync(Job job) => Task.FromResult(1);
    public Task<int> DeleteJobAsync(int id) => Task.FromResult(1);
}
