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
    SELECT
        JS.SlotID, JS.Department, JS.NumberOfPositions, JS.StartDate, JS.EndDate, JS.Status,
        COUNT(A.AssignmentID) AS AssignedCount,
        JS.CreatedByAdminID, JS.RequestedByName, JS.ModifiedByAdminID
    FROM JobSlots JS
    LEFT JOIN JobSlotAssignments A
        ON A.SlotID = JS.SlotID AND A.Status <> 'Cancelled'
    WHERE (@Department IS NULL OR JS.Department = @Department)
    GROUP BY JS.SlotID, JS.Department, JS.NumberOfPositions, JS.StartDate, JS.EndDate, JS.Status,
             JS.CreatedByAdminID, JS.RequestedByName, JS.ModifiedByAdminID
    ORDER BY JS.Department, JS.StartDate, JS.EndDate
END
GO
