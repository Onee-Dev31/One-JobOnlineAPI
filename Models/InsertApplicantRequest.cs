namespace JobOnlineAPI.Models
{
    public class InsertApplicantRequest
    {
        public int JobID { get; set; }
        public string? FirstNameThai { get; set; }
        public string? LastNameThai { get; set; }
        public string? FirstNameEng { get; set; }
        public string? LastNameEng { get; set; }
        public string? Nickname { get; set; }
        public string? Email { get; set; }
        public string? MobilePhone { get; set; }
        public string? JobTitle { get; set; }
        public string? CompanyName { get; set; }
        public string? Salary { get; set; }
        public string? Address { get; set; }
        public string? BirthDate { get; set; }
        public string? Gender { get; set; }
        public string? Nationality { get; set; }
        public string? Position { get; set; }
        public string? StartWorkDate { get; set; }
        public string? Source { get; set; }
        public string? Remark { get; set; }

        /// <summary>JSON array string เช่น [{"Degree":"ปริญญาตรี","Major":"IT","University":"ม.xxx","GraduateYear":"2020"}]</summary>
        public string? EducationList { get; set; }

        /// <summary>JSON array string เช่น [{"Company":"บริษัท","Position":"Dev","Duration":"2 ปี"}]</summary>
        public string? WorkExperienceList { get; set; }

        /// <summary>JSON array string เช่น [{"SkillName":"C#","Level":"ดี"}]</summary>
        public string? SkillsList { get; set; }
        public string? Title { get; set; }

        public IFormFileCollection? Files { get; set; }
    }
}
