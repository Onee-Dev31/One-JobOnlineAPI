-- Table: JobSlots
-- Each row is one "batch" (a recruiting window for a Department with N positions to fill).
-- Individual applicants assigned into a batch are tracked in JobSlotAssignments (1 batch : many assignments).
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'JobSlots')
BEGIN
    CREATE TABLE JobSlots (
        SlotID               INT IDENTITY(1,1) PRIMARY KEY,
        Department           NVARCHAR(200) NOT NULL,
        NumberOfPositions    INT NOT NULL,
        StartDate            DATETIME2 NULL,
        EndDate              DATETIME2 NULL,
        Status               NVARCHAR(50) NOT NULL DEFAULT 'Open', -- Open / Filled / Closed
        CreatedByAdminID     INT NULL FOREIGN KEY REFERENCES AdminUsers(AdminID), -- who logged in and created it
        RequestedByName      NVARCHAR(200) NULL, -- who asked for it (may not have a system login)
        CreatedAt            DATETIME2 NOT NULL DEFAULT GETDATE(),
        ModifiedAt           DATETIME2 NULL,
        ModifiedByAdminID    INT NULL FOREIGN KEY REFERENCES AdminUsers(AdminID) -- who logged in and last edited it
    )
    PRINT 'Created JobSlots'
END

-- Add ModifiedByAdminID if it doesn't exist yet (from a previous version of this script).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('JobSlots') AND name = 'ModifiedByAdminID')
BEGIN
    ALTER TABLE JobSlots ADD ModifiedByAdminID INT NULL FOREIGN KEY REFERENCES AdminUsers(AdminID)
END

-- Drop the old exact-match unique constraint if it exists (from a previous version of this script).
IF EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'UQ_JobSlots_Department_SlotNumber')
BEGIN
    ALTER TABLE JobSlots DROP CONSTRAINT UQ_JobSlots_Department_SlotNumber
END

-- Drop the overlap-prevention trigger if it exists (from a previous version of this script).
-- Department/SlotNumber and date ranges are no longer restricted at the DB level.
IF EXISTS (SELECT 1 FROM sys.triggers WHERE name = 'trg_JobSlots_PreventOverlap')
BEGIN
    DROP TRIGGER trg_JobSlots_PreventOverlap
END

-- Migrate from the old one-row-per-seat schema (SlotNumber/AssignedApplicantID/AssignedDate) to the
-- new one-row-per-batch schema (NumberOfPositions). Existing seat assignments move to JobSlotAssignments below.
-- The old table is kept as JobSlots_Old for a safety window before it gets dropped manually.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('JobSlots') AND name = 'SlotNumber')
BEGIN
    EXEC sp_rename 'JobSlots', 'JobSlots_Old';

    CREATE TABLE JobSlots (
        SlotID               INT IDENTITY(1,1) PRIMARY KEY,
        Department           NVARCHAR(200) NOT NULL,
        NumberOfPositions    INT NOT NULL,
        StartDate            DATETIME2 NULL,
        EndDate              DATETIME2 NULL,
        Status               NVARCHAR(50) NOT NULL DEFAULT 'Open',
        CreatedByAdminID     INT NULL FOREIGN KEY REFERENCES AdminUsers(AdminID),
        RequestedByName      NVARCHAR(200) NULL,
        CreatedAt            DATETIME2 NOT NULL DEFAULT GETDATE(),
        ModifiedAt           DATETIME2 NULL,
        ModifiedByAdminID    INT NULL FOREIGN KEY REFERENCES AdminUsers(AdminID)
    )

    -- Collapse old rows sharing the same Department+StartDate+EndDate into a single batch.
    INSERT INTO JobSlots (Department, NumberOfPositions, StartDate, EndDate, Status, CreatedByAdminID, RequestedByName, CreatedAt)
    SELECT
        Department,
        COUNT(*) AS NumberOfPositions,
        StartDate,
        EndDate,
        MIN(Status) AS Status,
        MIN(CreatedByAdminID) AS CreatedByAdminID,
        MIN(RequestedByName) AS RequestedByName,
        MIN(CreatedAt) AS CreatedAt
    FROM JobSlots_Old
    GROUP BY Department, StartDate, EndDate

    PRINT 'Migrated JobSlots to batch schema (old rows kept in JobSlots_Old)'
END

GO
-- Table: JobSlotAssignments
-- One row per applicant assigned into a JobSlots batch (1 batch : many assignments).
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'JobSlotAssignments')
BEGIN
    CREATE TABLE JobSlotAssignments (
        AssignmentID         INT IDENTITY(1,1) PRIMARY KEY,
        SlotID               INT NOT NULL FOREIGN KEY REFERENCES JobSlots(SlotID),
        ApplicantID          INT NULL FOREIGN KEY REFERENCES T_APPLICANTS(ApplicantID), -- NULL when entered manually (not sourced from T_APPLICANTS)
        ManualFirstNameThai  NVARCHAR(200) NULL,
        ManualLastNameThai   NVARCHAR(200) NULL,
        Status               NVARCHAR(50) NOT NULL DEFAULT 'Assigned', -- Assigned / Cancelled
        AssignedDate         DATETIME2 NOT NULL DEFAULT GETDATE(),
        AssignedByAdminID    INT NULL FOREIGN KEY REFERENCES AdminUsers(AdminID),
        ModifiedAt           DATETIME2 NULL,
        ModifiedByAdminID    INT NULL FOREIGN KEY REFERENCES AdminUsers(AdminID)
    )
    PRINT 'Created JobSlotAssignments'
END

-- Migrate existing seat assignments (AssignedApplicantID) from the old table into JobSlotAssignments.
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'JobSlots_Old')
   AND NOT EXISTS (SELECT 1 FROM JobSlotAssignments)
BEGIN
    INSERT INTO JobSlotAssignments (SlotID, ApplicantID, AssignedDate)
    SELECT js.SlotID, o.AssignedApplicantID, o.AssignedDate
    FROM JobSlots_Old o
    INNER JOIN JobSlots js
        ON js.Department = o.Department
        AND ISNULL(js.StartDate, '1900-01-01') = ISNULL(o.StartDate, '1900-01-01')
        AND ISNULL(js.EndDate, '9999-12-31') = ISNULL(o.EndDate, '9999-12-31')
    WHERE o.AssignedApplicantID IS NOT NULL

    PRINT 'Migrated existing seat assignments into JobSlotAssignments'
END

GO
CREATE OR ALTER PROCEDURE sp_GetJobSlotsByDepartment
    @Department NVARCHAR(200) = NULL,
    @Company NVARCHAR(50) = NULL,
    @Month INT = NULL,
    @Year INT = NULL
AS
BEGIN
    DECLARE @WindowStart DATE = NULL, @WindowEnd DATE = NULL;

    IF @Year IS NOT NULL
    BEGIN
        IF @Month IS NOT NULL
        BEGIN
            SET @WindowStart = DATEFROMPARTS(@Year, @Month, 1);
            SET @WindowEnd = EOMONTH(@WindowStart);
        END
        ELSE
        BEGIN
            SET @WindowStart = DATEFROMPARTS(@Year, 1, 1);
            SET @WindowEnd = DATEFROMPARTS(@Year, 12, 31);
        END
    END

    SELECT
        JS.SlotID,
        JS.Department,
        EMP.COMPANY_CODE AS CompanyCode,
        JS.NumberOfPositions,
        JS.StartDate,
        JS.EndDate,
        JS.Status,
        COUNT(A.AssignmentID) AS AssignedCount,
        JS.CreatedByAdminID,
        JS.RequestedByName,
        JS.CreatedAt,
        JS.ModifiedAt
    FROM JobSlots JS

    LEFT JOIN (
        SELECT DISTINCT
            COSTCENT,
            COMPANY_CODE
        FROM [HRMS_LINKED_SERVER].HRMS.dbo.T_EMPLOYEE_SSO
    ) EMP
        ON JS.Department = EMP.COSTCENT
    LEFT JOIN JobSlotAssignments A
        ON A.SlotID = JS.SlotID AND A.Status <> 'Cancelled'
    WHERE (@Department IS NULL OR JS.Department = @Department)
      AND (@Company IS NULL OR EMP.COMPANY_CODE = @Company)
      AND (
            @WindowStart IS NULL
            OR (
                (JS.StartDate IS NULL OR JS.StartDate <= @WindowEnd)
                AND (JS.EndDate IS NULL OR JS.EndDate >= @WindowStart)
            )
          )
    GROUP BY JS.SlotID, JS.Department, EMP.COMPANY_CODE, JS.NumberOfPositions,
             JS.StartDate, JS.EndDate, JS.Status, JS.CreatedByAdminID,
             JS.RequestedByName, JS.CreatedAt, JS.ModifiedAt
    ORDER BY JS.StartDate, JS.EndDate
END

GO
CREATE OR ALTER PROCEDURE sp_GetSlotAssignments
    @SlotID INT
AS
BEGIN
    SELECT
        A.AssignmentID,
        A.SlotID,
        A.ApplicantID,
        COALESCE(APP.FirstNameThai, A.ManualFirstNameThai) AS FirstNameThai,
        COALESCE(APP.LastNameThai, A.ManualLastNameThai) AS LastNameThai,
        A.Status,
        A.AssignedDate
    FROM JobSlotAssignments A
    LEFT JOIN T_APPLICANTS APP ON APP.ApplicantID = A.ApplicantID
    WHERE A.SlotID = @SlotID
      AND A.Status <> 'Cancelled'
    ORDER BY A.AssignedDate
END

GO
CREATE OR ALTER PROCEDURE sp_AddJobSlot
    @Department        NVARCHAR(200),
    @NumberOfPositions INT,
    @StartDate         DATETIME2 = NULL,
    @EndDate           DATETIME2 = NULL,
    @CreatedByAdminID  INT = NULL,
    @RequestedByName   NVARCHAR(200) = NULL
AS
BEGIN
    INSERT INTO JobSlots (Department, NumberOfPositions, StartDate, EndDate, CreatedByAdminID, RequestedByName)
    VALUES (@Department, @NumberOfPositions, @StartDate, @EndDate, @CreatedByAdminID, @RequestedByName)

    SELECT CAST(SCOPE_IDENTITY() AS INT)
END

GO
CREATE OR ALTER PROCEDURE sp_UpdateJobSlot
    @SlotID            INT,
    @NumberOfPositions INT = NULL,
    @StartDate         DATETIME2 = NULL,
    @EndDate           DATETIME2 = NULL,
    @Status            NVARCHAR(50) = NULL,
    @RequestedByName   NVARCHAR(200) = NULL,
    @ModifiedByAdminID INT = NULL
AS
BEGIN
    UPDATE JobSlots
    SET NumberOfPositions = COALESCE(@NumberOfPositions, NumberOfPositions),
        StartDate         = @StartDate,
        EndDate           = @EndDate,
        Status            = COALESCE(@Status, Status),
        RequestedByName   = COALESCE(@RequestedByName, RequestedByName),
        ModifiedAt        = GETDATE(),
        ModifiedByAdminID = COALESCE(@ModifiedByAdminID, ModifiedByAdminID)
    WHERE SlotID = @SlotID

    SELECT SlotID, Department, NumberOfPositions, StartDate, EndDate, Status,
           CreatedByAdminID, RequestedByName, CreatedAt, ModifiedAt, ModifiedByAdminID
    FROM JobSlots
    WHERE SlotID = @SlotID
END

GO
CREATE OR ALTER PROCEDURE sp_DeleteJobSlot
    @SlotID INT
AS
BEGIN
    DELETE FROM JobSlotAssignments WHERE SlotID = @SlotID
    DELETE FROM JobSlots WHERE SlotID = @SlotID
END

GO
CREATE OR ALTER PROCEDURE sp_AssignApplicantToSlot
    @SlotID              INT,
    @ApplicantID         INT = NULL,
    @ManualFirstNameThai NVARCHAR(200) = NULL,
    @ManualLastNameThai  NVARCHAR(200) = NULL,
    @AssignedByAdminID   INT = NULL
AS
BEGIN
    IF @ApplicantID IS NULL AND @ManualFirstNameThai IS NULL
    BEGIN
        RAISERROR('Either ApplicantID or a manual name must be provided.', 16, 1);
        RETURN;
    END

    DECLARE @Capacity INT, @CurrentCount INT;

    SELECT @Capacity = NumberOfPositions FROM JobSlots WHERE SlotID = @SlotID;
    SELECT @CurrentCount = COUNT(*) FROM JobSlotAssignments WHERE SlotID = @SlotID AND Status <> 'Cancelled';

    IF @Capacity IS NULL
    BEGIN
        RAISERROR('Slot not found.', 16, 1);
        RETURN;
    END

    IF @CurrentCount >= @Capacity
    BEGIN
        RAISERROR('This batch is already full.', 16, 1);
        RETURN;
    END

    INSERT INTO JobSlotAssignments (SlotID, ApplicantID, ManualFirstNameThai, ManualLastNameThai, AssignedByAdminID)
    VALUES (@SlotID, @ApplicantID, @ManualFirstNameThai, @ManualLastNameThai, @AssignedByAdminID)

    IF (@CurrentCount + 1) >= @Capacity
    BEGIN
        UPDATE JobSlots SET Status = 'Filled', ModifiedAt = GETDATE() WHERE SlotID = @SlotID
    END
END

GO
CREATE OR ALTER PROCEDURE sp_UnassignApplicantFromSlot
    @AssignmentID INT
AS
BEGIN
    DECLARE @SlotID INT = (SELECT SlotID FROM JobSlotAssignments WHERE AssignmentID = @AssignmentID);

    UPDATE JobSlotAssignments SET Status = 'Cancelled', ModifiedAt = GETDATE() WHERE AssignmentID = @AssignmentID

    UPDATE JobSlots SET Status = 'Open', ModifiedAt = GETDATE()
    WHERE SlotID = @SlotID AND Status = 'Filled'
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
