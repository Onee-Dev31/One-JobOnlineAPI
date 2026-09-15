-- ============================================================
-- 1. Add IsVisible / Label / Icon to T_ROLE_PERMISSION
--    (skip if already exists)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('T_ROLE_PERMISSION')
      AND name = 'IsVisible'
)
BEGIN
    ALTER TABLE T_ROLE_PERMISSION
        ADD IsVisible BIT NOT NULL DEFAULT 1;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('T_ROLE_PERMISSION')
      AND name = 'Label'
)
BEGIN
    ALTER TABLE T_ROLE_PERMISSION
        ADD Label NVARCHAR(200) NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('T_ROLE_PERMISSION')
      AND name = 'Icon'
)
BEGIN
    ALTER TABLE T_ROLE_PERMISSION
        ADD Icon NVARCHAR(50) NULL;
END
GO

-- ============================================================
-- 2. sp_GetRoutesByRoleName: filter to IsVisible = 1
--    (used by GET /RolePermissions/my for the legacy `routes` list)
-- ============================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[sp_GetRoutesByRoleName]
    @RoleName NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.RoutePath
    FROM T_ROLE r
    INNER JOIN T_ROLE_PERMISSION p ON r.ID = p.RoleID
    WHERE r.ROLE_NAME = @RoleName
      AND p.IsVisible = 1
    ORDER BY p.SortOrder;
END
GO

-- ============================================================
-- 3. sp_GetRoutesByRoleNameWithDetail: filter to IsVisible = 1,
--    return Label/Icon (used by GET /RolePermissions/by-role/{role}
--    and GET /RolePermissions/my for the new `items` list)
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_GetRoutesByRoleNameWithDetail]
    @RoleName NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.ID, p.RoutePath, p.SortOrder, p.Label, p.Icon
    FROM T_ROLE r
    INNER JOIN T_ROLE_PERMISSION p ON r.ID = p.RoleID
    WHERE r.ROLE_NAME = @RoleName
      AND p.IsVisible = 1
    ORDER BY p.SortOrder;
END
GO

-- ============================================================
-- 4. sp_GetAllRolePermissionsDetail: master list across every role
--    (visible AND hidden), for the new backoffice management screen.
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_GetAllRolePermissionsDetail]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.ID, p.RoleID, r.ROLE_NAME AS RoleName, p.RoutePath,
           p.Label, p.Icon, p.IsVisible, p.SortOrder
    FROM T_ROLE_PERMISSION p
    INNER JOIN T_ROLE r ON r.ID = p.RoleID
    ORDER BY r.ROLE_NAME, p.SortOrder;
END
GO

-- ============================================================
-- 5. sp_GetRolePermissionByRoleAndRoute: duplicate check used by
--    sp_CreateRolePermission's caller before inserting.
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_GetRolePermissionByRoleAndRoute]
    @RoleID INT,
    @RoutePath NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.ID, p.RoleID, r.ROLE_NAME AS RoleName, p.RoutePath,
           p.Label, p.Icon, p.IsVisible, p.SortOrder
    FROM T_ROLE_PERMISSION p
    INNER JOIN T_ROLE r ON r.ID = p.RoleID
    WHERE p.RoleID = @RoleID AND p.RoutePath = @RoutePath;
END
GO

-- ============================================================
-- 6. sp_CreateRolePermission: add a route to a role. @SortOrder
--    NULL = append to the end of that role's current menu.
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_CreateRolePermission]
    @RoleID     INT,
    @RoutePath  NVARCHAR(200),
    @Label      NVARCHAR(200) = NULL,
    @Icon       NVARCHAR(50) = NULL,
    @IsVisible  BIT = 1,
    @SortOrder  INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @ResolvedSortOrder INT = COALESCE(
        @SortOrder,
        (SELECT MAX(SortOrder) FROM T_ROLE_PERMISSION WHERE RoleID = @RoleID) + 1,
        1
    );

    INSERT INTO T_ROLE_PERMISSION (RoleID, RoutePath, Label, Icon, IsVisible, SortOrder)
    VALUES (@RoleID, @RoutePath, @Label, @Icon, @IsVisible, @ResolvedSortOrder);

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewID;
END
GO

-- ============================================================
-- 7. sp_UpdateRolePermission: field left NULL = keep existing value.
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_UpdateRolePermission]
    @ID        INT,
    @Label     NVARCHAR(200) = NULL,
    @Icon      NVARCHAR(50) = NULL,
    @IsVisible BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE T_ROLE_PERMISSION SET
        Label     = COALESCE(@Label, Label),
        Icon      = COALESCE(@Icon, Icon),
        IsVisible = COALESCE(@IsVisible, IsVisible)
    WHERE ID = @ID;

    SELECT @@ROWCOUNT AS AffectedRows;
END
GO

-- ============================================================
-- 8. sp_DeleteRolePermission: remove a route from a role.
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_DeleteRolePermission]
    @ID INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM T_ROLE_PERMISSION WHERE ID = @ID;
    SELECT @@ROWCOUNT AS AffectedRows;
END
GO
