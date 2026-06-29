namespace JobOnlineAPI.Services
{
    public interface IOtpService
    {
        Task<(bool Success, string Message)> RequestOtpAsync(string identifier, string emailToSend, string action);
        Task<bool> VerifyOtpAsync(string identifier, string otp, string action);
    }
}
