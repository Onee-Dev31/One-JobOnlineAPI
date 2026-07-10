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
        public int? ReportsToAdminID { get; set; }
        public string? ReportsToName { get; set; }
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
        // Required only when RoleID resolves to the "Secretary" role — the boss's EmpNo.
        public string? ReportsToEmpNo { get; set; }
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
        // Boss's EmpNo. Omit the field entirely to leave the existing ReportsToAdminID relationship untouched
        // (sent only when RoleID is "Secretary"); a non-empty value repoints ReportsToAdminID at that boss.
        public string? ReportsToEmpNo { get; set; }
    }

    public class AdminRole
    {
        public int RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }

    // Raw shape returned by sp_CreateSecretaryAdminUser.
    public class SecretaryCreateResult
    {
        public int NewAdminID { get; set; }
        public bool BossWasNewlyCreated { get; set; }
        public int? BossAdminID { get; set; }
        public string? BossNameThai { get; set; }
    }
}
