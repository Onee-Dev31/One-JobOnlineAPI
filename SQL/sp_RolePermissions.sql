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
-- 3. Alter sp_GetRoutesByRoleName to ORDER BY SortOrder
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
