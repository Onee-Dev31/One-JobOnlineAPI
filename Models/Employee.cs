namespace JobOnlineAPI.Models
{
    // Raw shape returned by sp_SearchEmployees (includes TotalCount for pagination, internal use only).
    public class EmployeeSearchRow
    {
        public string EmpNo { get; set; } = string.Empty;
        public string? AdUser { get; set; }
        public string? NameThai { get; set; }
        public string? NickName { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? Telephone { get; set; }
        public string? DepartmentCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? Position { get; set; }
        public string? CompanyCode { get; set; }
        public string? CompanyName { get; set; }
        public bool IsAdmin { get; set; }
        public int TotalCount { get; set; }
    }

    public class EmployeeSearchResult
    {
        public string EmpNo { get; set; } = string.Empty;
        public string? AdUser { get; set; }
        public string? NameThai { get; set; }
        public string? Nickname { get; set; }
        public string? Email { get; set; }
        public string? Mobile { get; set; }
        public string? Telephone { get; set; }
        public string? DepartmentCode { get; set; }
        public string? DepartmentName { get; set; }
        public string? Position { get; set; }
        public string? CompanyCode { get; set; }
        public string? CompanyName { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class EmployeeSearchResponse
    {
        public IEnumerable<EmployeeSearchResult> Items { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
    }
}
