using Dapper;
using JobOnlineAPI.DAL;
using System.Data;

namespace JobOnlineAPI.Services
{
    public class OtpService(DapperContext context, IEmailService emailService) : IOtpService
    {
        private readonly DapperContext _context = context;
        private readonly IEmailService _emailService = emailService;

        public async Task<(bool Success, string Message)> RequestOtpAsync(string identifier, string emailToSend, string action)
        {
            var otp = GenerateOtp();

            using var conn = _context.CreateConnection();
            var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "sp_RequestOTP",
                new { Identifier = identifier, OTPCode = otp, Action = action },
                commandType: CommandType.StoredProcedure);

            if (result == null || result!.Status != 1)
                return (false, (string)(result?.Message ?? "มี OTP ที่ใช้งานอยู่สำหรับ user นี้"));

            var body = LoadEmailTemplate("RequestOtp.html").Replace("{{otp}}", otp);

            await _emailService.SendEmailAsync(emailToSend, "ONEE Jobs - รหัส OTP สำหรับเข้าสู่ระบบ", body, true, "OTP", null, bypassTestMode: true);

            return (true, "ส่ง OTP ไปที่อีเมลแล้ว");
        }

        public async Task<bool> VerifyOtpAsync(string identifier, string otp, string action)
        {
            using var conn = _context.CreateConnection();
            var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "sp_VerifyAndConsumeOTP",
                new { Identifier = identifier, OTPCode = otp, Action = action },
                commandType: CommandType.StoredProcedure);

            return result != null && result!.IsValid == 1;
        }

        private static string GenerateOtp() =>
            Random.Shared.Next(100000, 999999).ToString();

        private static string LoadEmailTemplate(string templateName)
        {
            var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "Email", templateName);

            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Email template not found: {templatePath}");

            return File.ReadAllText(templatePath);
        }
    }
}
