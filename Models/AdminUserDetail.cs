namespace JobOnlineAPI.Models
{
    public class AdminUserDetail
    {
        public int AdminID { get; set; }
        public string Username { get; set; } = string.Empty;
        public int? HRID { get; set; }
        public string? EMAIL { get; set; }
        public string? Department { get; set; }
        public string? EmpNo { get; set; }
        public string? NameThai { get; set; }
        public string? Mobile { get; set; }
        public string? Position { get; set; }
        public string? CompanyName { get; set; }
        public int? RoleID { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string? Role { get; set; }
    }

    public class AdminUserCreateRequest
    {
        public required string Username { get; set; }
        public int? HRID { get; set; }
        public string? EMAIL { get; set; }
        public string? Department { get; set; }
        public string? EmpNo { get; set; }
        public string? NameThai { get; set; }
        public string? Mobile { get; set; }
        public string? Position { get; set; }
        public string? CompanyName { get; set; }
        public int? RoleID { get; set; }
    }

    public class AdminUserUpdateRequest
    {
        public string? EMAIL { get; set; }
        public string? Department { get; set; }
        public string? NameThai { get; set; }
        public string? Mobile { get; set; }
        public string? Position { get; set; }
        public string? CompanyName { get; set; }
        public int? RoleID { get; set; }
        public bool? IsActive { get; set; }
    }
}
