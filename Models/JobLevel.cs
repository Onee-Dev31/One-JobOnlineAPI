using System.ComponentModel.DataAnnotations;

namespace JobOnlineAPI.Models
{
    public class JobLevel
    {
        public int? JobLevelID { get; set; }

        [Required(ErrorMessage = "LevelName is required.")]
        public string LevelName { get; set; } = string.Empty;

        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
