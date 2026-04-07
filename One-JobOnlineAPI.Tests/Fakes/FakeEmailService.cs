using JobOnlineAPI.Services;

namespace JobOnlineAPI.Tests.Fakes;

public class FakeEmailService : IEmailService
{
    public Task SendEmailAsync(string to, string subject, string body, bool isHtml, string typeMail, int? jobIds)
        => Task.CompletedTask;
}
