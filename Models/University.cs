using System.ComponentModel.DataAnnotations;

namespace JobOnlineAPI.Models
{
    public class University
    {
        public int UniversityID { get; set; }

        [Required(ErrorMessage = "UniversityNameThai is required.")]
        public string UniversityNameThai { get; set; } = "";

        public string? UniversityNameEng { get; set; }

        [Required(ErrorMessage = "UniversityType is required.")]
        [RegularExpression("^(public|private)$", ErrorMessage = "UniversityType must be 'public' or 'private'.")]
        public string UniversityType { get; set; } = "";

        public bool IsActive { get; set; } = true;
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
    }
}
