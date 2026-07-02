-- Table: JobSlots
-- Each row is one open seat ("อัตรา") for a Job, with its own application period.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'JobSlots')
BEGIN
    CREATE TABLE JobSlots (
        SlotID               INT IDENTITY(1,1) PRIMARY KEY,
        JobID                INT NOT NULL FOREIGN KEY REFERENCES Jobs(JobID),
        SlotNumber           INT NOT NULL,
        StartDate            DATETIME2 NULL,
        EndDate              DATETIME2 NULL,
        Status               NVARCHAR(50) NOT NULL DEFAULT 'Open', -- Open / Filled / Closed
        AssignedApplicantID  INT NULL,
        AssignedDate         DATETIME2 NULL,
        CreatedAt            DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedAt           DATETIME2 NULL,
        CONSTRAINT UQ_JobSlots_Job_SlotNumber UNIQUE (JobID, SlotNumber)
    )
    PRINT 'Created JobSlots'
END

GO
CREATE OR ALTER PROCEDURE sp_GetJobSlotsByJobID
    @JobID INT
AS
BEGIN
    SELECT SlotID, JobID, SlotNumber, StartDate, EndDate, Status,
           AssignedApplicantID, AssignedDate, CreatedAt, ModifiedAt
    FROM JobSlots
    WHERE JobID = @JobID
    ORDER BY SlotNumber
END

GO
CREATE OR ALTER PROCEDURE sp_AddJobSlot
    @JobID      INT,
    @SlotNumber INT,
    @StartDate  DATETIME2 = NULL,
    @EndDate    DATETIME2 = NULL
AS
BEGIN
    INSERT INTO JobSlots (JobID, SlotNumber, StartDate, EndDate)
    VALUES (@JobID, @SlotNumber, @StartDate, @EndDate)

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
        ModifiedAt = GETUTCDATE()
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
        AssignedDate        = GETUTCDATE(),
        Status               = 'Filled',
        ModifiedAt           = GETUTCDATE()
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

    -- Result set 2: slots for those jobs
    SELECT s.SlotID, s.JobID, s.SlotNumber, s.StartDate, s.EndDate, s.Status,
           s.AssignedApplicantID, s.AssignedDate
    FROM JobSlots s
    INNER JOIN Jobs j ON j.JobID = s.JobID
    WHERE (@Department IS NULL OR j.Department = @Department)
    ORDER BY s.JobID, s.SlotNumber
END
