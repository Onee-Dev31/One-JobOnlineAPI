-- ============================================================
-- Multi-role menu assignment support for the Menu Management screen.
-- Run this AFTER setup_RolePermissionsVisibility.sql.
-- Safe to re-run.
-- ============================================================

-- ============================================================
-- 1. Data hygiene: unify Label/Icon across every role assignment
--    that shares the same RoutePath, using the lowest-ID row as
--    canonical. From now on Label/Icon/IsVisible are shared per
--    route across all roles (SortOrder stays per-role).
-- ============================================================
;WITH Canonical AS (
    SELECT
        RoutePath,
        Label AS CanonicalLabel,
        Icon AS CanonicalIcon,
        ROW_NUMBER() OVER (PARTITION BY RoutePath ORDER BY ID) AS rn
    FROM T_ROLE_PERMISSION
)
UPDATE p SET
    Label = c.CanonicalLabel,
    Icon = c.CanonicalIcon
FROM T_ROLE_PERMISSION p
INNER JOIN Canonical c ON c.RoutePath = p.RoutePath AND c.rn = 1
WHERE ISNULL(p.Label, '') <> ISNULL(c.CanonicalLabel, '')
   OR ISNULL(p.Icon, '') <> ISNULL(c.CanonicalIcon, '');
GO

-- ============================================================
-- 2. sp_SyncRolePermissionByRoute: create/edit a route's role
--    assignments in one call. Roles missing from @RoleIDsJson are
--    unassigned (row deleted), roles already present are updated,
--    roles newly listed are added (SortOrder appended per role).
--    Used by both "create menu" (route has no rows yet) and
--    "edit menu" (route already has some rows) from the frontend.
-- ============================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE OR ALTER PROCEDURE [dbo].[sp_SyncRolePermissionByRoute]
    @RoutePath    NVARCHAR(200),
    @Label        NVARCHAR(200) = NULL,
    @Icon         NVARCHAR(50) = NULL,
    @IsVisible    BIT = 1,
    @RoleIDsJson  NVARCHAR(MAX)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @DesiredRoles TABLE (RoleID INT PRIMARY KEY);
    INSERT INTO @DesiredRoles (RoleID)
    SELECT value FROM OPENJSON(@RoleIDsJson) WITH (value INT '$');

    BEGIN TRAN;

    DELETE FROM T_ROLE_PERMISSION
    WHERE RoutePath = @RoutePath
      AND RoleID NOT IN (SELECT RoleID FROM @DesiredRoles);

    UPDATE p SET
        Label = @Label,
        Icon = @Icon,
        IsVisible = @IsVisible
    FROM T_ROLE_PERMISSION p
    INNER JOIN @DesiredRoles d ON d.RoleID = p.RoleID
    WHERE p.RoutePath = @RoutePath;

    INSERT INTO T_ROLE_PERMISSION (RoleID, RoutePath, Label, Icon, IsVisible, SortOrder)
    SELECT
        d.RoleID,
        @RoutePath,
        @Label,
        @Icon,
        @IsVisible,
        ISNULL((SELECT MAX(SortOrder) FROM T_ROLE_PERMISSION WHERE RoleID = d.RoleID), 0) + 1
    FROM @DesiredRoles d
    WHERE NOT EXISTS (
        SELECT 1 FROM T_ROLE_PERMISSION p
        WHERE p.RoutePath = @RoutePath AND p.RoleID = d.RoleID
    );

    COMMIT TRAN;
END
GO
