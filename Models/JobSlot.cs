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
        public string? FirstNameThai { get; set; }
        public string? LastNameThai { get; set; }
        public string Status { get; set; } = "Assigned";
        public DateTime AssignedDate { get; set; }
    }

    public class AssignApplicantRequest
    {
        // Either ApplicantID (sourced from T_APPLICANTS) or FirstNameThai (manual entry) must be provided.
        public int? ApplicantID { get; set; }
        public string? FirstNameThai { get; set; }
        public string? LastNameThai { get; set; }
    }
}
