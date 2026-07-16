using System.Data;
using Dapper;
using JobOnlineAPI.DAL;
using JobOnlineAPI.Models;
using Microsoft.Data.SqlClient;

namespace JobOnlineAPI.Services;

public sealed class ManualTraineeService(
    DapperContext context,
    INetworkShareService networkShareService,
    ILogger<ManualTraineeService> logger) : IManualTraineeService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        { ".pdf", ".png", ".jpg", ".jpeg", ".doc", ".docx" };
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
        { "application/pdf", "image/png", "image/jpeg", "application/msword", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
    private const long MaxFileSize = 40 * 1024 * 1024;

    public async Task<CreateManualTraineeResult> CreateAsync(
        ManualTraineeRequest request,
        IReadOnlyDictionary<string, IReadOnlyList<IFormFile>> files,
        int? adminId,
        CancellationToken cancellationToken)
    {
        ValidateFiles(files);
        await networkShareService.ConnectAsync();
        var createdPaths = new List<string>();
        using var conn = context.CreateConnection();
        if (conn.State != ConnectionState.Open) conn.Open();
        using var transaction = conn.BeginTransaction();

        try
        {
            var result = await conn.QueryFirstAsync<CreateManualTraineeResult>(
                "sp_CreateManualTraineeManagement",
                ToParameters(request, adminId), transaction, commandType: CommandType.StoredProcedure);

            var folder = Path.Combine(networkShareService.GetBasePath(), $"applicant_{result.ApplicantId}");
            Directory.CreateDirectory(folder);
            foreach (var group in files)
            foreach (var file in group.Value.Where(f => f.Length > 0))
            {
                var safeOriginalName = Path.GetFileName(file.FileName);
                var storedName = $"{Guid.NewGuid():N}_{safeOriginalName}";
                var path = Path.Combine(folder, storedName);
                await using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    await file.CopyToAsync(stream, cancellationToken);
                createdPaths.Add(path);

                await conn.ExecuteAsync("usp_ApplicantFile_Insert", new
                {
                    ApplicantID = result.ApplicantId,
                    ApplicationID = result.TraineeApplicationId,
                    FilePath = path.Replace('\\', '/'),
                    FileName = storedName,
                    FileSize = file.Length,
                    FileType = file.ContentType,
                    SectionFile = group.Key
                }, transaction, commandType: CommandType.StoredProcedure);
            }

            transaction.Commit();
            return result;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627 or 51001)
        {
            transaction.Rollback();
            Cleanup(createdPaths);
            throw new ManualTraineeConflictException("อีเมลหรือเลขบัตรประชาชนนี้มีอยู่ในระบบแล้ว");
        }
        catch (Exception ex)
        {
            transaction.Rollback();
            Cleanup(createdPaths);
            logger.LogError(ex, "Manual trainee transaction failed for company {CompanyCode}, department {DepartmentCode}", request.CompanyCode, request.DepartmentCode);
            throw;
        }
        finally
        {
            networkShareService.Disconnect();
        }
    }

    private static void ValidateFiles(IReadOnlyDictionary<string, IReadOnlyList<IFormFile>> groups)
    {
        foreach (var file in groups.Values.SelectMany(x => x))
        {
            if (file.Length > MaxFileSize) throw new ArgumentException($"ไฟล์ {file.FileName} ใหญ่เกิน 40MB");
            var extension = Path.GetExtension(file.FileName);
            if (!AllowedExtensions.Contains(extension) || !AllowedMimeTypes.Contains(file.ContentType))
                throw new ArgumentException($"ไฟล์ {file.FileName} มีประเภทไฟล์ไม่ถูกต้อง");
        }
    }

    private static object ToParameters(ManualTraineeRequest r, int? adminId) => new
    {
        r.CompanyCode, r.DepartmentCode, r.StartDate, r.EndDate, r.DesiredField1, r.DesiredField2, r.DesiredField3,
        r.InternshipType, r.Reason, r.ReasonOther, r.PrefixT, r.NameFirstT, r.NameLastT, r.NicknameT,
        r.PrefixE, r.NameFirstE, r.NameLastE, r.NicknameE, r.Gender, r.DateOfBirth, r.PlaceOfBirth,
        r.Nationality, r.Race, r.Religion, r.Height, r.Weight, r.IDCardNo, r.IDIssuedBy, r.IDExpiredDate,
        r.Address, r.ProvinceID, r.DistrictID, r.SubDistrictID, r.PostalCode, r.Telephone, r.Mobile, r.Email,
        r.FatherName, r.FatherOccupation, r.FatherStatus, r.MotherName, r.MotherOccupation, r.MotherStatus,
        r.SiblingCount, r.SiblingOrder, r.EmergencyName, r.EmergencyRelation, r.EmergencyAddress, r.EmergencyPhone,
        r.School, r.Faculty, r.Major, r.Minor, r.YearOfStudy, r.AdvisorName, r.AdvisorPhone, r.Activities,
        r.InfoSources, r.InfoSourceStaffName, r.InfoSourceDepartment, r.InfoSourceOther, AssignedByAdminID = adminId
    };

    private static void Cleanup(IEnumerable<string> paths)
    {
        foreach (var path in paths)
            try { if (File.Exists(path)) File.Delete(path); } catch { /* original failure is logged/rethrown */ }
    }
}
