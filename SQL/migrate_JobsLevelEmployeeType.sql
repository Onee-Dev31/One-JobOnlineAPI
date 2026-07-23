-- Step 1 of the Tier/EmployeeType -> LevelID/EmployeeTypeID cutover.
-- Additive and non-destructive: adds the new FK columns and backfills them by matching the
-- existing free-text values against JobLevels.LevelName / EmployeeTypes.TypeName.
-- Run setup_JobLevels.sql and setup_EmployeeTypes.sql (tables + seed data) BEFORE this script.
--
-- Does NOT drop Jobs.Tier / Jobs.EmployeeType — that is a separate, deliberately irreversible
-- step (see migrate_JobsLevelEmployeeType_DropColumns.sql), to be run only after confirming the
-- two "unmatched" reports below come back empty and all dependent procs have been redeployed.

IF COL_LENGTH('Jobs', 'LevelID') IS NULL
    ALTER TABLE Jobs ADD LevelID INT NULL
GO

IF COL_LENGTH('Jobs', 'EmployeeTypeID') IS NULL
    ALTER TABLE Jobs ADD EmployeeTypeID INT NULL
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Jobs_JobLevels')
    ALTER TABLE Jobs ADD CONSTRAINT FK_Jobs_JobLevels FOREIGN KEY (LevelID) REFERENCES JobLevels(JobLevelID)
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Jobs_EmployeeTypes')
    ALTER TABLE Jobs ADD CONSTRAINT FK_Jobs_EmployeeTypes FOREIGN KEY (EmployeeTypeID) REFERENCES EmployeeTypes(EmployeeTypeID)
GO

-- Backfill by exact name match. Only touches rows that aren't mapped yet, so this script is
-- safe to re-run.
UPDATE J
SET J.LevelID = L.JobLevelID
FROM Jobs J
JOIN JobLevels L ON L.LevelName = J.Tier
WHERE J.LevelID IS NULL AND J.Tier IS NOT NULL
GO

UPDATE J
SET J.EmployeeTypeID = E.EmployeeTypeID
FROM Jobs J
JOIN EmployeeTypes E ON E.TypeName = J.EmployeeType
WHERE J.EmployeeTypeID IS NULL AND J.EmployeeType IS NOT NULL
GO

-- Report only — does NOT auto-map. Any row listed here has a Tier/EmployeeType value with no
-- matching row in JobLevels/EmployeeTypes (typo or retired value) and needs a human decision
-- before Tier/EmployeeType can be dropped.
SELECT JobID, Tier AS UnmatchedTier
FROM Jobs
WHERE Tier IS NOT NULL AND LevelID IS NULL

SELECT JobID, EmployeeType AS UnmatchedEmployeeType
FROM Jobs
WHERE EmployeeType IS NOT NULL AND EmployeeTypeID IS NULL
GO
