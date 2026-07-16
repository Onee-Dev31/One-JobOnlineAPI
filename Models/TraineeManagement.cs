namespace JobOnlineAPI.Models
{
    public class TraineeQuota
    {
        // Null when this department has no TraineeQuota row yet — sp_GetTraineeQuota lists every
        // HRMS department (not just configured ones), so QuotaID/CreatedAt are only set once an
        // admin has actually saved a quota for it.
        public int? QuotaID { get; set; }
        public string CompanyCode { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public int Quota { get; set; }
        public bool IsAcceptingTrainees { get; set; }
        public string? Notes { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? CreatedByAdminID { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public int? ModifiedByAdminID { get; set; }
    }

    public class UpsertTraineeQuotaRequest
    {
        public int Quota { get; set; }
        public bool IsAcceptingTrainees { get; set; } = true;
        public string? DepartmentName { get; set; }
        public string? Notes { get; set; }
        public int? AdminID { get; set; }
    }

    // One row per department in the /Trainee/overview response, HRMS-department LEFT JOINed with
    // its (possibly not-yet-configured) local quota row.
    public class TraineeOverviewDepartment
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string DepartmentCode { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public int Quota { get; set; }
        public bool IsAcceptingTrainees { get; set; }
        public List<TraineeAssignment> Assignments { get; set; } = [];
    }

    public class TraineeAssignment
    {
        public int AssignmentID { get; set; }
        public string? CompanyCode { get; set; }
        public string? DepartmentCode { get; set; }
        public int? ApplicationID { get; set; }
        public int? ApplicantID { get; set; }
        public string? Title { get; set; }
        public string? FirstNameThai { get; set; }
        public string? LastNameThai { get; set; }
        public DateTime? InternStartDate { get; set; }
        public DateTime? InternEndDate { get; set; }
        public string Status { get; set; } = "Assigned";
        public DateTime AssignedDate { get; set; }
    }

    public class TraineeAssignmentFile
    {
        public int FileID { get; set; }
        public int AssignmentID { get; set; }
        public string FilePath { get; set; } = "";
        public string FileName { get; set; } = "";
        public long FileSize { get; set; }
        public string? FileType { get; set; }
        public string? SectionFile { get; set; }
        public DateTime UploadedDate { get; set; }
    }

    public class CreateTraineeAssignmentRequest
    {
        // Set on POST /Trainee/assignments (admin, direct-to-department). Left null by the
        // caller on POST /Trainee/apply (public self-apply, placed into a department later).
        public int? UserID { get; set; }
        public string? CompanyCode { get; set; }
        public string? DepartmentCode { get; set; }
        public int JobID { get; set; }
        // Existing JobApplications row — either still-pending (self-submitted, no department yet)
        // or an already-created manual entry. The SP figures out whether to place it or insert new.
        public int? ApplicationID { get; set; }
        public string? Title { get; set; }
        public string? FirstNameThai { get; set; }
        public string? LastNameThai { get; set; }
        public string? Nickname { get; set; }
        public int? Age { get; set; }
        public string? Year { get; set; }
        public decimal? GPA { get; set; }
        public string? Major { get; set; }
        public string? Faculty { get; set; }
        public string? University { get; set; }
        public string? InternshipType { get; set; }
        public DateTime? InternStartDate { get; set; }
        public DateTime? InternEndDate { get; set; }
        public string? DurationMonths { get; set; }
        public string? PreferredPosition { get; set; }
        public string? PreferredPositionBackup { get; set; }
        public string? MobilePhone { get; set; }
        public string? Email { get; set; }
        public bool? CanCommute { get; set; }
        public bool? CanTravelOutside { get; set; }
        public bool? FlexibleWork { get; set; }
        public string? ReasonForInterest { get; set; }
        // Set true to proceed despite sp_CreateOrPlaceTraineeAssignment reporting the department
        // as over quota for the given date range (soft-warn, not a hard block).
        public bool ForceOverQuota { get; set; }
    }

    public class CreateTraineeAssignmentResult
    {
        public int AssignmentID { get; set; }
        public bool IsOverQuota { get; set; }
        public int ActiveOverlapCount { get; set; }
        public int? Quota { get; set; }
    }

    // Seed data for prefilling a Part2-style continuation form from a TraineeAssignments row.
    public class TraineeAssignmentSeedData
    {
        public int AssignmentID { get; set; }
        public string? PrefixT { get; set; }
        public string? NameFirstT { get; set; }
        public string? NameLastT { get; set; }
        public string? NicknameT { get; set; }
        public string? Mobile { get; set; }
        public string? Email { get; set; }
        public string? YearOfStudy { get; set; }
        public string? Major { get; set; }
        public string? Faculty { get; set; }
        public string? School { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string? DesiredField1 { get; set; }
        public string? DesiredField2 { get; set; }
        public string? InternshipType { get; set; }
        public List<TraineeAssignmentFile> Files { get; set; } = [];
    }

    // ========================================================================================
    // DTOs below match /admin/trainee-management's TypeScript types exactly (property-for-property,
    // camelCase over the wire via the default ASP.NET Core JSON naming policy), so the frontend can
    // swap its mockCompanies array for a fetch() of GET /TraineeManagement/overview with no other
    // changes. See Controllers/TraineeManagementController.cs.
    // ========================================================================================

    public class TraineeManagementTrainee
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Nickname { get; set; }
        public string? University { get; set; }
        public string StartDate { get; set; } = string.Empty; // "YYYY-MM-DD"
        public string EndDate { get; set; } = string.Empty;   // "YYYY-MM-DD"

        // Not part of the frontend Trainee type; used only to bucket rows into their
        // DepartmentGroup while mapping the stored proc's two result sets in the controller.
        [System.Text.Json.Serialization.JsonIgnore]
        public string? CompanyCode { get; set; }
        [System.Text.Json.Serialization.JsonIgnore]
        public string? DepartmentCode { get; set; }
    }

    public class TraineeManagementDepartment
    {
        public string DepartmentCode { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public int Required { get; set; }
        public bool IsOpen { get; set; }
        public List<TraineeManagementTrainee> Trainees { get; set; } = [];

        // Not part of the frontend DepartmentGroup type; used only to group into
        // TraineeManagementCompany while mapping the stored proc's result set in the controller.
        [System.Text.Json.Serialization.JsonIgnore]
        public string CompanyCode { get; set; } = string.Empty;
        [System.Text.Json.Serialization.JsonIgnore]
        public string? CompanyName { get; set; }
    }

    public class TraineeManagementCompany
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public List<TraineeManagementDepartment> Departments { get; set; } = [];
    }

    public class UpdateDepartmentQuotaRequest
    {
        public int Required { get; set; }
        public bool IsOpen { get; set; }
    }

    public class CreateTraineeManagementTraineeRequest
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
        // Existing candidate (T_APPLICANTS.ApplicantID) who already applied to one of the
        // "นักศึกษาฝึกงาน" job postings. Omit and fill FirstName/LastName/Nickname/University
        // instead for a manual (walk-in) entry.
        public int? ApplicantID { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Nickname { get; set; }
        public string? University { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    public class CreateTraineeManagementTraineeResult
    {
        public int Id { get; set; }
        public bool IsOverQuota { get; set; }
        public int ActiveOverlapCount { get; set; }
        public int Quota { get; set; }
    }

    public class ManualTraineeRequest : TraineeApplication
    {
        public string CompanyCode { get; set; } = string.Empty;
        public string DepartmentCode { get; set; } = string.Empty;
    }

    public class CreateManualTraineeResult
    {
        public int Id { get; set; }
        public int TraineeApplicationId { get; set; }
        public int? ApplicantId { get; set; }
        public bool IsOverQuota { get; set; }
        public int ActiveOverlapCount { get; set; }
        public int Quota { get; set; }
    }

    // For the manual-trainee form's optional "attribute to this posting" dropdown — the open
    // "นักศึกษาฝึกงาน" job postings in one department. See sp_GetOpenTraineeJobsByDepartment.
    public class TraineeManagementOpenJob
    {
        public int JobID { get; set; }
        public string JobTitle { get; set; } = string.Empty;
        public int NumberOfPositions { get; set; }
    }
}
