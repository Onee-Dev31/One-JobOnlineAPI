using System.ComponentModel.DataAnnotations;

namespace JobOnlineAPI.Models
{
    public class JobSlot
    {
        public int? SlotID { get; set; }

        [Required(ErrorMessage = "Department is required.")]
        public string Department { get; set; } = string.Empty;

        [Required(ErrorMessage = "SlotNumber is required.")]
        public int? SlotNumber { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public string Status { get; set; } = "Open";

        public int? AssignedApplicantID { get; set; }
        public DateTime? AssignedDate { get; set; }

        public int? CreatedByAdminID { get; set; }
        public string? RequestedByName { get; set; }

        public DateTime? CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public int? ModifiedByAdminID { get; set; }
    }

    public class AssignApplicantRequest
    {
        [Required(ErrorMessage = "ApplicantID is required.")]
        public int ApplicantID { get; set; }
    }
}
