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

-- Routes that exist in the frontend but were never seeded into any role's menu.
-- Seeded for Admin only, matching the /admin/role-permissions precedent above;
-- run POST /api/RolePermissions afterward to add other roles as needed.
-- /admin/new-coming → Role: Admin
INSERT INTO T_ROLE_PERMISSION (RoleID, RoutePath, SortOrder)
SELECT
    r.ID,
    '/admin/new-coming',
    ISNULL((SELECT MAX(SortOrder) FROM T_ROLE_PERMISSION WHERE RoleID = r.ID), 0) + 1
FROM T_ROLE r
WHERE r.ROLE_NAME = 'Admin'
  AND NOT EXISTS (
      SELECT 1 FROM T_ROLE_PERMISSION
      WHERE RoleID = r.ID AND RoutePath = '/admin/new-coming'
  );
GO

-- /admin/settings/job-groups → Role: Admin
INSERT INTO T_ROLE_PERMISSION (RoleID, RoutePath, SortOrder)
SELECT
    r.ID,
    '/admin/settings/job-groups',
    ISNULL((SELECT MAX(SortOrder) FROM T_ROLE_PERMISSION WHERE RoleID = r.ID), 0) + 1
FROM T_ROLE r
WHERE r.ROLE_NAME = 'Admin'
  AND NOT EXISTS (
      SELECT 1 FROM T_ROLE_PERMISSION
      WHERE RoleID = r.ID AND RoutePath = '/admin/settings/job-groups'
  );
GO

-- /admin/settings/universities → Role: Admin
INSERT INTO T_ROLE_PERMISSION (RoleID, RoutePath, SortOrder)
SELECT
    r.ID,
    '/admin/settings/universities',
    ISNULL((SELECT MAX(SortOrder) FROM T_ROLE_PERMISSION WHERE RoleID = r.ID), 0) + 1
FROM T_ROLE r
WHERE r.ROLE_NAME = 'Admin'
  AND NOT EXISTS (
      SELECT 1 FROM T_ROLE_PERMISSION
      WHERE RoleID = r.ID AND RoutePath = '/admin/settings/universities'
  );
GO
