-- Fixes the JobLevels seed that was already inserted by the older version of
-- setup_JobLevels.sql (8 rows, with a placeholder 'P' that should have been
-- 'Executive Director'/'VP'/'SVP'). TRUNCATE TABLE JobLevels fails once
-- FK_Jobs_JobLevels exists (see migrate_JobsLevelEmployeeType.sql) even if no
-- Jobs row references it yet -- so fix in place with UPDATE/INSERT instead of
-- wiping the table. Safe to re-run.

-- Rename the placeholder row in place (keeps its JobLevelID, so if any Job
-- already points at it via LevelID, the FK stays valid and just shows the
-- corrected name instead of breaking).
UPDATE JobLevels SET LevelName = N'Executive Director', SortOrder = 6
WHERE LevelName = N'P'
GO

-- Insert the two levels that were missing from the original seed.
IF NOT EXISTS (SELECT 1 FROM JobLevels WHERE LevelName = N'VP')
    INSERT INTO JobLevels (LevelName, SortOrder, IsActive, CreatedDate) VALUES (N'VP', 7, 1, GETDATE())
GO
IF NOT EXISTS (SELECT 1 FROM JobLevels WHERE LevelName = N'SVP')
    INSERT INTO JobLevels (LevelName, SortOrder, IsActive, CreatedDate) VALUES (N'SVP', 8, 1, GETDATE())
GO

-- Shift EVP/C-Level to make room for the two new levels above.
UPDATE JobLevels SET SortOrder = 9 WHERE LevelName = N'EVP'
GO
UPDATE JobLevels SET SortOrder = 10 WHERE LevelName = N'C-Level'
GO

-- Verify: should show 10 rows in this exact order.
SELECT JobLevelID, LevelName, SortOrder, IsActive FROM JobLevels ORDER BY SortOrder
GO
