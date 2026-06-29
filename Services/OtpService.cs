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

            if (result == null || result.Status != 1)
                return (false, (string)(result?.Message ?? "มี OTP ที่ใช้งานอยู่สำหรับ user นี้"));

            var body = $@"
                <div style='font-family:Arial,sans-serif;padding:20px;'>
                    <p>รหัส OTP ของคุณคือ</p>
                    <h2 style='letter-spacing:8px;color:#2E86C1;'>{otp}</h2>
                    <p style='color:#999;'>หมดอายุใน <strong>5 นาที</strong> และใช้ได้เพียงครั้งเดียว</p>
                    <p style='color:red;font-weight:bold;'>*อีเมลนี้คือข้อความอัตโนมัติ กรุณาอย่าตอบกลับ*</p>
                </div>";

            await _emailService.SendEmailAsync(emailToSend, "ONEE Jobs - รหัส OTP สำหรับเข้าสู่ระบบ", body, true, "OTP", null);

            return (true, "ส่ง OTP ไปที่อีเมลแล้ว");
        }

        public async Task<bool> VerifyOtpAsync(string identifier, string otp, string action)
        {
            using var conn = _context.CreateConnection();
            var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "sp_VerifyAndConsumeOTP",
                new { Identifier = identifier, OTPCode = otp, Action = action },
                commandType: CommandType.StoredProcedure);

            return result != null && result.IsValid == 1;
        }

        private static string GenerateOtp() =>
            Random.Shared.Next(100000, 999999).ToString();
    }
}
