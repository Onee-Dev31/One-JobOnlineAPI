-- Step 2 of the Tier/EmployeeType -> LevelID/EmployeeTypeID cutover. IRREVERSIBLE.
--
-- Run this ONLY after:
--   1) migrate_JobsLevelEmployeeType.sql has been run and both "unmatched" reports came back empty
--      (or every unmatched row has been resolved by hand).
--   2) sp_AddJob, sp_UpdateJob, sp_GetAllJobsV2, sp_GetAllJobsAdmin,
--      sp_GetAllJobsWithoutClosingFilter, sp_GetDepartmentDashboard, setup_JobSlots.sql,
--      setup_TraineeManagementV2.sql, setup_TraineeApplicationsFromApplicants.sql, and
--      setup_ApplicantDataFormTraineeFiles.sql have all been redeployed with the
--      LevelID/EmployeeTypeID versions — otherwise those procs break immediately.

DECLARE @sql NVARCHAR(MAX) = N'';

SELECT @sql = @sql + N'ALTER TABLE Jobs DROP CONSTRAINT ' + QUOTENAME(dc.name) + N';' + CHAR(10)
FROM sys.default_constraints dc
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.Jobs')
  AND c.name IN ('Tier', 'EmployeeType')

EXEC sp_executesql @sql
GO

ALTER TABLE Jobs DROP COLUMN Tier, EmployeeType
GO
