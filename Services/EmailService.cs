using Microsoft.Extensions.Options;
using JobOnlineAPI.Models;
using MailKit.Net.Smtp;
using MimeKit;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace JobOnlineAPI.Services
{
    public class EmailService(IConfiguration configuration, IOptions<EmailSettings> emailSettings) : IEmailService
    {
        private readonly EmailSettings _emailSettings = emailSettings.Value;
        private readonly IDbConnection _dbConnection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));

        public Task SendEmailAsync(string to, string subject, string body, bool isHtml, string typeMail, int? JobsId, bool bypassTestMode = false)
            => SendEmailCoreAsync(to, null, subject, body, isHtml, typeMail, JobsId, bypassTestMode);

        public Task SendEmailWithCcAsync(string to, string? cc, string subject, string body, bool isHtml, string typeMail, int? JobsId, bool bypassTestMode = false)
            => SendEmailCoreAsync(to, cc, subject, body, isHtml, typeMail, JobsId, bypassTestMode);

        private async Task SendEmailCoreAsync(string to, string? cc, string subject, string body, bool isHtml, string typeMail, int? JobsId, bool bypassTestMode)
        {
            var (isTestMode, testRecipients) = await GetEmailConfigAsync();
            var redirectToTestMode = isTestMode && !bypassTestMode;

            var recipients = redirectToTestMode ? testRecipients : to.Split([';', ','], StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).ToList();
            var ccRecipients = string.IsNullOrWhiteSpace(cc)
                ? []
                : redirectToTestMode ? testRecipients : cc.Split([';', ','], StringSplitOptions.RemoveEmptyEntries).Select(e => e.Trim()).ToList();
            var finalSubject = redirectToTestMode
                ? (string.IsNullOrWhiteSpace(cc) ? $"[TEST] {subject} To {to}" : $"[TEST] {subject} To {to} Cc {cc}")
                : subject;

            var emailMessage = new MimeMessage();
            emailMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.FromEmail));

            foreach (var address in recipients)
            {
                emailMessage.To.Add(new MailboxAddress("", address));
            }

            foreach (var address in ccRecipients)
            {
                emailMessage.Cc.Add(new MailboxAddress("", address));
            }

            emailMessage.Subject = finalSubject;

            var bodyBuilder = new BodyBuilder
            {
                HtmlBody = isHtml ? body : null,
                TextBody = !isHtml ? body : null
            };

            emailMessage.Body = bodyBuilder.ToMessageBody();

            string status = "Success";
            string? errorMessage = null;

            try
            {
                using var client = new SmtpClient();
                await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, _emailSettings.UseSSL);

                if (!string.IsNullOrEmpty(_emailSettings.SmtpUser) && !string.IsNullOrEmpty(_emailSettings.SmtpPass))
                {
                    await client.AuthenticateAsync(_emailSettings.SmtpUser, _emailSettings.SmtpPass);
                }

                await client.SendAsync(emailMessage);
                await client.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                status = "Failed";
                errorMessage = ex.Message;
                Console.WriteLine($"❌ Error sending email: {errorMessage}");
            }

            await LogEmailAsync(to, finalSubject, body, status, errorMessage, typeMail, JobsId);
        }

        private async Task<(bool isTestMode, List<string> testRecipients)> GetEmailConfigAsync()
        {
            using var connection = new SqlConnection(_dbConnection.ConnectionString);
            using var multi = await connection.QueryMultipleAsync("sp_GetEmailConfig", commandType: CommandType.StoredProcedure);
            var isTestMode = await multi.ReadFirstOrDefaultAsync<bool>();
            var testRecipients = (await multi.ReadAsync<string>()).ToList();
            return (isTestMode, testRecipients);
        }

        private async Task LogEmailAsync(string recipient, string subject, string body, string status, string? errorMessage, string? mailType, int? JobsId)
        {
            using var connection = new SqlConnection(_dbConnection.ConnectionString);
            var parameters = new
            {
                Recipient = recipient,
                Subject = subject,
                Body = body,
                Status = status,
                ErrorMessage = errorMessage,
                MailType = mailType,
                SystemTag = "ONEEJobs",
                JobID = JobsId
            };

            await connection.ExecuteAsync("sp_LogEmailActivity", parameters, commandType: CommandType.StoredProcedure);
        }
    }
}