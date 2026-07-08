using System.ComponentModel.DataAnnotations;

namespace JobOnlineAPI.Models
{
    public class JobSlot
    {
        public int? SlotID { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        public string Department { get; set; } = string.Empty;
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
        public int SlotID { get; set; }
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
        // Either ApplicantID (sourced from T_APPLICANTS) or FirstNameThai (manual entry) must be provided.
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
    }
}
