namespace JobOnlineAPI.Models
{
    public class RolePermissionFlat
    {
        public int RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string RoutePath { get; set; } = string.Empty;
    }

    public class RolePermissionResponse
    {
        public int RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public List<string> Routes { get; set; } = [];
    }

    public class MyPermissionResponse
    {
        public List<string> Routes { get; set; } = [];
    }

    public class RoutesSortOrderItem
    {
        public int ID { get; set; }
        public int SortOrder { get; set; }
    }

    public class RouteDetail
    {
        public int ID { get; set; }
        public string RoutePath { get; set; } = string.Empty;
        public int SortOrder { get; set; }
    }
}
