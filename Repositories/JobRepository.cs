using System.Data;
using Dapper;
using JobOnlineAPI.Models;
using JobOnlineAPI.Services;
using Microsoft.Data.SqlClient;
using static Org.BouncyCastle.Math.EC.ECCurve;


namespace JobOnlineAPI.Repositories
{
    

    public class JobRepository(IConfiguration configuration, IEmailService emailService, IEmailNotificationService emailNotificationService) : IJobRepository
    {
        private readonly string _connectionString = configuration?.GetConnectionString("DefaultConnection")
                ?? throw new ArgumentNullException(nameof(configuration), "Connection string 'DefaultConnection' is not found.");
        private readonly IEmailService _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        private readonly IEmailNotificationService _emailNotificationService = emailNotificationService ?? throw new ArgumentNullException(nameof(emailNotificationService));
        private readonly IConfiguration _config = configuration;
        public async Task<IEnumerable<Job>> GetAllJobsAsync()
        {
            using var db = new SqlConnection(_connectionString);
            string sql = "sp_GetAllJobsV2";
            return await db.QueryAsync<Job>(sql, commandType: CommandType.StoredProcedure);
        }

        public async Task<Job> GetJobByIdAsync(int id)
        {
            using var db = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("JobID", id);
            //sp_GetAllJobsV2
            var job = await db.QueryFirstOrDefaultAsync<Job>(
                "sp_GetAllJobsAdmin",
                parameters,
                commandType: CommandType.StoredProcedure
            );
            return job ?? throw new InvalidOperationException($"No job found with ID {id}");
        }
        


        public async Task<int> AddJobAsync(Job job)
        {
            try
            {
                using var db = new SqlConnection(_connectionString);
                await db.OpenAsync();
                string sql = "sp_AddJob";

                var parameters = new
                {
                    job.JobTitle,
                    job.JobDescription,
                    job.Requirements,
                    job.Location,
                    job.ExperienceYears,
                    job.NumberOfPositions,
                    job.Department,
                    job.JobStatus,
                    job.ApprovalStatus,
                    job.OpenFor,
                    ClosingDate = job.ClosingDate.HasValue ? (object)job.ClosingDate.Value : DBNull.Value,
                    CreatedBy = job.CreatedBy.HasValue ? (object)job.CreatedBy.Value : DBNull.Value,
                    job.CreatedByRole,
                    job.JobGroupID,
                    job.Office,
                    job.LevelID,
                    job.EmployeeTypeID
                };

                var result = await db.ExecuteScalarAsync(sql, parameters, commandType: CommandType.StoredProcedure);
                if (result == null || !int.TryParse(result.ToString(), out int id) || id == 0)
                {
                    throw new InvalidOperationException("Failed to retrieve valid JobID after inserting the job.");
                }

                await SendJobNotificationEmailsAsync(job, db);

                return id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error adding job: {ex.Message}");
                throw;
            }
        }

        private async Task SendJobNotificationEmailsAsync(Job job, SqlConnection db)
        {
            var roleSendMail = GetRoleSendMail(job.Role);
            var emailParameters = new DynamicParameters();
            emailParameters.Add("@CreatorRole", job.Role);
            emailParameters.Add("@CreatorDepartment", job.Department);
            emailParameters.Add("@OpenForEmpID",
                string.IsNullOrWhiteSpace(job.OpenFor)
                    ? (object)DBNull.Value
                    : job.OpenFor,
                DbType.String);
            emailParameters.Add("@CreatorUsername", job.CreatedByRole);
            var applicationFormUri = _config["FileStorage:ApplicationFormUri"];
            try
            {
                //"sp_GetDateSendEmailV4"
                var staffList = await db.QueryAsync<StaffEmail>(
                    "sp_GetDataSendEmailJobManagement",
                    emailParameters,
                    commandType: CommandType.StoredProcedure
                );

                // Role "2" = HR created the job themself: only notify the person it's opened for, not the whole staff list.
                if (job.Role == "2")
                {
                    staffList = staffList.Where(s => s.CODEMPID == job.OpenFor).ToList();
                }

                string requesterInfo = job.Role == "1" || job.Role == "2"
                    ? $"<tr><td style='padding:14px 20px;color:#6b7280;'>ฝ่ายทรัพยากรบุคคล</td><td style='padding:14px 20px;font-weight:600;'>{job.NAMETHAI}</td></tr>"
                    : $"<tr><td style='padding:14px 20px;color:#6b7280;'>ผู้ขอ</td><td style='padding:14px 20px;font-weight:600;'>{job.NAMETHAI}</td></tr>";

                // Role "2" = HR created the job themself, so greet the person it's opened for (looked up by CODEMPID == OpenFor)
                // by name instead of the generic HR department line.
                var openForStaff = staffList.FirstOrDefault(s => s.CODEMPID == job.OpenFor);
                string greetingName = job.Role == "2"
                    ? $"คุณ {openForStaff?.NAMETHAI}"
                    : "ฝ่ายทรัพยากรบุคคล";
                string subjectMail = job.Role == "2" ? $"Onee Jobs : เปิดตำแหน่งงาน :{job.JobTitle}" : $"Onee Jobs : คำขอเปิดตำแหน่งงาน :{job.JobTitle}";

                // Role "2" = HR created the job themself: hide the "pending request" subtitle, the หน่วยงาน/เบอร์โทร/Email
                // rows (Job model doesn't carry reliable data for those), and show the job title as its own row instead
                // (right above อัตรา). For other roles, look up the contact info by the OpenFor employee code.
                string headerSubtitle = job.Role == "2"
                    ? ""
                    : @"<p style=""color:#d1d5db;margin-top:8px;font-size:15px;"">มีคำขอเปิดรับสมัครงานตำแหน่งใหม่รอการพิจารณา</p>";

                string contactRows = "";
                if (job.Role != "2")
                {
                    var openForContact = await db.QueryFirstOrDefaultAsync<OpenForContactInfo>(
                        "GetDataOpenForByEmpCode",
                        new { EmpCode = job.OpenFor },
                        commandType: CommandType.StoredProcedure);

                    contactRows = $@"<tr bgcolor='#fafafa'><td style='padding:14px 20px;color:#6b7280;'>หน่วยงาน</td><td style='padding:14px 20px;font-weight:600;'>{openForContact?.DEPARTMENT}</td></tr>
                        <tr><td style='padding:14px 20px;color:#6b7280;'>เบอร์โทร</td><td style='padding:14px 20px;'>{openForContact?.TELOFF}</td></tr>
                        <tr bgcolor='#fafafa'><td style='padding:14px 20px;color:#6b7280;'>Email</td><td style='padding:14px 20px;'>{openForContact?.EMAIL}</td></tr>";
                }

                string jobTitleRow = job.Role == "2"
                    ? $"<tr bgcolor='#fafafa'><td style='padding:14px 20px;color:#6b7280;'>ตำแหน่งงาน</td><td style='padding:14px 20px;font-weight:600;'>{job.JobTitle}</td></tr>"
                    : "";

                int successCount = 0;
                var emailTasks = staffList
                    .Where(s => !string.IsNullOrWhiteSpace(s.Email))
                    .Select(async s =>
                    {
                        string openForInfo = s.CODEMPID == job.OpenFor
                            ? $"<tr bgcolor='#fafafa'><td style='padding:14px 20px;color:#6b7280;'>เปิดให้</td><td style='padding:14px 20px;font-weight:600;'>{s.NAMETHAI} </td></tr>"
                            : "";
                        var hrBody = GenerateNewJobRequestNotificationBody(job.JobTitle, greetingName, headerSubtitle, requesterInfo, contactRows, jobTitleRow, job.NumberOfPositions, openForInfo, applicationFormUri);

                        await _emailService.SendEmailAsync(s.Email!, subjectMail, hrBody, true, "Jobs", null);
                        Interlocked.Increment(ref successCount);
                    });

                await Task.WhenAll(emailTasks);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR executing stored procedure: {ex.Message}");
                throw;
            }
        }

        private static string GetRoleSendMail(string? role) =>
            role switch
            {
                "1" => "<Admin>",
                "2" => "<HR>",
                _ => ""
            };

        private static string GenerateNewJobRequestNotificationBody(
            string jobTitle,
            string greetingName,
            string headerSubtitle,
            string requesterInfo,
            string contactRows,
            string jobTitleRow,
            int numberOfPositions,
            string openForInfo,
            string? applicationFormUri)
        {
            var template = LoadEmailTemplate("NewJobRequestNotification.html");
            return ReplaceTemplatePlaceholders(template, new Dictionary<string, string>
            {
                { "JobTitle", jobTitle },
                { "GreetingName", greetingName },
                { "HeaderSubtitle", headerSubtitle },
                { "RequesterInfo", requesterInfo },
                { "ContactRows", contactRows },
                { "JobTitleRow", jobTitleRow },
                { "Positions", numberOfPositions.ToString() },
                { "OpenForInfo", openForInfo },
                { "BaseUrl", applicationFormUri ?? "" }
            });
        }

        private static string LoadEmailTemplate(string templateName)
        {
            var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", "Email", templateName);

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

        public async Task<int> UpdateJobAsync(Job job)
        {
            using var db = new SqlConnection(_connectionString);
            string sql = "sp_UpdateJob";

            var parameters = new
            {
                job.JobID,
                job.JobTitle,
                job.JobDescription,
                job.Requirements,
                job.Location,
                job.ExperienceYears,
                job.NumberOfPositions,
                job.Department,
                job.JobStatus,
                // job.ApprovalStatus,
                job.OpenFor,
                job.Remark,
                PostedDate = job.PostedDate.HasValue ? (object)job.PostedDate.Value : DBNull.Value,
                ClosingDate = job.ClosingDate.HasValue ? (object)job.ClosingDate.Value : DBNull.Value,
                ModifiedBy = job.ModifiedBy.HasValue ? (object)job.ModifiedBy.Value : DBNull.Value,
                ModifiedDate = job.ModifiedDate.HasValue ? (object)job.ModifiedDate.Value : DBNull.Value,
                job.JobGroupID,
                job.Office,
                job.LevelID,
                job.EmployeeTypeID
            };
            // await SendJobNotificationEmailsAsync(job, db);
            return await db.ExecuteAsync(sql, parameters, commandType: CommandType.StoredProcedure);
        }

        public async Task<int> DeleteJobAsync(int id)
        {
            using var db = new SqlConnection(_connectionString);
            string sql = "DELETE FROM Jobs WHERE JobID = @Id";
            return await db.ExecuteAsync(sql, new { Id = id });
        }
    }

  internal class StaffEmail
    {
        public string? CODEMPID { get; set; }
        public string? NAMETHAI { get; set; }
        public string? NAMECOSTCENT { get; set; }
        public string? Email { get; set; }
    }

    internal class OpenForContactInfo
    {
        public string? CODEMPID { get; set; }
        public string? DEPARTMENT { get; set; }
        public string? TELOFF { get; set; }
        public string? EMAIL { get; set; }
    }

}