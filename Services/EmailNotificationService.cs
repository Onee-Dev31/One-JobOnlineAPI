using System.Data;
using System.Globalization;
using System.Text.Json;
using Castle.DynamicProxy.Internal;
using Dapper;
using JobOnlineAPI.DAL;
using JobOnlineAPI.Models;
using Microsoft.VisualStudio.TestPlatform.Common;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Client;
using Org.BouncyCastle.Ocsp;

namespace JobOnlineAPI.Services
{
    public interface IEmailNotificationService
    {
        Task<int> SendHireToHrEmailsAsync(ApplicantRequestData requestData);
        Task<int> SendManagerEmailsAsync(ApplicantRequestData requestData);
        Task<int> SendHrEmailsAsync(ApplicantRequestData requestData);
        Task<int> SendEmailWhenHRReceived(ApplicantRequestData requestData);
        Task<int> SendNotificationEmailsAsync(ApplicantRequestData requestData);
        Task<int> SendApplicationEmailsAsync(IDictionary<string, object?> req, (int ApplicantId, string ApplicantEmail, string HrManagerEmails, string JobManagerEmails, string JobTitle, string CompanyName, int OutJobID) dbResult, string applicationFormUri);
        Task<int> SendEmailCandidatePass(ApplicantRequestData requestData);
        Task<int> SendEmailsJobsStatusAsync(int JobID);
        Task<int> SendEmailsTraineeRegisterAsync(int TraineeApplicationID, int JobID);
        Task<int> SendEmailsNotiHrAfterApplyAsync(int ApplicationID, int JobID, bool istrainee);
        Task<int> SendEmailTraineepart2(int ApplicantID, int ApplicationID, int? JobID, int? AssignmentID);
    }

    public class EmailNotificationService(
        IEmailService emailService,
        DapperContext context,
        ILogger<EmailNotificationService> logger, IConfiguration config) : IEmailNotificationService
    {
        private readonly IEmailService _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        private readonly DapperContext _context = context ?? throw new ArgumentNullException(nameof(context));
        private readonly ILogger<EmailNotificationService> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IConfiguration _config = config;
        private async Task<IEnumerable<string>> GetEmailRecipientsAsync(int? role = null, int? jobId = null)
        {
            using var connection = _context.CreateConnection();
            var parameters = new DynamicParameters();

            if (role.HasValue)
                parameters.Add("@Role", role.Value);

            if (jobId.HasValue)
                parameters.Add("@JobID", jobId.Value);

            var staffList = await connection.QueryAsync<StaffEmail>(
                "sp_GetDataSendEmailRecipients",
                parameters,
                commandType: CommandType.StoredProcedure);
            return staffList
                .Select(staff => staff.Email?.Trim() ?? string.Empty)
                .Where(email => !string.IsNullOrWhiteSpace(email));
        }


        public async Task<int> SendApplicationEmailsAsync(IDictionary<string, object?> req, (int ApplicantId, string ApplicantEmail, string HrManagerEmails, string JobManagerEmails, string JobTitle, string CompanyName, int OutJobID) dbResult, string applicationFormUri)
        {
            var fullNameThai = GetFullName(req);
            var jobTitle = req.TryGetValue("JobTitle", out var jobTitleObj) ? jobTitleObj?.ToString() ?? "-" : "-";
            // เรียกใช้
            var JobStartDate = req.TryGetValue("JobStartDate", out var JobStartDateObj)
                ? FormatThaiDate(JobStartDateObj!)
                : "-";

            var CodeMPID = req.TryGetValue("CodeMPID", out var CodeMPIDObj) ? CodeMPIDObj?.ToString() ?? "-" : "-";
            var typeMail = req.TryGetValue("TypeMail", out var typeMailObj) && typeMailObj != null
                ? typeMailObj is JsonElement t && t.ValueKind == JsonValueKind.String ? t.GetString() : typeMailObj.ToString()
                : null;

            int successCount = 0;
            if (!string.IsNullOrWhiteSpace(typeMail) && typeMail == "HrEdit")
            {
                _logger.LogInformation("Skip sending email because typeMail is HrEdit");
                return 0;
            }
            //"sp_GetDateSendEmailV3"
            using var connection = _context.CreateConnection();
            var results = await connection.QueryAsync<StaffEmail>(
                "sp_GetDataSendEmailRegister",
                new { JobID = dbResult.OutJobID },
                commandType: CommandType.StoredProcedure);

            var parameters = new DynamicParameters();
            parameters.Add("@JobID", dbResult.OutJobID);
            //sp_GetDateSendEmail 
            var resultsNew = await connection.QueryAsync<StaffEmailNew>(
                "EXEC sp_GetDataSendEmailByJobID_V2  @JobID",
                parameters);

            var resultsWithOpenFor = await connection.QueryAsync<StaffEmail>(
                "sp_GetDataSendEmailRegisterWithOpenFor",
                new { JobID = dbResult.OutJobID },
                commandType: CommandType.StoredProcedure);

            var firstHr = results.FirstOrDefault(x => x.Role == 2 && !string.IsNullOrEmpty(x.NAMETHAI));
            var OpenForName = resultsWithOpenFor.FirstOrDefault(x => x.SourceType == "OpenFor" && !string.IsNullOrEmpty(x.NAMETHAI));

            if (!string.IsNullOrWhiteSpace(typeMail) && typeMail == "Applicant")
            {
                string managerBody = GenerateManagerEmailBody(fullNameThai, jobTitle);
                foreach (var staff in results.Where(s => s.Role != 2))
                {
                    var emailStaff = staff.Email?.Trim();
                    if (string.IsNullOrWhiteSpace(emailStaff))
                        continue;

                    try
                    {
                        await _emailService.SendEmailAsync(emailStaff, "ONEE Jobs - You've got the new candidate update information", managerBody, true, "Register", null);
                        successCount++;
                        _logger.LogInformation("Successfully sent email to {Email}", emailStaff);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send email to {Email}: {Message}", emailStaff, ex.Message);
                    }
                }
            }
            else if (!string.IsNullOrWhiteSpace(typeMail) && typeMail == "HRConfirmed")
            {
                var openForStaff = resultsWithOpenFor.FirstOrDefault(staff => staff.SourceType == "OpenFor");
                string fullName;
                string? toEmail;

                var candidateData = await connection.QueryFirstOrDefaultAsync<dynamic>(
                    "GetDataCandidateByJobID",
                new { JobID = dbResult.OutJobID, ApplicantID = dbResult.ApplicantId },
                commandType: CommandType.StoredProcedure);
                string companyName = candidateData?.companyName?.ToString() ?? string.Empty;
                string departmentName = candidateData?.departmentName?.ToString() ?? string.Empty;

                if (openForStaff != null)
                {
                    fullName = $"{openForStaff.NAMETHAI}".Trim();
                    toEmail = openForStaff.Email?.Trim();
                }
                else
                {
                    var createStaff = resultsNew.FirstOrDefault(staff => staff.DATATYPE == "Create");
                    fullName = createStaff != null
                        ? $"{createStaff.NAMFIRSTT} {createStaff.NAMLASTT}".Trim()
                        : "ไม่พบข้อมูลผู้สมัคร";
                    toEmail = createStaff?.Email?.Trim();
                }

                // อีเมลกลุ่มทั้งหมดจาก GetEmailGroup ให้แนบไปเป็น CC แทนที่จะส่งแยกทีละฉบับ
                var ccgroup = await connection.QueryAsync<EmailGroup>(
                    "GetEmailGroup",
                    commandType: CommandType.StoredProcedure);

                var ccEmails = ccgroup
                    .Select(group => group.Email?.Trim())
                    .Where(email => !string.IsNullOrWhiteSpace(email) && !string.Equals(email, toEmail, StringComparison.OrdinalIgnoreCase))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (!string.IsNullOrWhiteSpace(toEmail))
                {
                    try
                    {
                        string managerBody = GenerateRegistrationConfirmedToHRBody(fullName, fullNameThai, CodeMPID, JobStartDate, applicationFormUri, companyName, departmentName);
                        string SubjectMail = $@"แจ้งผลการสรรหา - คุณ {fullNameThai}";
                        string? ccEmail = ccEmails.Count != 0 ? string.Join(";", ccEmails) : null;
                        await _emailService.SendEmailWithCcAsync(toEmail, ccEmail, SubjectMail, managerBody, true, "Register", null);
                        successCount++;
                        _logger.LogInformation("Successfully sent email to {Email} (cc: {Cc})", toEmail, ccEmail);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send email to {Email}: {Message}", toEmail, ex.Message);
                    }
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(dbResult.ApplicantEmail))
                {
                    string applicantBody = typeMail == "Part2"
                    ? GenerateApplicantPart2EmailBody(fullNameThai, jobTitle, dbResult.CompanyName)
                    : GenerateEmailBody(true, dbResult.CompanyName, fullNameThai, jobTitle, dbResult.ApplicantId, applicationFormUri);
                    string applicantSubject = typeMail == "Part2"
                    ? $"ยืนยันการได้รับข้อมูลประวัติประกอบการทำสัญญาจ้างงาน ตำแหน่ง - {(string.IsNullOrWhiteSpace(jobTitle) ? "-" : jobTitle)} - {(string.IsNullOrWhiteSpace(fullNameThai) ? "-" : fullNameThai)} "
                    : $"ยืนยันการได้รับข้อมูลประวัติประกอบการสมัครงาน - {(string.IsNullOrWhiteSpace(jobTitle) ? "-" : jobTitle)}";
                    try
                    {
                        await _emailService.SendEmailAsync(dbResult.ApplicantEmail, applicantSubject, applicantBody, true, "Register", null, true);
                        successCount++;
                        _logger.LogInformation("Successfully sent email to {Email}", dbResult.ApplicantEmail);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send email to {Email}: {Message}", dbResult.ApplicantEmail, ex.Message);
                    }
                }

                if (typeMail == "Part2") {
                    foreach (var staff in resultsWithOpenFor)
                    {
                        var emailStaff = staff.Email?.Trim();
                        if (string.IsNullOrWhiteSpace(emailStaff))
                            continue;

                        //string managerBody = GenerateEmailBody(false, emailStaff, fullNameThai, jobTitle, null, dbResult.ApplicantId, applicationFormUri);
                        string managerBody = typeMail == "Part2"
                       ? GenerateApplicantPart2ToHREmailBody(fullNameThai, jobTitle, staff!.NAMETHAI!)
                       : await GenerateApplicantPart1ToHREmailBody(dbResult.ApplicantId, jobTitle, _config, context, dbResult.OutJobID, OpenForName!.NAMETHAI!);
                        //: GenerateEmailBody(true, dbResult.CompanyName, fullNameThai, jobTitle, firstHr, dbResult.ApplicantId, applicationFormUri);
                        string applicantSubject = typeMail == "Part2"
                        ? $"Onee Jobs - ผู้สมัครงาน ตำแหน่ง {(string.IsNullOrWhiteSpace(jobTitle) ? "-" : jobTitle)} ได้กรอกข้อมูลประวัติประกอบการทำสัญญาจ้างงานเรียบร้อยแล้ว"
                        : $"Onee Jobs - มีผู้สมัครงานเข้ามาใหม่  {(string.IsNullOrWhiteSpace(jobTitle) ? "-" : jobTitle)}";
                        try
                        {
                            await _emailService.SendEmailAsync(emailStaff, applicantSubject, managerBody, true, "Register", null);
                            successCount++;
                            _logger.LogInformation("Successfully sent email to {Email}", emailStaff);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to send email to {Email}: {Message}", emailStaff, ex.Message);
                        }
                    }
                }
            }

            return successCount;
        }

        public async Task<int> SendEmailCandidatePass(ApplicantRequestData requestData)
        {
            int successCount = 0;

            using var connection = _context.CreateConnection();
            var results = await connection.QueryAsync<StaffEmail>(
                "sp_GetDataSendEmailRegister",
                new { requestData.JobID },
                commandType: CommandType.StoredProcedure);

            var parameters = new DynamicParameters();
            parameters.Add("@JobID", requestData.JobID);
            var resultsNew = await connection.QueryAsync<StaffEmailNew>(
                "EXEC sp_GetDataSendEmailByJobID_V2  @JobID",
                parameters);

            var resultsWithOpenFor = await connection.QueryAsync<StaffEmail>(
                "sp_GetDataSendEmailRegisterWithOpenFor",
                new { requestData.JobID },
                commandType: CommandType.StoredProcedure);

            var OpenForName = resultsWithOpenFor.FirstOrDefault(x => x.SourceType == "OpenFor" && !string.IsNullOrEmpty(x.NAMETHAI));

            var candidateData = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "GetDataCandidateByJobID",
                new { requestData.JobID, requestData.ApplicantID },
                commandType: CommandType.StoredProcedure);

            string fullNameThai = candidateData?.fullNameThai?.ToString() ?? string.Empty;
            string jobTitle = candidateData?.jobTitle?.ToString() ?? string.Empty;
            string applicantEmail = candidateData?.applicantEmail?.ToString() ?? string.Empty;
            string companyName = candidateData?.companyName?.ToString() ?? string.Empty;
            string applicationFormUri = _config["FileStorage:ApplicationFormUri"] ?? string.Empty;


            foreach (var staff in resultsWithOpenFor.Where(s => s.SourceType == "OpenFor"))
            {
                var emailStaff = staff.Email?.Trim();
                if (string.IsNullOrWhiteSpace(emailStaff))
                    continue;

                string managerBody = await GenerateApplicantPart1ToHREmailBody(requestData.ApplicantID, jobTitle, _config, _context, requestData.JobID, OpenForName!.NAMETHAI!);
                string applicantSubject = $"Onee Jobs - มีผู้สมัครงานเข้ามาใหม่ - {(string.IsNullOrWhiteSpace(jobTitle) ? "-" : jobTitle)}";
                try
                {
                    await _emailService.SendEmailAsync(emailStaff, applicantSubject, managerBody, true, "Register", null);
                    successCount++;
                    _logger.LogInformation("Successfully sent email to {Email}", emailStaff);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email to {Email}: {Message}", emailStaff, ex.Message);
                }
            }

            return successCount;
        }

        public async Task<int> SendEmailsTraineeRegisterAsync(int TraineeApplicationID, int JobID)
        {
            int successCount = 0;
            using var connection = _context.CreateConnection();

            var candidateData = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "GetTraineeDataByAppJobForSendMail_V2",
                new { TraineeApplicationID, JobID },
                commandType: CommandType.StoredProcedure);

            string fullNameThai = candidateData?.fullNameThai?.ToString() ?? string.Empty;
            string jobTitle = candidateData?.jobTitle?.ToString() ?? string.Empty;
            string applicantEmail = candidateData?.applicantEmail?.ToString() ?? string.Empty;
            string companyName = candidateData?.companyName?.ToString() ?? string.Empty;
            string applicationFormUri = _config["FileStorage:ApplicationFormUri"] ?? string.Empty;

            if (!string.IsNullOrEmpty(applicantEmail))
            {
                string applicantBody = GenerateEmailBody(true, companyName, fullNameThai, jobTitle, TraineeApplicationID, applicationFormUri);
                string applicantSubject = $"ยืนยันการได้รับข้อมูลประวัติประกอบการสมัครฝึกงาน - {(string.IsNullOrWhiteSpace(jobTitle) ? "-" : jobTitle)}";
                try
                {
                    await _emailService.SendEmailAsync(applicantEmail, applicantSubject, applicantBody, true, "Register", null, bypassTestMode: true);
                    _logger.LogInformation("Successfully sent email to {Email}", applicantEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send email to {Email}: {Message}", applicantEmail, ex.Message);
                }
            }
            return successCount;
        }

        public async Task<int> SendEmailTraineepart2(int ApplicantID, int ApplicationID, int? JobID, int? AssignmentID)
        {
            int successCount = 0;
            using var connection = _context.CreateConnection();

            var candidateData = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "GetTraineeDataByAppJobForSendMail_V2",
                new { TraineeApplicationID = AssignmentID, JobID },
                commandType: CommandType.StoredProcedure);

            string fullNameThai = candidateData?.fullNameThai?.ToString() ?? string.Empty;
            string jobTitle = candidateData?.jobTitle?.ToString() ?? string.Empty;
            string applicantEmail = candidateData?.applicantEmail?.ToString() ?? string.Empty;
            string companyName = candidateData?.companyName?.ToString() ?? string.Empty;

            // 1) แจ้งผู้สมัครฝึกงาน — ใช้ template/เนื้อหาเดียวกับ ApplicantPart2 (candidate)
            if (!string.IsNullOrEmpty(applicantEmail))
            {
                string applicantBody = GenerateTraineePart2EmailBody(fullNameThai, jobTitle, companyName);
                string applicantSubject = $"ยืนยันการได้รับข้อมูลประวัติประกอบการเข้าฝึกงาน ตำแหน่ง - {(string.IsNullOrWhiteSpace(jobTitle) ? "-" : jobTitle)} - {(string.IsNullOrWhiteSpace(fullNameThai) ? "-" : fullNameThai)} ";

                try
                {
                    await _emailService.SendEmailAsync(applicantEmail, applicantSubject, applicantBody, true, "Register", null, true);
                    successCount++;
                    _logger.LogInformation("Successfully sent trainee part2 email to {Email}", applicantEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send trainee part2 email to {Email}: {Message}", applicantEmail, ex.Message);
                }
            }
            else
            {
                _logger.LogWarning("SendEmailTraineepart2: applicant email not found (ApplicantID={ApplicantID}, ApplicationID={ApplicationID})", ApplicantID, ApplicationID);
            }

            // 2) แจ้ง HR (OpenFor staff) — เนื้อหาเป็นของ trainee โดยเฉพาะ (ไม่ใช่ wording ทำสัญญาจ้างงานของ candidate)
            if (JobID.HasValue)
            {
                var resultsWithOpenFor = await connection.QueryAsync<StaffEmail>(
                    "sp_GetDataSendEmailRegisterWithOpenFor",
                    new { JobID = JobID.Value },
                    commandType: CommandType.StoredProcedure);

                foreach (var staff in resultsWithOpenFor.Where(s => s.SourceType == "OpenFor"))
                {
                    var emailStaff = staff.Email?.Trim();
                    if (string.IsNullOrWhiteSpace(emailStaff))
                        continue;

                    string hrBody = GenerateTraineePart2ToHREmailBody(fullNameThai, jobTitle, staff.NAMETHAI ?? "");
                    string hrSubject = $"Onee Jobs - ผู้สมัครฝึกงาน ตำแหน่ง {(string.IsNullOrWhiteSpace(jobTitle) ? "-" : jobTitle)} ได้กรอกข้อมูลประวัติประกอบการเข้าฝึกงานเรียบร้อยแล้ว";

                    try
                    {
                        await _emailService.SendEmailAsync(emailStaff, hrSubject, hrBody, true, "Register", null);
                        successCount++;
                        _logger.LogInformation("Successfully sent trainee part2 HR notice to {Email}", emailStaff);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send trainee part2 HR notice to {Email}: {Message}", emailStaff, ex.Message);
                    }
                }
            }
            else
            {
                _logger.LogWarning("SendEmailTraineepart2: JobID not provided, skipping HR notice (ApplicantID={ApplicantID}, ApplicationID={ApplicationID})", ApplicantID, ApplicationID);
            }

            return successCount;
        }

        public async Task<int> SendEmailsNotiHrAfterApplyAsync(int ApplicationID, int JobID, bool istrainee)
        {
            int successCount = 0;

            try
            {
                using var connection = _context.CreateConnection();

                dynamic? candidateData;
                try
                {
                    candidateData = istrainee
                        ? await connection.QueryFirstOrDefaultAsync<dynamic>(
                            "GetTraineeDataByAppJobForSendMail_V2",
                            new { TraineeApplicationID = ApplicationID, JobID },
                            commandType: CommandType.StoredProcedure)
                        : await connection.QueryFirstOrDefaultAsync<dynamic>(
                            "GetDataCandidateByJobID",
                            new { JobID, ApplicantID = ApplicationID },
                            commandType: CommandType.StoredProcedure);

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "STEP 1 FAILED: Query candidateData (istrainee={IsTrainee}, AppID={AppID}, JobID={JobID})", istrainee, ApplicationID, JobID);
                    return successCount;
                }

                string fullNameThai = candidateData?.fullNameThai?.ToString() ?? string.Empty;
                string jobTitle = candidateData?.jobTitle?.ToString() ?? string.Empty;
                string applicantEmail = candidateData?.applicantEmail?.ToString() ?? string.Empty;
                string companyName = candidateData?.companyName?.ToString() ?? string.Empty;
                string companyCode = candidateData?.companyCode?.ToString() ?? string.Empty;
                string departmentName = candidateData?.departmentName?.ToString() ?? string.Empty;
                string createDate = candidateData?.createdate?.ToString() ?? string.Empty;
                string mobliephone = candidateData?.MobilePhone?.ToString() ?? string.Empty;
                string applicationFormUri = _config["FileStorage:ApplicationFormUri"] ?? string.Empty;

                _logger.LogInformation("Parsed candidateData: fullNameThai={FullName}, jobTitle={JobTitle}, applicantEmail={Email}, companyCode={CompanyCode}",
                    fullNameThai, jobTitle, applicantEmail, companyCode);

                IEnumerable<dynamic> hrEmailList;
                try
                {
                    hrEmailList = await connection.QueryAsync<dynamic>(
                        "GetEmailHRByCompanyCode",
                        new { companyCode },
                        commandType: CommandType.StoredProcedure);

                    _logger.LogInformation("hrEmailList query success. Count: {Count}", hrEmailList?.Count() ?? 0);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "STEP 2 FAILED: Query hrEmailList (companyCode={CompanyCode})", companyCode);
                    return successCount;
                }

                string hrSubject = $"แจ้งเตือน: มีผู้สมัครใหม่รอการคัดกรอง - {(string.IsNullOrWhiteSpace(jobTitle) ? "-" : jobTitle)}";

                string hrBody;
                try
                {
                    hrBody = await GenerateEmailsNotiHrAfterApplyBody(_config, jobTitle, fullNameThai, applicantEmail, mobliephone, createDate, departmentName, companyName);
                    _logger.LogInformation("hrBody generated. Length: {Length}", hrBody?.Length ?? 0);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "STEP 3 FAILED: GenerateEmailsNotiHrAfterApplyBody");
                    return successCount;
                }

                if (hrEmailList == null || !hrEmailList.Any())
                {
                    _logger.LogWarning("hrEmailList is empty. No HR emails to send. companyCode={CompanyCode}", companyCode);
                    return successCount;
                }

                foreach (var hr in hrEmailList)
                {
                    string hrEmail = hr?.Email?.ToString() ?? string.Empty;
                    if (string.IsNullOrEmpty(hrEmail))
                    {
                        _logger.LogWarning("Skipped HR row with empty Email.");
                        continue;
                    }

                    try
                    {
                        await _emailService.SendEmailAsync(hrEmail, hrSubject, hrBody, true, "Register", null);
                        _logger.LogInformation("Successfully sent HR notification email to {Email}", hrEmail);
                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send HR notification email to {Email}: {Message}", hrEmail, ex.Message);
                    }
                }

                return successCount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "UNEXPECTED FAILURE in SendEmailsNotiHrAfterApplyAsync (AppID={AppID}, JobID={JobID}, istrainee={IsTrainee})", ApplicationID, JobID, istrainee);
                return successCount;
            }
        }

        public async Task<int> SendHireToHrEmailsAsync(ApplicantRequestData requestData)
        {
            var candidateNames = requestData.Candidates?
                .Select((candidate, index) =>
                {
                    var remarkText = !string.IsNullOrWhiteSpace(candidate.Remark)
                        ? $" <span style='color: #555;'>(หมายเหตุ: {candidate.Remark})</span>"
                        : "";
                    return $"ลำดับที่ {index + 1}: {candidate.Title} {candidate.FirstNameThai} {candidate.LastNameThai}{remarkText}".Trim();
                })
                .ToList() ?? [];
            var applicationFormUri = _config["FileStorage:ApplicationFormUri"];
            string candidateNamesString = string.Join("<br>", candidateNames);
            var tel = "";
            var recipientNames = "";
            var recipientDep = "";
            string? requesterMail = requestData.RequesterMail;
            if (requestData.Role == 2)
            {
                using var connection = _context.CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@JobID", requestData.JobID);
                var result = await connection.QueryAsync<dynamic>(
                    "sp_GetDataSendMailJobs @JobID",
                    parameters);
                var resultList = result.ToList();
                var firstRecord = resultList.Count == 2
                    ? resultList.FirstOrDefault(r => (string?)r?.RoleType == "Head") ?? resultList.FirstOrDefault()
                    : resultList.FirstOrDefault();

                recipientNames = firstRecord?.ApproveNameThai;
                requesterMail = ((string?)firstRecord?.EMAIL)?.Trim();
                recipientDep = firstRecord?.NAMECOSTCENT;
                tel = firstRecord?.Tel;
            }

            string? requesterName = requestData.Role == 2 ? recipientNames : requestData.RequesterName;
            string? requesterDep = requestData.Role == 2 ? recipientDep : requestData.NameCon;
            string hrBody = GenerateNegotiateCandidateToHRBody(requesterDep, requesterName, tel, requesterMail, requestData.JobTitle, candidateNamesString, applicationFormUri);
            var subjectEmail = $"Onee Jobs - เจรจาต่อรองผู้สมัคร ตำแหน่ง {requestData.JobTitle}";
            var recipients = await GetEmailRecipientsAsync(2);
            return await SendEmailsAsync(recipients, subjectEmail, hrBody, null);
        }

        public async Task<int> SendManagerEmailsAsync(ApplicantRequestData requestData)
        {
            var candidateNames = requestData.Candidates?
                .Where(candidate =>
                    candidate.Status is "Nagotiate Success" or "Nagotiate Failed" or "Nagotiate Cancel" or "Waiting HR Re-check" or "Reject")
                .Select((candidate, index) =>
                {
                    string statusText = candidate.Status switch
                    {
                        "Nagotiate Success" => "<b style='color: green;'>สำเร็จ</b>",
                        "Nagotiate Failed" => "<b style='color: red;'>ต่อรองไม่สำเร็จ</b>",
                        "Nagotiate Cancel" => "<b style='color: red;'>ยกเลิก</b>",
                        "Waiting HR Re-check" => "<b style='color: #1677ff;'>กำลังดำเนินการ</b>", // nz primary
                        "Reject" => "<b style='color: red;'>ยกเลิก</b>",
                        _ => ""
                    };

                    string remarkText = (candidate.Status == "Nagotiate Failed" && !string.IsNullOrWhiteSpace(candidate.Remark))
                        ? $" <span style='color: red;'>(หมายเหตุ: {candidate.Remark})</span>"
                        : "";

                    return $"ลำดับที่ {index + 1}: {candidate.Title} {candidate.FirstNameThai} {candidate.LastNameThai} <strong>สถานะ {statusText}{remarkText}</strong> ".Trim();
                }).ToList() ?? [];

            string candidateNamesString = string.Join("<br>", candidateNames);
            using var connection = _context.CreateConnection();
            var parameters = new DynamicParameters();
            var DepartmentName = requestData?.DeptName;
            var JobTitle = requestData?.JobTitle;
            var applicationFormUri = _config["FileStorage:ApplicationFormUri"];
            int jobId = requestData?.JobID ?? 0;
            parameters.Add("@JobID", jobId, DbType.Int32);
            // ตัวอย่าง Dapper async
            var result = await connection.QueryAsync<dynamic>(
                "sp_GetDataSendEmailByJobID",
                parameters,
                commandType: CommandType.StoredProcedure);

            var hasOpenFor = result.Any(r => (string?)r?.DATATYPE == "Openfor");

            var emails = result
                .Where(r =>
                    (string?)r?.DATATYPE == "DiHr" ||
                    (string?)r?.DATATYPE == "Openfor" ||
                    (!hasOpenFor && (string?)r?.DATATYPE == "Create")
                )
                .Select(r => ((string?)r?.EMAIL)?.Trim())
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var SentToName = result.FirstOrDefault(x => x.DATATYPE == "Openfor")
                  ?? result.FirstOrDefault(x => x.DATATYPE == "Create");
            string recipientName = $"{SentToName?.NAMFIRSTT} {SentToName?.NAMLASTT}".Trim();
            bool hasSuccess = requestData.Candidates?.Any(candidate => candidate.Status == "Nagotiate Success") ?? false;
            string hrBody = GenerateManagerNegotiationResultBody(recipientName, requestData!.JobTitle!, candidateNamesString, hasSuccess);
            var subjectEmail = $"Onee Jobs - ผลเจรจาต่อรองผู้สมัคร ตำแหน่ง {requestData!.JobTitle}";
            // var recipients = await GetEmailRecipientsAsync(null,requestData.JobID);
            var recipients = (await GetEmailRecipientsAsync(null, requestData.JobID))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
            return await SendEmailsAsync(emails!, subjectEmail, hrBody, null);
        }

        public async Task<int> SendHrEmailsAsync(ApplicantRequestData requestData)
        {
            // var candidateNames = requestData.Candidates?
            //     .Select(candidate => $"{candidate.Title} {candidate.FirstNameThai} {candidate.LastNameThai}".Trim())
            //     .ToList() ?? [];

            // string candidateNamesString = string.Join(" ", candidateNames);

            var candidateNames = requestData.Candidates?
                .Select((candidate, index) =>
                {
                    var name = $"{index + 1}. คุณ {candidate.FirstNameThai} {candidate.LastNameThai}".Trim();
                    return string.IsNullOrWhiteSpace(candidate.Remark)
                        ? name
                        : $"{name}<br>&nbsp;&nbsp;&nbsp;<span style='color:#555;'>หมายเหตุ: {candidate.Remark}</span>";
                })
                .ToList() ?? [];

            string candidateNamesString = string.Join("<br>", candidateNames);

            using var connection = _context.CreateConnection();
            var parameters = new DynamicParameters();
            var DepartmentName = requestData?.DeptName;
            var JobTitle = requestData?.JobTitle;
            var applicationFormUri = _config["FileStorage:ApplicationFormUri"];
            int jobId = requestData?.JobID ?? 0;
            parameters.Add("@JobID", jobId, DbType.Int32);
            // ตัวอย่าง Dapper async
            var result = await connection.QueryAsync<dynamic>(
                "sp_GetDataSendEmailByJobID",
                parameters,
                commandType: CommandType.StoredProcedure);

            var hasOpenFor = result.Any(r => (string?)r?.DATATYPE == "Openfor");

            var emails = result
                .Where(r =>
                    (string?)r?.DATATYPE == "DiHr" ||
                    (string?)r?.DATATYPE == "Openfor" ||
                    (!hasOpenFor && (string?)r?.DATATYPE == "Create")
                )
                .Select(r => ((string?)r?.EMAIL)?.Trim())
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var SentToName = result.FirstOrDefault(x => x.DATATYPE == "Openfor") 
                  ?? result.FirstOrDefault(x => x.DATATYPE == "Create");

            string hrBody = GenerateInterviewCallToHRBody(
                requestData!.JobTitle!,
                candidateNamesString,
                $"{SentToName!.NAMFIRSTT} {SentToName.NAMLASTT}".Trim(),
                (string?)SentToName.POST ?? "",
                (string?)SentToName.TELOFF ?? "",
                (string?)SentToName.EMAIL ?? "",
                (string?)SentToName.NAMECOSTCENT ?? "",
                (string?)SentToName.COMPANY_NAME ?? "");
            var subjectEmail = $"Onee Jobs เรียกผู้สมัครสัมภาษณ์งาน - ตำแหน่ง {requestData!.JobTitle}";
            var recipients = await GetEmailRecipientsAsync(2);
            return await SendEmailsAsync(recipients, subjectEmail, hrBody, null);
        }

        public async Task<int> SendNotificationEmailsAsync(ApplicantRequestData requestData)
        {
            try
            {
                var candidateNames = requestData.Candidates?
                    .Select(candidate => $"{candidate.FirstNameThai} {candidate.LastNameThai}".Trim())
                    // .Select(candidate => $"{candidate.Title} {candidate.FirstNameThai} {candidate.LastNameThai}".Trim())
                    .ToList() ?? [];

                var candidateEmails = requestData.Candidates?
                    .Select(candidate => candidate.Email ?? "")
                    .ToList() ?? [];

                var jobIds = new List<int?> { requestData?.JobID };

                // var candidateApplicantIDs = requestData.Candidates?
                //     .Select(candidate => candidate.ApplicantID ?? "")
                //     .ToList() ?? [];
                var candidateApplicantIDs = requestData?.Candidates?
                .Select(candidate => candidate.ApplicantID)
                .ToList() ?? [];

                string candidateName = candidateNames.FirstOrDefault() ?? "ผู้สมัคร";
                // string candidateApplicantID = candidateApplicantIDs.FirstOrDefault() ?? "0";

                // string candidateApplicantID = candidateApplicantIDs.Count != 0
                // ? candidateApplicantIDs.First().ToString()
                // : "0";
                int candidateApplicantID = candidateApplicantIDs.Count != 0
                    ? candidateApplicantIDs.First()
                    : 0;
                int jobId = requestData?.JobID ?? 0;
                int applicationId = requestData?.ApplicationID ?? 0;

                using var connection = _context.CreateConnection();
                var url = new DynamicParameters();
                url.Add("@ApplicantID", candidateApplicantID, DbType.Int32);
                url.Add("@JobID", jobId, DbType.Int32);
                url.Add("@ApplicationID", applicationId, DbType.Int32);
                var urllist = await connection.QueryAsync<dynamic>(
                    "GetDataForEmailNotiSelectCandidate",
                    url,
                    commandType: CommandType.StoredProcedure);

                var urlResult = urllist.FirstOrDefault();
                string fromRegis = BuildCandidateLoginUrl(urlResult);

                string reqBody = GenerateWaittingCandidateInFoEmailBody(candidateName, requestData?.JobTitle!, fromRegis);
                //    $@"
                //<div style='font-family: Arial, sans-serif; background-color: #f4f4f4; padding: 20px; font-size: 14px;'>
                //    <p style='font-weight: bold; margin: 0 0 10px 0;'>เรียน คุณ{candidateName}</p>
                //    <p style='font-weight: bold; margin: 0 0 10px 0;'>เรื่อง: ผลสัมภาษณ์ผู้สมัครตำแหน่ง {requestData?.JobTitle}</p>
                //    <br>
                //    <p style='margin: 0 0 10px 0;'>
                //        ตามที่ท่านได้สมัครในตำแหน่ง <strong>{requestData?.JobTitle}</strong> ทางบริษัทได้พิจารณาให้คุณผ่านการคัดเลือก กรุณาเข้าไปกรอกรายละเอียดของท่าน ตามลิงก์ด้านล่าง
                //    </p>
                //    <p style='margin: 0 0 10px 0;'>
                //        Click : <a href='{fromRegis}'>{fromRegis}</a>
                //    </p>
                //    <br>
                //    <p style='color: red; font-weight: bold;'>**อีเมลนี้เป็นข้อความอัตโนมัติ กรุณาอย่าตอบกลับ**</p>
                //</div>";

                // return await SendEmailsAsync(candidateEmails, "ONEE Jobs - แจ้งผลการสัมภาษณ์งาน", reqBody, jobIds.FirstOrDefault());
                var result = await SendEmailsAsync(candidateEmails,
                    "ONEE Jobs - แจ้งผลการสัมภาษณ์งาน",
                    reqBody,
                    jobIds.FirstOrDefault(),
                    bypassTestMode: true
                );

                await InsertEmailSendQueueAsync( connection, requestData!, reqBody, "ONEE Jobs - แจ้งผลการสัมภาษณ์งาน" );
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing GetDataForEmailNotiSelectCandidate with");
                throw;
            }
        }

        private const string LoginUrlUnavailable = "ลิงก์ไม่พร้อมใช้งาน";

        private string BuildCandidateLoginUrl(dynamic? urlResult)
        {
            if (urlResult == null)
                return LoginUrlUnavailable;

            bool isTrainee = urlResult.IsTrainee == true;

            if (isTrainee)
            {
                var template = _config["EmailNotiUrls:TraineeLoginUrl"];
                var assignmentId = urlResult.AssignmentID;
                if (string.IsNullOrWhiteSpace(template) || assignmentId == null)
                    return LoginUrlUnavailable;

                return string.Format(template, assignmentId);
            }

            var candidateTemplate = _config["EmailNotiUrls:CandidateLoginUrl"];
            var jobId = urlResult.JobID;
            string? companyName = urlResult.CompanyName?.ToString();
            if (string.IsNullOrWhiteSpace(candidateTemplate) || jobId == null)
                return LoginUrlUnavailable;

            return string.Format(candidateTemplate, jobId, companyName);
        }

       private async Task InsertEmailSendQueueAsync( IDbConnection connection, ApplicantRequestData requestData, string emailBody, string emailSubject)
        {
            if (requestData?.Candidates == null || !requestData.Candidates.Any())
                return;

            foreach (var c in requestData.Candidates)
            {
                try
                {
                    var p = new DynamicParameters();
                    p.Add("@ApplicationID", c.ApplicationID);
                    p.Add("@ApplicantID", c.ApplicantID);
                    p.Add("@JobID", c.JobID);
                    p.Add("@EmailBody", emailBody);
                    p.Add("@EmailSubject", emailSubject);
                    p.Add("@RecipientEmail", c.Email);

                    await connection.ExecuteAsync(
                        "sp_InsertEmailSendQueue",
                        p,
                        commandType: CommandType.StoredProcedure
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Error inserting EmailSendQueue for ApplicationID {ApplicationID}, ApplicantID {ApplicantID}, JobID {JobID}. Error: {Message}",
                        c.ApplicationID, c.ApplicantID, c.JobID, ex.Message);
                }
            }
        }



        private async Task<int> SendEmailsAsync(IEnumerable<string> recipients, string subject, string body, int? jobIds, bool bypassTestMode = false)
        {
            var recipientList = recipients
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .ToList();

            if (recipientList.Count == 0) return 0;

            try
            {
                string to = string.Join(";", recipientList);

                await _emailService.SendEmailAsync(to, subject, body, true, "Register", jobIds, bypassTestMode);

                _logger.LogInformation("Successfully sent email to {Count} recipients", recipientList.Count);
                return recipientList.Count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to multiple recipients: {Message}", ex.Message);
                return 0;
            }
        }

        private static string GenerateManagerEmailBody(string fullNameThai, string jobTitle)
        {
            var template = LoadEmailTemplate("ManagerPart2Notice.html");
            return ReplaceTemplatePlaceholders(template, new Dictionary<string, string>
            {
                { "FullNameThai", fullNameThai },
                { "JobTitle", jobTitle }
            });
        }

        private static string GenerateRegistrationConfirmedToHRBody(string recipientName, string fullNameThai, string codeMPID, string jobStartDate, string? applicationFormUri, string companyname, string departmentname)
        {
            var template = LoadEmailTemplate("RegistrationConfirmedToHR.html");
            return ReplaceTemplatePlaceholders(template, new Dictionary<string, string>
            {
                { "RecipientName", recipientName },
                { "FullNameThai", fullNameThai },
                { "CodeMPID", codeMPID },
                { "JobStartDate", jobStartDate },
                { "CompanyName", companyname },
                { "DepartmentName", departmentname },
                { "BaseUrl", applicationFormUri ?? "" }
            });
        }

        private static string GenerateNegotiateCandidateToHRBody(string? deptName, string? requesterName, string tel, string? requesterMail, string? jobTitle, string candidateList, string? applicationFormUri)
        {
            var template = LoadEmailTemplate("NegotiateCandidateToHR.html");
            return ReplaceTemplatePlaceholders(template, new Dictionary<string, string>
            {
                { "DeptName", deptName ?? "" },
                { "RequesterName", requesterName ?? "" },
                { "Tel", tel },
                { "RequesterMail", requesterMail ?? "" },
                { "JobTitle", jobTitle ?? "" },
                { "CandidateList", candidateList },
                { "BaseUrl", applicationFormUri ?? "" }
            });
        }

        private static string GenerateManagerNegotiationResultBody(string recipientName, string jobTitle, string candidateList, bool hasSuccess)
        {
            var template = LoadEmailTemplate("ManagerNegotiationResult.html");
            const string successNotice = @"
                    <tr>
                        <td style=""padding:0 35px 35px;"">
                            <div style=""background:#f9fafb;border-left:4px solid #111827;padding:18px 20px;border-radius:8px;"">
                                <p style=""margin:0;color:#4b5563;font-size:14px;line-height:24px;"">
                                    สำหรับผู้สมัครที่ต่อรองสำเร็จ ทางฝ่ายฯ จะดำเนินการตามกระบวนการถัดไป
                                    เพื่อรับผู้สมัครเข้าเป็นพนักงานและกำหนดวันที่เริ่มงานต่อไป
                                </p>
                            </div>
                        </td>
                    </tr>";

            return ReplaceTemplatePlaceholders(template, new Dictionary<string, string>
            {
                { "RecipientName", recipientName },
                { "JobTitle", jobTitle },
                { "CandidateList", candidateList },
                { "SuccessNotice", hasSuccess ? successNotice : "" }
            });
        }

        private static string GenerateInterviewCallToHRBody(string jobTitle, string candidateList, string contactName, string contactPosition, string contactPhone, string contactEmail, string department, string company)
        {
            var template = LoadEmailTemplate("InterviewCallToHR.html");
            return ReplaceTemplatePlaceholders(template, new Dictionary<string, string>
            {
                { "JobTitle", jobTitle },
                { "CandidateList", candidateList },
                { "ContactName", contactName },
                { "ContactPosition", contactPosition },
                { "ContactPhone", contactPhone },
                { "ContactEmail", contactEmail },
                { "Department", department },
                { "Company", company }
            });
        }

        private static string GenerateHRRequestReceivedBody(string jobtile, string recipientName, string? applicationFormUri)
        {
            var template = LoadEmailTemplate("HRRequestReceived.html");
            return ReplaceTemplatePlaceholders(template, new Dictionary<string, string>
            {
                { "Jobtile", jobtile},
                { "RecipientName", recipientName },
                { "BaseUrl", applicationFormUri ?? "" }
            });
        }

        private static string GenerateJobStatusApprovalBody(string recipientNames, string jobTitle, string statusBlock)
        {
            var template = LoadEmailTemplate("JobStatusApproval.html");
            return ReplaceTemplatePlaceholders(template, new Dictionary<string, string>
            {
                { "RecipientNames", recipientNames },
                { "JobTitle", jobTitle },
                { "StatusBlock", statusBlock }
            });
        }

        private static string GenerateEmailBody(bool isApplicant, string recipient, string fullNameThai, string jobTitle, int applicantId, string applicationFormUri)
        {
            if (isApplicant)
            {
                string companyName = recipient;
                var applicantTemplate = LoadEmailTemplate("ApplicantReceivedConfirmation.html");
                return ReplaceTemplatePlaceholders(applicantTemplate, new Dictionary<string, string>
                {
                    { "CompanyName", companyName },
                    { "FullNameThai", fullNameThai },
                    { "JobTitle", jobTitle }
                });
            }

            var staffTemplate = LoadEmailTemplate("StaffNewApplicantNotice.html");
            return ReplaceTemplatePlaceholders(staffTemplate, new Dictionary<string, string>
            {
                { "FullNameThai", fullNameThai },
                { "JobTitle", jobTitle },
                { "Link", $"{applicationFormUri}?id={applicantId}" }
            });
        }

        private static string GetFullName(IDictionary<string, object?> req)
        {
            req.TryGetValue("FirstNameThai", out var firstNameObj);
            req.TryGetValue("LastNameThai", out var lastNameObj);
            return $"{firstNameObj?.ToString() ?? ""} {lastNameObj?.ToString() ?? ""}".Trim();
        }

        public async Task<int> SendEmailsJobsStatusAsync(int JobID)
        {
            using var connection = _context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@JobID", JobID);
            var result = await connection.QueryAsync<dynamic>(
                "sp_GetDataSendMailJobs @JobID",
                parameters);
            var emails = result
                .Select(r => ((string?)r?.EMAIL)?.Trim())
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            var firstRecord = result.FirstOrDefault();

            string recipientNames = string.IsNullOrEmpty(firstRecord?.ApproveNameThai)
                ? $"คุณ{firstRecord?.NAMETHAI}"
                : $"คุณ{firstRecord?.NAMETHAI} และ คุณ{firstRecord?.ApproveNameThai}";

            string statusBlock = firstRecord?.ApprovalStatus == "Approved"
                ? $@"<p style='margin:0;'>ฝ่ายทรัพยากรบุคคลได้พิจารณา <strong style='color:#16a34a;'>อนุมัติ</strong></p>
                    <p style='margin:0;'> คำขอเปิดรับสมัครงานในตำแหน่ง <strong>{firstRecord?.JobTitle}</strong> เรียบร้อยแล้ว</p>"
                : $@"<p style='margin:0 0 10px;'>ฝ่ายทรัพยากรบุคคลพิจารณา <strong style='color:#dc2626;'>ไม่อนุมัติ</strong> คำขอเปิดรับสมัครงานในตำแหน่ง <strong>{firstRecord?.JobTitle}</strong> ด้วยเหตุผลดังต่อไปนี้ค่ะ:</p>
                    <blockquote style='background-color:#fff3f3;padding:10px;border-left:4px solid #ff4d4f;margin:0 0 10px;'><strong>{firstRecord?.Remark}</strong></blockquote>
                    <p style='margin:0;'>หากต้องการข้อมูลเพิ่มเติม กรุณาติดต่อฝ่ายทรัพยากรบุคคลโดยตรงค่ะ</p>";

            string hrBody = GenerateJobStatusApprovalBody(recipientNames, (string?)firstRecord?.JobTitle ?? "", statusBlock);
            string SubjectMail = $@"Onee Jobs : ผลการพิจารณาการขอเปิดตำแหน่งงาน : {firstRecord?.JobTitle}";
            return await SendEmailsAsync(emails!, SubjectMail, hrBody, null);
        }

        public async Task<int> SendEmailWhenHRReceived(ApplicantRequestData requestData)
        {
            using var connection = _context.CreateConnection();
            var parameters = new DynamicParameters();
            var DepartmentName = requestData?.DeptName;
            var JobTitle = requestData?.JobTitle;
            var applicationFormUri = _config["FrontEnd:BaseUrl"];
            int jobId = requestData?.JobID ?? 0;
            parameters.Add("@JobID", jobId, DbType.Int32);
            // ตัวอย่าง Dapper async
            var result = await connection.QueryAsync<dynamic>(
                "sp_GetDataSendEmailByJobID",
                parameters,
                commandType: CommandType.StoredProcedure);

            var hasOpenFor = result.Any(r => (string?)r?.DATATYPE == "Openfor");

            var emails = result
                .Where(r =>
                    (string?)r?.DATATYPE == "DiHr" ||
                    (string?)r?.DATATYPE == "Openfor" ||
                    (!hasOpenFor && (string?)r?.DATATYPE == "Create")
                )
                .Select(r => ((string?)r?.EMAIL)?.Trim())
                .Where(email => !string.IsNullOrWhiteSpace(email))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var SentToName = result.FirstOrDefault(x => x.DATATYPE == "Openfor") 
                  ?? result.FirstOrDefault(x => x.DATATYPE == "Create");


            string recipientName = $"{SentToName?.NAMFIRSTT} {SentToName?.NAMLASTT}".Trim();
            string hrBody = GenerateHRRequestReceivedBody(requestData!.JobTitle!, recipientName, applicationFormUri);
            string SubjectMail = $@"Onee Jobs - รับเรื่องการเจราจาต่อรองผู้สมัคร ตำแหน่ง {JobTitle}";

            return await SendEmailsAsync(emails!, SubjectMail, hrBody, null);
        }

        private static string GenerateApplicantPart2EmailBody(string fullNameThai, string jobTitle, string companyname)
        {
            var template = LoadEmailTemplate("ApplicantPart2.html");

            return ReplaceTemplatePlaceholders(template, new Dictionary<string, string>
            {
                { "FullNameThai", fullNameThai },
                { "JobTitle", jobTitle },
                { "CompanyName", companyname }
            });
        }
        private static string GenerateApplicantPart2ToHREmailBody(string fullNameThai, string jobTitle, string staffName)
        {
            var template = LoadEmailTemplate("ApplicantPart2ToHR.html");

            return ReplaceTemplatePlaceholders(template, new Dictionary<string, string>
            {
                { "FullNameThai", fullNameThai },
                { "JobTitle", jobTitle },
                { "StaffName", staffName }
            });
        }
        private static string GenerateTraineePart2EmailBody(string fullNameThai, string jobTitle, string companyname)
        {
            var template = LoadEmailTemplate("TraineePart2.html");

            return ReplaceTemplatePlaceholders(template, new Dictionary<string, string>
            {
                { "FullNameThai", fullNameThai },
                { "JobTitle", jobTitle },
                { "CompanyName", companyname }
            });
        }
        private static string GenerateTraineePart2ToHREmailBody(string fullNameThai, string jobTitle, string staffName)
        {
            var template = LoadEmailTemplate("TraineePart2ToHR.html");

            return ReplaceTemplatePlaceholders(template, new Dictionary<string, string>
            {
                { "FullNameThai", fullNameThai },
                { "JobTitle", jobTitle },
                { "StaffName", staffName }
            });
        }
        private static async Task<string> GenerateApplicantPart1ToHREmailBody(
            int applicantId,
            string jobTitle,
            IConfiguration config,
            DapperContext context,
            int JobId,
            string managerName)
        {
            var baseUrl = config["UrlFile:BaseUrl"] ?? "";
            var baseUrlFront = config["FrontEnd:BaseUrl"] ?? "";
            var template = LoadEmailTemplate("ApplicantPart1ToHR.html");

            using var connection = context.CreateConnection();

            var data = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "GetCandidateInfoToHr",
                new { ApplicantId = applicantId, JobID = JobId }, 
                // หรือ JobID = applicantId แล้วแต่ SP รับอะไร
                commandType: CommandType.StoredProcedure
            );

            if (data == null)
                throw new Exception("ไม่พบข้อมูลสำหรับสร้างอีเมล HR");

            string firstName = data.FirstNameThai?.ToString() ?? "";
            string lastName = data.LastNameThai?.ToString() ?? "";
            string fullNameThai = $"{firstName} {lastName}".Trim();

            string mobile = data.MobilePhone?.ToString() ?? "";
            string email = data.Email?.ToString() ?? "";
            var culture = new CultureInfo("th-TH");

            string createDate = data.CreatedDate != null
                ? Convert.ToDateTime(data.CreatedDate).ToString("d MMMM yyyy", culture)
                : "";
            string filePathsRaw = data.FilePaths?.ToString() ?? "";
            var filePaths = filePathsRaw
            .Split('|', StringSplitOptions.RemoveEmptyEntries);

            string fileHtml = string.Join("<br/>", filePaths.Select(path =>
            {
                var cleanPath = path.Replace("\\", "/");
                var fileName = Path.GetFileName(path);
                return $"<a href='{baseUrl}/{cleanPath}' target='_blank'>📄 {fileName}</a>";
            }));


            return ReplaceTemplatePlaceholders(template, new Dictionary<string, string>
            {
                { "ManagerName", managerName },
                { "FullNameThai", fullNameThai },
                { "JobTitle", jobTitle },
                { "MobilePhone", mobile },
                { "Email", email },
                { "CreateDate", createDate },
                { "FileList", fileHtml },
                { "BaseUrl", baseUrlFront }
            });
        }

        private static async Task<string> GenerateEmailsNotiHrAfterApplyBody(
            IConfiguration config,
            string jobTitle,
            string fullNameThai,
            string applicantEmail,
            string mobliephone,
            string createDate,
            string departmentName,
            string companyName
        )
        {
            var baseUrl = config["UrlFile:BaseUrl"] ?? "";
            var baseUrlFront = config["FrontEnd:BaseUrl"] ?? "";
            var template = LoadEmailTemplate("ToHROnlyFormApply.html");

            var culture = new CultureInfo("th-TH");
            string CreateDate = createDate != null
                ? Convert.ToDateTime(createDate).ToString("d MMMM yyyy", culture)
                : "";


            return ReplaceTemplatePlaceholders(template, new Dictionary<string, string>
            {
                { "FullNameThai", fullNameThai },
                { "JobTitle", jobTitle },
                { "MobilePhone", mobliephone },
                { "Email", applicantEmail },
                { "CreateDate", CreateDate },
                { "Department", departmentName },
                { "CompanyName", companyName },
                { "BaseUrl", baseUrlFront }
            });
        }
        private static string GenerateWaittingCandidateInFoEmailBody(string fullNameThai, string jobTitle, string url)
        {
            var template = LoadEmailTemplate("WaittingCandidateInfo.html");

            return ReplaceTemplatePlaceholders(template, new Dictionary<string, string>
            {
                { "FullNameThai", fullNameThai },
                { "JobTitle", jobTitle },
                { "Url", url}
            });
        }
        private static string LoadEmailTemplate(string templateName)
        {
            var templatePath = Path.Combine(
                AppContext.BaseDirectory,
                "Templates",
                "Email",
                templateName
            );

            if (!File.Exists(templatePath))
                throw new FileNotFoundException($"Email template not found: {templatePath}");

            return File.ReadAllText(templatePath);
        }

        private static string ReplaceTemplatePlaceholders(string template, Dictionary<string, string> values)
        {
            foreach (var item in values)
            {
                template = template.Replace($"{{{{{item.Key}}}}}", item.Value ?? string.Empty);
            }

            return template;
        }

        static readonly string[] ThaiMonthsFull = {
            "", "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน",
            "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม"
        };

        static string FormatThaiDate(object value)
        {
            if (value == null) return "-";

            var text = value.ToString();
            if (!DateTime.TryParse(text, System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.None, out var dt))
                return "-";

            int buddhistYear = dt.Year + 543;
            return $"{dt.Day:D2} {ThaiMonthsFull[dt.Month]} {buddhistYear}";
        }

    }
}

