-- Migration: JobSlots.JobID -> JobSlots.Department
-- Run this once against a database where JobSlots was already created with the
-- old JobID-based schema (before the Department refactor). Re-running this
-- script is safe - every step is guarded and skips if already applied.
-- BACK UP THE JobSlots TABLE BEFORE RUNNING (this drops the JobID column).

-- 1. Add Department column (nullable first, so existing rows can be backfilled)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('JobSlots') AND name = 'Department'
)
BEGIN
    ALTER TABLE JobSlots ADD Department NVARCHAR(200) NULL;
END
GO

-- 2. Backfill Department from the linked Job, for rows that still have JobID
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('JobSlots') AND name = 'JobID'
)
BEGIN
    UPDATE s
    SET s.Department = j.Department
    FROM JobSlots s
    INNER JOIN Jobs j ON j.JobID = s.JobID
    WHERE s.Department IS NULL;
END
GO

-- 3. Drop the old FK constraint on JobID (name is system-generated, so look it up)
DECLARE @fkName NVARCHAR(200);
SELECT @fkName = fk.name
FROM sys.foreign_keys fk
WHERE fk.parent_object_id = OBJECT_ID('JobSlots')
  AND EXISTS (
      SELECT 1 FROM sys.foreign_key_columns fkc
      INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
      WHERE fkc.constraint_object_id = fk.object_id AND c.name = 'JobID'
  );

IF @fkName IS NOT NULL
    EXEC('ALTER TABLE JobSlots DROP CONSTRAINT ' + @fkName);
GO

-- 4. Drop the old unique constraint on (JobID, SlotNumber), if present
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_JobSlots_Job_SlotNumber')
    ALTER TABLE JobSlots DROP CONSTRAINT UQ_JobSlots_Job_SlotNumber;
GO

-- 5. Drop the JobID column itself
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('JobSlots') AND name = 'JobID'
)
BEGIN
    ALTER TABLE JobSlots DROP COLUMN JobID;
END
GO

-- 6. Now that every row has a Department, enforce NOT NULL
--    (skips with a warning if some rows still have no Department - fix those
--    manually, e.g. orphaned slots whose Job no longer exists, then re-run)
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('JobSlots') AND name = 'Department' AND is_nullable = 1
)
BEGIN
    IF EXISTS (SELECT 1 FROM JobSlots WHERE Department IS NULL)
        PRINT 'WARNING: JobSlots has rows with NULL Department - fix them manually, then re-run this script to enforce NOT NULL.'
    ELSE
        ALTER TABLE JobSlots ALTER COLUMN Department NVARCHAR(200) NOT NULL;
END
GO

-- 7. Add the new unique constraint (Department, SlotNumber)
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_JobSlots_Department_SlotNumber')
    ALTER TABLE JobSlots ADD CONSTRAINT UQ_JobSlots_Department_SlotNumber UNIQUE (Department, SlotNumber);
GO

-- 8. Add CreatedByAdminID (who logged in and created it) and RequestedByName
--    (who asked for it, may not have a system login)
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('JobSlots') AND name = 'CreatedByAdminID'
)
BEGIN
    ALTER TABLE JobSlots ADD CreatedByAdminID INT NULL FOREIGN KEY REFERENCES AdminUsers(AdminID);
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('JobSlots') AND name = 'RequestedByName'
)
BEGIN
    ALTER TABLE JobSlots ADD RequestedByName NVARCHAR(200) NULL;
END
GO
