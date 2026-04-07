using System.Net;
using System.Net.Http.Json;
using JobOnlineAPI.Tests.Helpers;
using Xunit;

namespace JobOnlineAPI.Tests.Controllers;

public class AuthControllerTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthControllerTests(TestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ==================== POST /api/auth/request-otp ====================

    [Fact(DisplayName = "request-otp: email ว่าง → 400")]
    public async Task RequestOtp_Returns400_WhenEmailEmpty()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/request-otp",
            new { email = "", action = "REGISTER" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact(DisplayName = "request-otp: action ว่าง → 400")]
    public async Task RequestOtp_Returns400_WhenActionEmpty()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/request-otp",
            new { email = "user@test.com", action = "" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact(DisplayName = "request-otp: รูปแบบ email ผิด → 400")]
    public async Task RequestOtp_Returns400_WhenEmailFormatInvalid()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/request-otp",
            new { email = "notanemail", action = "REGISTER" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact(DisplayName = "request-otp: action ไม่ใช่ REGISTER/RESET → 400")]
    public async Task RequestOtp_Returns400_WhenActionInvalid()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/request-otp",
            new { email = "user@test.com", action = "INVALID" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ==================== POST /api/auth/verify-otp ====================

    [Fact(DisplayName = "verify-otp: email ว่าง → 400")]
    public async Task VerifyOtp_Returns400_WhenEmailEmpty()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/verify-otp",
            new { email = "", otp = "123456" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact(DisplayName = "verify-otp: OTP ว่าง → 400")]
    public async Task VerifyOtp_Returns400_WhenOtpEmpty()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/verify-otp",
            new { email = "user@test.com", otp = "" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ==================== POST /api/auth/register ====================

    [Fact(DisplayName = "register: email ว่าง → 400")]
    public async Task Register_Returns400_WhenEmailEmpty()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/register",
            new { email = "", password = "pass123" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact(DisplayName = "register: รูปแบบ email ผิด → 400")]
    public async Task Register_Returns400_WhenEmailFormatInvalid()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/register",
            new { email = "bademail", password = "pass123" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact(DisplayName = "register: password ว่าง → 400")]
    public async Task Register_Returns400_WhenPasswordEmpty()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/register",
            new { email = "user@test.com", password = "" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ==================== POST /api/auth/reset-password ====================

    [Fact(DisplayName = "reset-password: email ว่าง → 400")]
    public async Task ResetPassword_Returns400_WhenEmailEmpty()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new { email = "", password = "newpass" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact(DisplayName = "reset-password: รูปแบบ email ผิด → 400")]
    public async Task ResetPassword_Returns400_WhenEmailFormatInvalid()
    {
        var res = await _client.PostAsJsonAsync("/api/auth/reset-password",
            new { email = "bademail", password = "newpass" });
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
