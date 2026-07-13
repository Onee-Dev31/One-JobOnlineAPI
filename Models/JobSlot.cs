using System.ComponentModel.DataAnnotations;

namespace JobOnlineAPI.Models
{
    public class JobSlot
    {
        public int? SlotID { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        public string Department { get; set; } = string.Empty;
        public string DepartmentName { get; set; } = string.Empty;
        public string CompanyCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "NumberOfPositions is required.")]
        public int? NumberOfPositions { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string Status { get; set; } = "Open";

        public int AssignedCount { get; set; }

        public int? CreatedByAdminID { get; set; }
        public string? RequestedByName { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public int? ModifiedByAdminID { get; set; }
    }

    public class JobSlotAssignment
    {
        public int AssignmentID { get; set; }
        public int? SlotID { get; set; }
        public int? JobID { get; set; }
        public int? ApplicantID { get; set; }
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
        public int? UserID { get; set; }
        public string Status { get; set; } = "Assigned";
        public DateTime AssignedDate { get; set; }
        public List<JobSlotAssignmentFile> Files { get; set; } = [];
    }

    public class JobSlotAssignmentFile
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

    public class AssignApplicantRequest
    {
        // Either ApplicationID (an existing, already-vetted JobApplications row) or FirstNameThai
        // (manual entry) must be provided.
        public int JobID { get; set; }
        // Existing JobApplications row — whether self-submitted (still awaiting a batch, from
        // sp_GetUnassignedTraineeAssignments) or an already-created manual entry. The SP figures out
        // whether to update its pending JobSlotAssignments row or create a new one.
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
    }

    // Seed data for prefilling Part2 (TraineeApplication) from a Part1 JobSlotAssignments row.
    // Column aliases in sp_GetJobSlotAssignmentSeedData match these property names directly.
    public class JobSlotAssignmentSeedData
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
        public List<JobSlotAssignmentFile> Files { get; set; } = [];
    }
}
