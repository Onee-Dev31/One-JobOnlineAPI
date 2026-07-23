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
        public List<RouteDetail> Items { get; set; } = [];
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
        public string? Label { get; set; }
        public string? Icon { get; set; }
    }

    // Flat row from sp_GetAllRolePermissionsDetail / sp_GetRolePermissionByRoleAndRoute —
    // every (Role, Route) assignment, visible or not, for the backoffice management screen.
    public class RolePermissionItem
    {
        public int ID { get; set; }
        public int RoleID { get; set; }
        public string RoleName { get; set; } = string.Empty;
        public string RoutePath { get; set; } = string.Empty;
        public string? Label { get; set; }
        public string? Icon { get; set; }
        public bool IsVisible { get; set; }
        public int SortOrder { get; set; }
    }

    public class RolePermissionCreateRequest
    {
        public int RoleID { get; set; }
        public required string RoutePath { get; set; }
        public string? Label { get; set; }
        public string? Icon { get; set; }
        public bool IsVisible { get; set; } = true;
        // Omit to append to the end of that role's current menu.
        public int? SortOrder { get; set; }
    }

    // Field left null = keep the existing value untouched.
    public class RolePermissionUpdateRequest
    {
        public string? Label { get; set; }
        public string? Icon { get; set; }
        public bool? IsVisible { get; set; }
    }

    // Syncs one route's role assignments in a single call: roles not in RoleIDs are
    // unassigned, roles already assigned are updated, roles newly listed are added.
    // Used for both create (route has no rows yet) and edit (route already has some).
    public class RolePermissionSyncRequest
    {
        public required string RoutePath { get; set; }
        public string? Label { get; set; }
        public string? Icon { get; set; }
        public bool IsVisible { get; set; } = true;
        public required List<int> RoleIDs { get; set; }
    }
}
