-- Table: JobSlots
-- Each row is one open seat ("อัตรา") for a Department, with its own application period.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'JobSlots')
BEGIN
    CREATE TABLE JobSlots (
        SlotID               INT IDENTITY(1,1) PRIMARY KEY,
        Department           NVARCHAR(200) NOT NULL,
        SlotNumber           INT NOT NULL,
        StartDate            DATETIME2 NULL,
        EndDate              DATETIME2 NULL,
        Status               NVARCHAR(50) NOT NULL DEFAULT 'Open', -- Open / Filled / Closed
        AssignedApplicantID  INT NULL,
        AssignedDate         DATETIME2 NULL,
        CreatedByAdminID     INT NULL FOREIGN KEY REFERENCES AdminUsers(AdminID), -- who logged in and created it
        RequestedByName      NVARCHAR(200) NULL, -- who asked for it (may not have a system login)
        CreatedAt            DATETIME2 NOT NULL DEFAULT GETDATE(),
        ModifiedAt           DATETIME2 NULL,
        CONSTRAINT UQ_JobSlots_Department_SlotNumber UNIQUE (Department, SlotNumber)
    )
    PRINT 'Created JobSlots'
END

GO
CREATE OR ALTER PROCEDURE sp_GetJobSlotsByDepartment
    @Department NVARCHAR(200)
AS
BEGIN
    SELECT SlotID, Department, SlotNumber, StartDate, EndDate, Status,
           AssignedApplicantID, AssignedDate, CreatedByAdminID, RequestedByName, CreatedAt, ModifiedAt
    FROM JobSlots
    WHERE Department = @Department
    ORDER BY SlotNumber
END

GO
CREATE OR ALTER PROCEDURE sp_AddJobSlot
    @Department       NVARCHAR(200),
    @SlotNumber       INT,
    @StartDate        DATETIME2 = NULL,
    @EndDate          DATETIME2 = NULL,
    @CreatedByAdminID INT = NULL,
    @RequestedByName  NVARCHAR(200) = NULL
AS
BEGIN
    INSERT INTO JobSlots (Department, SlotNumber, StartDate, EndDate, CreatedByAdminID, RequestedByName)
    VALUES (@Department, @SlotNumber, @StartDate, @EndDate, @CreatedByAdminID, @RequestedByName)

    SELECT CAST(SCOPE_IDENTITY() AS INT)
END

GO
CREATE OR ALTER PROCEDURE sp_UpdateJobSlot
    @SlotID     INT,
    @StartDate  DATETIME2 = NULL,
    @EndDate    DATETIME2 = NULL,
    @Status     NVARCHAR(50) = NULL
AS
BEGIN
    UPDATE JobSlots
    SET StartDate  = @StartDate,
        EndDate    = @EndDate,
        Status     = COALESCE(@Status, Status),
        ModifiedAt = GETDATE()
    WHERE SlotID = @SlotID
END

GO
CREATE OR ALTER PROCEDURE sp_DeleteJobSlot
    @SlotID INT
AS
BEGIN
    DELETE FROM JobSlots WHERE SlotID = @SlotID
END

GO
CREATE OR ALTER PROCEDURE sp_AssignApplicantToSlot
    @SlotID      INT,
    @ApplicantID INT
AS
BEGIN
    UPDATE JobSlots
    SET AssignedApplicantID = @ApplicantID,
        AssignedDate        = GETDATE(),
        Status               = 'Filled',
        ModifiedAt           = GETDATE()
    WHERE SlotID = @SlotID
END

GO
CREATE OR ALTER PROCEDURE sp_GetDepartmentDashboard
    @Department NVARCHAR(200) = NULL
AS
BEGIN
    -- Result set 1: jobs
    SELECT JobID, JobTitle, JobDescription, Department, NumberOfPositions, JobStatus
    FROM Jobs
    WHERE (@Department IS NULL OR Department = @Department)

    -- Result set 2: slots for that department
    SELECT SlotID, Department, SlotNumber, StartDate, EndDate, Status,
           AssignedApplicantID, AssignedDate, CreatedByAdminID, RequestedByName
    FROM JobSlots
    WHERE (@Department IS NULL OR Department = @Department)
    ORDER BY Department, SlotNumber
END
