using System.Net;
using System.Net.Http.Json;
using JobOnlineAPI.Models;
using JobOnlineAPI.Tests.Fakes;
using JobOnlineAPI.Tests.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace JobOnlineAPI.Tests.Controllers;

public class JobsControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly FakeJobRepository _fakeRepo;
    private readonly HttpClient _client;

    public JobsControllerTests(TestWebApplicationFactory factory)
    {
        _fakeRepo = factory.Services.GetRequiredService<FakeJobRepository>();
        _client = factory.CreateClient();
    }

    // ==================== GET /api/jobs ====================

    [Fact(DisplayName = "GET /jobs: มีงาน → 200")]
    public async Task GetAll_Returns200_WhenJobsExist()
    {
        _fakeRepo.Jobs = new List<Job> { MakeJob(1, "Software Engineer") };
        var res = await _client.GetAsync("/api/jobs");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact(DisplayName = "GET /jobs: ไม่มีงาน → 404")]
    public async Task GetAll_Returns404_WhenNoJobs()
    {
        _fakeRepo.Jobs = new List<Job>();
        var res = await _client.GetAsync("/api/jobs");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    // ==================== GET /api/jobs/{id} ====================

    [Fact(DisplayName = "GET /jobs/{id}: พบงาน → 200")]
    public async Task GetById_Returns200_WhenJobExists()
    {
        _fakeRepo.Jobs = new List<Job> { MakeJob(5, "UX Designer") };
        var res = await _client.GetAsync("/api/jobs/5");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact(DisplayName = "GET /jobs/{id}: ไม่พบงาน → 404")]
    public async Task GetById_Returns404_WhenJobNotFound()
    {
        _fakeRepo.Jobs = new List<Job>();
        var res = await _client.GetAsync("/api/jobs/999");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact(DisplayName = "GET /jobs/{id}: id = 0 → 400")]
    public async Task GetById_Returns400_WhenIdIsZero()
    {
        var res = await _client.GetAsync("/api/jobs/0");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact(DisplayName = "GET /jobs/{id}: id ติดลบ → 400")]
    public async Task GetById_Returns400_WhenIdIsNegative()
    {
        var res = await _client.GetAsync("/api/jobs/-1");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ==================== POST /api/jobs ====================

    [Fact(DisplayName = "POST /jobs: ข้อมูลถูกต้อง → 201")]
    public async Task AddJob_Returns201_WhenValid()
    {
        _fakeRepo.Jobs = new List<Job>();
        var res = await _client.PostAsJsonAsync("/api/jobs", MakeJob(null, "DevOps Engineer"));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
    }

    [Fact(DisplayName = "POST /jobs: ไม่มีชื่อตำแหน่ง → 400")]
    public async Task AddJob_Returns400_WhenTitleIsEmpty()
    {
        var res = await _client.PostAsJsonAsync("/api/jobs", MakeJob(null, ""));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ==================== PUT /api/jobs/{id} — ต้อง JWT ====================

    [Fact(DisplayName = "PUT /jobs/{id}: ไม่มี token → 401")]
    public async Task UpdateJob_Returns401_WhenNoToken()
    {
        var res = await _client.PutAsJsonAsync("/api/jobs/1", MakeJob(1, "Dev"));
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ==================== DELETE /api/jobs/{id} — ต้อง JWT ====================

    [Fact(DisplayName = "DELETE /jobs/{id}: ไม่มี token → 401")]
    public async Task DeleteJob_Returns401_WhenNoToken()
    {
        var res = await _client.DeleteAsync("/api/jobs/1");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ==================== helper ====================

    private static Job MakeJob(int? id, string title) => new()
    {
        JobID = id,
        JobTitle = title,
        JobDescription = "Test description",
        Requirements = "Test requirements",
        Location = "Bangkok",
        ExperienceYears = "2",
        NumberOfPositions = 1,
        Department = "IT",
        JobStatus = "Open",
        ClosingDate = DateTime.Now.AddDays(30)
    };
}
