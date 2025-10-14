using Dapper;
using JobOnlineAPI.DAL;
using JobOnlineAPI.Models;
using System.Data;
using System.Diagnostics;

namespace JobOnlineAPI.Services
{
    public class EmailQueueProcessorService(ILogger<EmailQueueProcessorService> logger, IServiceProvider serviceProvider, IEmailNotificationService emailNotificationService) : BackgroundService
    {
        private readonly ILogger<EmailQueueProcessorService> _logger = logger;
        private readonly IServiceProvider _serviceProvider = serviceProvider;
        private readonly IEmailNotificationService _emailNotificationService = emailNotificationService;
        private Timer? _timer;
        private int _intervalMinutes = 5;  // Default fallback

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Query interval จาก DB ด้วย SP ก่อน set Timer
            await LoadIntervalFromDatabaseAsync();

            if (_intervalMinutes <= 0)
            {
                _logger.LogWarning("Invalid interval from DB, using default 5 minutes");
                _intervalMinutes = 5;
            }

            _timer = new Timer(ProcessQueue, null, TimeSpan.Zero, TimeSpan.FromMinutes(_intervalMinutes));

            _logger.LogInformation("EmailQueueProcessor started with interval: {IntervalMinutes} minutes", _intervalMinutes);
        }

        private async Task LoadIntervalFromDatabaseAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DapperContext>();

            using var connection = context.CreateConnection();
            var parameters = new DynamicParameters();
            parameters.Add("@SchedulerName", "EmailQueueProcessor");

            var result = await connection.QuerySingleOrDefaultAsync<int?>(
                "sp_GetSchedulerInterval",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            if (result.HasValue)
            {
                _intervalMinutes = result.Value;
            }
        }

        private async void ProcessQueue(object? state)
        {
            var stopwatch = Stopwatch.StartNew();  // เริ่มวัดเวลา
            string status = "Success";
            string? errorMessage = null;
            int processedCount = 0;
            int sentCount = 0;
            int failedCount = 0;

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<DapperContext>();

            try
            {
                using var connection = context.CreateConnection();
                var parameters = new DynamicParameters();
                parameters.Add("@MaxBatchSize", 10);
                parameters.Add("@ProcessedCount", dbType: DbType.Int32, direction: ParameterDirection.Output);

                // Query queue ที่ ready (SP return list)
                var queueItems = await connection.QueryAsync<EmailQueueItem>(
                    "sp_ProcessEmailQueue",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                processedCount = parameters.Get<int>("@ProcessedCount");

                foreach (var item in queueItems)
                {
                    try
                    {
                        // สร้าง CandidateDto จาก item
                        var candidate = new CandidateDto
                        {
                            ApplicantID = item.ApplicantID,
                            JobID = item.JobID,
                            Email = item.RecipientEmail,
                            Title = item.Title ?? "",
                            FirstNameThai = item.FirstNameThai ?? "",
                            LastNameThai = item.LastNameThai ?? "",
                            Status = "Success"
                        };

                        // สร้าง ApplicantRequestData สำหรับ single candidate
                        var requestData = new ApplicantRequestData
                        {
                            JobID = item.JobID,
                            JobTitle = item.JobTitle ?? "",
                            Candidates = [candidate]
                            // Fields อื่น ๆ ไม่จำเป็นสำหรับ SendNotificationEmailsAsync
                        };

                        // เรียก SendNotificationEmailsAsync เพื่อส่งเมล (ใช้ logic เดิม)
                        var sentThisTime = await _emailNotificationService.SendNotificationEmailsAsync(requestData);
                        sentCount += sentThisTime;

                        // Update status เป็น Sent ด้วย SP (หลังส่งสำเร็จ)
                        var sentParams = new DynamicParameters();
                        sentParams.Add("@QueueID", item.QueueID);
                        sentParams.Add("@SentDate", DateTime.UtcNow);

                        await connection.ExecuteAsync(
                            "sp_UpdateEmailQueueSent",
                            sentParams,
                            commandType: CommandType.StoredProcedure
                        );

                        _logger.LogInformation("Sent notification email for QueueID {QueueID} to {RecipientEmail} (sent {SentThisTime})", item.QueueID, item.RecipientEmail, sentThisTime);
                    }
                    catch (Exception ex)
                    {
                        // Handle retry หรือ failed
                        var retryLimit = 3;
                        if (item.RetryCount < retryLimit)
                        {
                            // Retry ด้วย SP
                            var retryParams = new DynamicParameters();
                            retryParams.Add("@QueueID", item.QueueID);
                            retryParams.Add("@ScheduledSendTime", DateTime.UtcNow.AddHours(1));
                            retryParams.Add("@RetryCount", item.RetryCount + 1);
                            retryParams.Add("@ErrorMessage", ex.Message);

                            await connection.ExecuteAsync(
                                "sp_UpdateEmailQueueRetry",
                                retryParams,
                                commandType: CommandType.StoredProcedure
                            );

                            _logger.LogWarning(ex, "Retry email for QueueID {QueueID} (attempt {RetryCount})", item.QueueID, item.RetryCount + 1);
                        }
                        else
                        {
                            // Mark failed ด้วย SP
                            var failedParams = new DynamicParameters();
                            failedParams.Add("@QueueID", item.QueueID);
                            failedParams.Add("@ErrorMessage", ex.Message);

                            await connection.ExecuteAsync(
                                "sp_UpdateEmailQueueFailed",
                                failedParams,
                                commandType: CommandType.StoredProcedure
                            );

                            failedCount++;
                            _logger.LogError(ex, "Failed email for QueueID {QueueID} after {RetryCount} attempts", item.QueueID, item.RetryCount);
                        }
                    }
                }

                _logger.LogInformation("Email queue processed: {SentCount} sent, {FailedCount} failed out of {TotalCount}", sentCount, failedCount, processedCount);
            }
            catch (Exception ex)
            {
                status = "Error";
                errorMessage = ex.Message;
                _logger.LogError(ex, "Error processing email queue");
            }
            finally
            {
                stopwatch.Stop();
                int durationSeconds = (int)stopwatch.Elapsed.TotalSeconds;

                // Insert log ด้วย SP
                try
                {
                    using var logConnection = context.CreateConnection();
                    var logParams = new DynamicParameters();
                    logParams.Add("@SchedulerName", "EmailQueueProcessor");
                    logParams.Add("@ProcessedCount", processedCount);
                    logParams.Add("@SentCount", sentCount);
                    logParams.Add("@FailedCount", failedCount);
                    logParams.Add("@Status", status);
                    logParams.Add("@ErrorMessage", errorMessage);
                    logParams.Add("@DurationSeconds", durationSeconds);

                    await logConnection.ExecuteAsync(
                        "sp_InsertSchedulerRunLog",
                        logParams,
                        commandType: CommandType.StoredProcedure
                    );
                }
                catch (Exception logEx)
                {
                    _logger.LogError(logEx, "Failed to insert scheduler run log");
                }
            }
        }

        public override void Dispose()
        {
            _timer?.Dispose();
            GC.SuppressFinalize(this);
            base.Dispose();
        }
    }

    public class EmailQueueItem
    {
        public int QueueID { get; set; }
        public int ApplicationID { get; set; }
        public int ApplicantID { get; set; }
        public int JobID { get; set; }
        public string EmailType { get; set; } = "";
        public string RecipientEmail { get; set; } = "";
        public string? EmailBody { get; set; }
        public string? EmailSubject { get; set; }
        public int RetryCount { get; set; }
        public string? Title { get; set; }
        public string? FirstNameThai { get; set; }
        public string? LastNameThai { get; set; }
        public string? JobTitle { get; set; }
    }
}