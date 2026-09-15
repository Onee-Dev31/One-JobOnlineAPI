SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[sp_GetAllJobsAdmin]
    @JobID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        j.JobID,
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
        ISNULL(MAX(j.Remark), '-') AS Remark,
        MAX(j.JobGroupID) AS JobGroupID,
        MAX(j.Office) AS Office,
        MAX(j.LevelID) AS LevelID,
        MAX(jl.LevelName) AS LevelName,
        MAX(j.EmployeeTypeID) AS EmployeeTypeID,
        MAX(et.TypeName) AS EmployeeTypeName,
        COUNT(DISTINCT ja.ApplicationID) AS ApplicantCount,
        MAX(cd.COMPANY_CODE) AS ComCode,

        -- From T_EMPLOYEE
        MAX(t.CODEMPID) AS CODEMPID,
        MAX(t.NAMETHAI) AS NAMETHAI,
        MAX(t.NAMEENG) AS NAMEENG,
        MAX(t.NICKNAME) AS NICKNAME,
        MAX(t.POST) AS POST,
        MAX(t.DEPARTMENT) AS DepartmentName,
        MAX(t.JOBGRADE) AS JOBGRADE,
        COALESCE(MAX(CONCAT(h.NAMFIRSTT, ' ', h.NAMLASTT)),'-') AS OpenForNameThai,
        COALESCE(MAX(CONCAT(h.TITLEENG, h.NAMFIRSTE, ' ', h.NAMLASTE)),'-') AS OpenForNameENG

    FROM
        Jobs j
    LEFT JOIN
        JobApplications ja ON j.JobID = ja.JobID
    LEFT JOIN
        [HRMS_LINKED_SERVER].HRMS.Dbo.T_EMPLOYEE t ON j.CreatedByRole = t.AD_USER
    LEFT JOIN
        [HRMS_LINKED_SERVER].HRMS.Dbo.T_EMPLOYEE_SSO h ON j.OpenFor = h.CODEMPID
    LEFT JOIN [HRMS_LINKED_SERVER].HRMS.Dbo.T_EMPLOYEE_SSO cd
        ON j.Department = cd.COSTCENT
    LEFT JOIN JobLevels jl ON jl.JobLevelID = j.LevelID
    LEFT JOIN EmployeeTypes et ON et.EmployeeTypeID = j.EmployeeTypeID
    WHERE
        (@JobID IS NULL OR j.JobID = @JobID)
    GROUP BY
        j.JobID
    ORDER BY
        MAX(j.CreatedDate) DESC
END
GO
