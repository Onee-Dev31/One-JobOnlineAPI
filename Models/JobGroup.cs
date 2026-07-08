using System.ComponentModel.DataAnnotations;

namespace JobOnlineAPI.Models
{
    public class JobGroup
    {
        public int? JobGroupID { get; set; }

        [Required(ErrorMessage = "GroupName is required.")]
        public string GroupName { get; set; } = string.Empty;

        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
