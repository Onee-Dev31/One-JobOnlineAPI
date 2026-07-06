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
    SELECT SlotID, Department, SlotNumber, StartDate, EndDate, Status,
           AssignedApplicantID, AssignedDate, CreatedByAdminID, RequestedByName, ModifiedByAdminID
    FROM JobSlots
    WHERE (@Department IS NULL OR Department = @Department)
    ORDER BY Department, SlotNumber
END
GO
