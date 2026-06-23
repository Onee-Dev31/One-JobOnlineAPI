-- ============================================================
-- 1. Add SortOrder column to T_ROLE_PERMISSION
--    (skip if already exists)
-- ============================================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('T_ROLE_PERMISSION')
      AND name = 'SortOrder'
)
BEGIN
    ALTER TABLE T_ROLE_PERMISSION
        ADD SortOrder INT NOT NULL DEFAULT 0;
END
GO

-- ============================================================
-- 2. Initialize SortOrder from current alphabetical order
--    (per RoleID group) so existing menus keep their sequence
-- ============================================================
WITH Ranked AS (
    SELECT
        ID,
        ROW_NUMBER() OVER (PARTITION BY RoleID ORDER BY RoutePath) AS rn
    FROM T_ROLE_PERMISSION
)
UPDATE p
SET p.SortOrder = r.rn
FROM T_ROLE_PERMISSION p
JOIN Ranked r ON p.ID = r.ID;

-- ต่อท้าย: /admin/admin-users ให้อยู่หลังสุดของ RoleID นั้นๆ
UPDATE p
SET p.SortOrder = (
    SELECT MAX(SortOrder) + 1
    FROM T_ROLE_PERMISSION
    WHERE RoleID = p.RoleID
)
FROM T_ROLE_PERMISSION p
WHERE p.RoutePath = '/admin/admin-users';
GO

-- ============================================================
-- 3. Add sp_GetRoutesByRoleNameWithDetail (returns ID + SortOrder)
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_GetRoutesByRoleNameWithDetail]
    @RoleName NVARCHAR(100)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT p.ID, p.RoutePath, p.SortOrder
    FROM T_ROLE r
    INNER JOIN T_ROLE_PERMISSION p ON r.ID = p.RoleID
    WHERE r.ROLE_NAME = @RoleName
    ORDER BY p.SortOrder;
END
GO

-- ============================================================
-- 4. Add sp_UpdateRoutesSortOrder (bulk update via JSON)
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_UpdateRoutesSortOrder]
    @payload NVARCHAR(MAX)  -- JSON: [{"ID":10,"SortOrder":1}, ...]
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE p
    SET p.SortOrder = j.SortOrder
    FROM T_ROLE_PERMISSION p
    JOIN OPENJSON(@payload)
        WITH (ID INT, SortOrder INT) j ON p.ID = j.ID;
END
GO

-- ============================================================
-- 5. Alter sp_GetRoutesByRoleName to ORDER BY SortOrder
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
    ORDER BY p.SortOrder;
END
GO
