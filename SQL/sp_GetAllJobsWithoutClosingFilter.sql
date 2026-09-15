SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[sp_GetAllJobsWithoutClosingFilter]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        j.JobID AS JobID,
        MAX(j.JobTitle) AS JobTitle,
        MAX(j.JobDescription) AS JobDescription,
        MAX(j.Requirements) AS Requirements,
        MAX(j.Location) AS Location,
        MAX(j.ExperienceYears) AS ExperienceYears,
        MAX(j.NumberOfPositions) AS NumberOfPositions,
        MAX(j.Department) AS Department,
        MAX(j.JobStatus) AS JobStatus,
        MAX(j.PostedDate) AS PostedDate,
        MAX(j.ClosingDate) AS ClosingDate,
        MAX(j.CreatedBy) AS CreatedBy,
        MAX(j.CreatedByRole) AS CreatedByRole,
        MAX(j.CreatedDate) AS CreatedDate,
        MAX(j.ModifiedBy) AS ModifiedBy,
        MAX(j.ModifiedDate) AS ModifiedDate,
        MAX(j.ApprovalStatus) AS ApprovalStatus,
        ISNULL(MAX(j.OpenFor), '-') AS OpenFor,
        COUNT(ja.ApplicantID) AS ApplicantCount,
        ISNULL(MAX(j.Remark), '-') AS Remark,
        MAX(j.JobGroupID) AS JobGroupID,
        MAX(j.Office) AS Office,
        MAX(j.LevelID) AS LevelID,
        MAX(jl.LevelName) AS LevelName,
        MAX(j.EmployeeTypeID) AS EmployeeTypeID,
        MAX(et.TypeName) AS EmployeeTypeName
    FROM
        Jobs j
    LEFT JOIN
        JobApplications ja ON j.JobID = ja.JobID
    LEFT JOIN JobLevels jl ON jl.JobLevelID = j.LevelID
    LEFT JOIN EmployeeTypes et ON et.EmployeeTypeID = j.EmployeeTypeID
    GROUP BY
        j.JobID
    ORDER BY
        MAX(j.CreatedDate) DESC
END
GO
