using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using JobOnlineAPI.Tests.Helpers;
using Xunit;

namespace JobOnlineAPI.Tests.Controllers;

public class LoginControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public LoginControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Login_UserNotFound_Returns401()
    {
        var res = await _client.PostAsJsonAsync("/api/login", new
        {
            username = "nobody@test.com",
            password = "password123",
            jobID = 1
        });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("ไม่พบบัญชีผู้ใช้งาน", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        // user exists (test@test.com) แต่ password ผิด → FakeUserService คืน null
        var res = await _client.PostAsJsonAsync("/api/login", new
        {
            username = "test@test.com",
            password = "wrongpassword",
            jobID = 1
        });

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains("ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง", body.GetProperty("message").GetString());
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithToken()
    {
        var res = await _client.PostAsJsonAsync("/api/login", new
        {
            username = "test@test.com",
            password = "password123",
            jobID = 1
        });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("token", out var token));
        Assert.False(string.IsNullOrEmpty(token.GetString()));
        Assert.Equal("test@test.com", body.GetProperty("username").GetString());
    }

    [Fact]
    public async Task Login_MissingBody_Returns400()
    {
        var res = await _client.PostAsJsonAsync("/api/login", new { });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
