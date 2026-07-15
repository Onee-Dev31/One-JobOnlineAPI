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
}
