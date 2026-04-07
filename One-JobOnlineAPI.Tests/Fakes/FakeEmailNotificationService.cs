using JobOnlineAPI.Models;
using JobOnlineAPI.Services;

namespace JobOnlineAPI.Tests.Fakes;

public class FakeEmailNotificationService : IEmailNotificationService
{
    public Task<int> SendHireToHrEmailsAsync(ApplicantRequestData requestData) => Task.FromResult(0);
    public Task<int> SendManagerEmailsAsync(ApplicantRequestData requestData) => Task.FromResult(0);
    public Task<int> SendHrEmailsAsync(ApplicantRequestData requestData) => Task.FromResult(0);
    public Task<int> SendEmailWhenHRReceived(ApplicantRequestData requestData) => Task.FromResult(0);
    public Task<int> SendNotificationEmailsAsync(ApplicantRequestData requestData) => Task.FromResult(0);
    public Task<int> SendApplicationEmailsAsync(
        IDictionary<string, object?> req,
        (int ApplicantId, string ApplicantEmail, string HrManagerEmails, string JobManagerEmails, string JobTitle, string CompanyName, int OutJobID) dbResult,
        string applicationFormUri) => Task.FromResult(0);
    public Task<int> SendEmailsJobsStatusAsync(int JobID) => Task.FromResult(0);
}
