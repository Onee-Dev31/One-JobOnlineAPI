SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER   PROCEDURE [dbo].[sp_GetDepartmentDashboard]
    @Department NVARCHAR(200) = NULL
AS
BEGIN
    -- Result set 1: jobs
    SELECT JobID, JobTitle, JobDescription, Department, NumberOfPositions, JobStatus,
           JobGroupID, Office, Tier, EmployeeType
    FROM Jobs
    WHERE (@Department IS NULL OR Department = @Department)

    -- Result set 2: slots for that department
    SELECT JS.SlotID, JS.Department, JS.SlotNumber, JS.StartDate, JS.EndDate, JS.Status,
           JS.AssignedApplicantID,
           APP.FirstNameThai AS AssignedApplicantFirstNameThai,
           APP.LastNameThai AS AssignedApplicantLastNameThai,
           JS.AssignedDate, JS.CreatedByAdminID, JS.RequestedByName, JS.ModifiedByAdminID
    FROM JobSlots JS
    LEFT JOIN T_APPLICANTS APP
        ON JS.AssignedApplicantID = APP.ApplicantID
    WHERE (@Department IS NULL OR JS.Department = @Department)
    ORDER BY JS.Department, JS.SlotNumber
END
GO
