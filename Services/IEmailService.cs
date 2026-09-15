namespace JobOnlineAPI.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body, bool isHtml, string typeMail, int? jobIds, bool bypassTestMode = false);
        Task SendEmailWithCcAsync(string to, string? cc, string subject, string body, bool isHtml, string typeMail, int? jobIds, bool bypassTestMode = false);
    }
}
