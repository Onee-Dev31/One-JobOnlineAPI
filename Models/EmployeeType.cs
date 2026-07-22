using System.ComponentModel.DataAnnotations;

namespace JobOnlineAPI.Models
{
    public class EmployeeType
    {
        public int? EmployeeTypeID { get; set; }

        [Required(ErrorMessage = "TypeName is required.")]
        public string TypeName { get; set; } = string.Empty;

        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;

        public string? CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
