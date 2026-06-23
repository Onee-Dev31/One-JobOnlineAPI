-- ============================================================
-- Seed: Insert missing routes into T_ROLE_PERMISSION
-- Run this after sp_RolePermissions.sql
-- Safe to re-run (INSERT only if not exists)
-- ============================================================

-- /admin/role-permissions → Role: Admin
INSERT INTO T_ROLE_PERMISSION (RoleID, RoutePath, SortOrder)
SELECT
    r.ID,
    '/admin/role-permissions',
    ISNULL((SELECT MAX(SortOrder) FROM T_ROLE_PERMISSION WHERE RoleID = r.ID), 0) + 1
FROM T_ROLE r
WHERE r.ROLE_NAME = 'Admin'
  AND NOT EXISTS (
      SELECT 1 FROM T_ROLE_PERMISSION
      WHERE RoleID = r.ID AND RoutePath = '/admin/role-permissions'
  );
GO
