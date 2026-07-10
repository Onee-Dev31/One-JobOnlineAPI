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
        AssignmentID                  INT IDENTITY(1,1) PRIMARY KEY,
        SlotID                        INT NULL FOREIGN KEY REFERENCES JobSlots(SlotID), -- NULL until an admin assigns this application into a batch
        ApplicationID                 INT NULL FOREIGN KEY REFERENCES JobApplications(ApplicationID), -- which application (job + applicant) this is for; NULL for a pure manual/walk-in entry
        UserID                        INT NULL FOREIGN KEY REFERENCES Users(UserId), -- candidate's login account (Users.UserId), from the auth_token JWT/cookie; NULL for a staff manual/walk-in entry with no candidate login
        ManualTitle                   NVARCHAR(20) NULL,
        ManualFirstNameThai           NVARCHAR(200) NULL,
        ManualLastNameThai            NVARCHAR(200) NULL,
        ManualNickname                NVARCHAR(50) NULL,
        ManualAge                     INT NULL,
        ManualYear                    NVARCHAR(10) NULL, -- ชั้นปีการศึกษา
        ManualGPA                     DECIMAL(3, 2) NULL,
        ManualMajor                   NVARCHAR(200) NULL,
        ManualFaculty                 NVARCHAR(200) NULL,
        ManualUniversity              NVARCHAR(200) NULL,
        ManualInternshipType          NVARCHAR(50) NULL, -- ฝึกงานภาคฤดูร้อน / ฝึกงานตามหลักสูตร / ฝึกงานสหกิจ
        ManualInternStartDate         DATE NULL,
        ManualInternEndDate           DATE NULL,
        ManualDurationMonths          NVARCHAR(20) NULL,
        ManualPreferredPosition       NVARCHAR(100) NULL,
        ManualPreferredPositionBackup NVARCHAR(100) NULL,
        ManualMobilePhone             NVARCHAR(20) NULL,
        ManualEmail                   NVARCHAR(150) NULL,
        ManualCanCommute              BIT NULL, -- สะดวกเดินทางมาทำงานที่ตึกได้หรือไม่
        ManualCanTravelOutside        BIT NULL, -- สะดวกออกกองข้างนอก/ต่างจังหวัดหรือไม่ (Production)
        ManualFlexibleWork            BIT NULL, -- ทำงานแบบยืดหยุ่นได้หรือไม่ (Production)
        ManualReasonForInterest       NVARCHAR(1000) NULL,
        Status                        NVARCHAR(50) NOT NULL DEFAULT 'Assigned', -- Assigned / Cancelled
        AssignedDate                  DATETIME2 NOT NULL DEFAULT GETDATE(),
        AssignedByAdminID             INT NULL FOREIGN KEY REFERENCES AdminUsers(AdminID),
        ModifiedAt                    DATETIME2 NULL,
        ModifiedByAdminID             INT NULL FOREIGN KEY REFERENCES AdminUsers(AdminID)
    )
    PRINT 'Created JobSlotAssignments'
END

-- Table: JobSlotAssignmentFiles
-- Attached files (Resume, CV) for a JobSlotAssignments row. Replaces the old
-- ManualTranscriptUrl/ManualResumeLink/ManualPortfolioLink link fields.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'JobSlotAssignmentFiles')
BEGIN
    CREATE TABLE JobSlotAssignmentFiles (
        FileID          INT IDENTITY(1,1) PRIMARY KEY,
        AssignmentID    INT NOT NULL FOREIGN KEY REFERENCES JobSlotAssignments(AssignmentID),
        FilePath        NVARCHAR(500) NOT NULL,
        FileName        NVARCHAR(300) NOT NULL,
        FileSize        BIGINT NOT NULL,
        FileType        NVARCHAR(100) NULL,
        SectionFile     NVARCHAR(20) NOT NULL, -- resume / cv
        UploadedDate    DATETIME2 NOT NULL DEFAULT GETDATE()
    )
    PRINT 'Created JobSlotAssignmentFiles'
END

-- Drop the old manual link columns (from a previous version of this script) — attachments are now
-- stored as files in JobSlotAssignmentFiles instead of pasted links.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('JobSlotAssignments') AND name = 'ManualTranscriptUrl')
BEGIN
    ALTER TABLE JobSlotAssignments DROP COLUMN ManualTranscriptUrl, ManualResumeLink, ManualPortfolioLink
END

-- Add manual contact fields if they don't exist yet (from a previous version of this script).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('JobSlotAssignments') AND name = 'ManualMobilePhone')
BEGIN
    ALTER TABLE JobSlotAssignments ADD
        ManualMobilePhone NVARCHAR(20) NULL,
        ManualEmail       NVARCHAR(150) NULL
END

-- Drop CitizenID/BirthDate from a previous version of this script — the internship application
-- form (docs.google.com/forms/.../1FAIpQLSesXWC0Hnng3mfc9Rl27VGsITC4XExcnONyOaiSuf0gMcrPIA) collects
-- Age instead, and doesn't ask for a citizen ID at all.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('JobSlotAssignments') AND name = 'ManualCitizenID')
BEGIN
    ALTER TABLE JobSlotAssignments DROP COLUMN ManualCitizenID, ManualBirthDate
END

-- Add the remaining internship-application fields if they don't exist yet (from a previous version of this script).
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('JobSlotAssignments') AND name = 'ManualNickname')
BEGIN
    ALTER TABLE JobSlotAssignments ADD
        ManualTitle                   NVARCHAR(20) NULL,
        ManualNickname                NVARCHAR(50) NULL,
        ManualAge                     INT NULL,
        ManualYear                    NVARCHAR(10) NULL,
        ManualGPA                     DECIMAL(3, 2) NULL,
        ManualMajor                   NVARCHAR(200) NULL,
        ManualFaculty                 NVARCHAR(200) NULL,
        ManualUniversity              NVARCHAR(200) NULL,
        ManualInternshipType          NVARCHAR(50) NULL,
        ManualInternStartDate         DATE NULL,
        ManualInternEndDate           DATE NULL,
        ManualDurationMonths          NVARCHAR(20) NULL,
        ManualPreferredPosition       NVARCHAR(100) NULL,
        ManualPreferredPositionBackup NVARCHAR(100) NULL,
        ManualCanCommute              BIT NULL,
        ManualCanTravelOutside        BIT NULL,
        ManualFlexibleWork            BIT NULL,
        ManualReasonForInterest       NVARCHAR(1000) NULL
END

-- Allow SlotID to be NULL — a JobSlotAssignments row can now be created directly from a trainee's
-- application (JobID set, no batch chosen yet) before an admin assigns it into a JobSlots batch.
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('JobSlotAssignments') AND name = 'SlotID' AND is_nullable = 0
)
BEGIN
    ALTER TABLE JobSlotAssignments ALTER COLUMN SlotID INT NULL
END

-- Replace the direct ApplicantID (-> T_APPLICANTS) and JobID (-> Jobs) columns with a single
-- ApplicationID (-> JobApplications) — an assignment now links through JobApplications, which
-- already tracks JobID and ApplicantID, instead of duplicating that linkage on JobSlotAssignments.
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('JobSlotAssignments') AND name = 'ApplicantID')
BEGIN
    DECLARE @ApplicantFk NVARCHAR(200) = (
        SELECT fk.name FROM sys.foreign_keys fk
        WHERE fk.parent_object_id = OBJECT_ID('JobSlotAssignments')
          AND fk.referenced_object_id = OBJECT_ID('T_APPLICANTS')
    )
    IF @ApplicantFk IS NOT NULL EXEC('ALTER TABLE JobSlotAssignments DROP CONSTRAINT ' + @ApplicantFk)

    ALTER TABLE JobSlotAssignments DROP COLUMN ApplicantID
END

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('JobSlotAssignments') AND name = 'JobID')
BEGIN
    DECLARE @JobFk NVARCHAR(200) = (
        SELECT fk.name FROM sys.foreign_keys fk
        WHERE fk.parent_object_id = OBJECT_ID('JobSlotAssignments')
          AND fk.referenced_object_id = OBJECT_ID('Jobs')
    )
    IF @JobFk IS NOT NULL EXEC('ALTER TABLE JobSlotAssignments DROP CONSTRAINT ' + @JobFk)

    ALTER TABLE JobSlotAssignments DROP COLUMN JobID
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('JobSlotAssignments') AND name = 'ApplicationID')
BEGIN
    ALTER TABLE JobSlotAssignments ADD ApplicationID INT NULL FOREIGN KEY REFERENCES JobApplications(ApplicationID)
END

-- Add UserID to link a row back to the candidate's login account (Users.UserId), captured from the
-- auth_token JWT/cookie at submission time. NULL for staff manual/walk-in entries with no candidate login.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('JobSlotAssignments') AND name = 'UserID')
BEGIN
    ALTER TABLE JobSlotAssignments ADD UserID INT NULL FOREIGN KEY REFERENCES Users(UserId)
END

-- Allow UserID to be NULL — a staff manual/walk-in entry has no candidate login to attach.
IF EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('JobSlotAssignments') AND name = 'UserID' AND is_nullable = 0
)
BEGIN
    ALTER TABLE JobSlotAssignments ALTER COLUMN UserID INT NULL
END

-- Migrate existing seat assignments from the old table into JobSlotAssignments. The old schema has
-- no JobApplications-compatible record for these seats, so the applicant link isn't carried over —
-- only the seat itself (SlotID/AssignedDate) is preserved.
IF EXISTS (SELECT 1 FROM sys.tables WHERE name = 'JobSlots_Old')
   AND NOT EXISTS (SELECT 1 FROM JobSlotAssignments)
BEGIN
    INSERT INTO JobSlotAssignments (SlotID, AssignedDate)
    SELECT js.SlotID, o.AssignedDate
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
        EMP.NAMECOSTCENT AS DepartmentName,
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
            COMPANY_CODE,
            NAMECOSTCENT
        FROM [HRMS_LINKED_SERVER].HRMS.dbo.T_EMPLOYEE_SSO
        WHERE COSTCENT IS NOT NULL
        AND COSTCENT <> ''
        AND NAMECOSTCENT IS NOT NULL
        AND NAMECOSTCENT <> ''
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
    GROUP BY JS.SlotID, JS.Department, EMP.COMPANY_CODE, EMP.NAMECOSTCENT, JS.NumberOfPositions,
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
        JA.JobID,
        JA.ApplicantID,
        A.ManualTitle AS Title,
        A.ManualFirstNameThai AS FirstNameThai,
        A.ManualLastNameThai AS LastNameThai,
        A.ManualNickname AS Nickname,
        A.ManualAge AS Age,
        A.ManualYear AS Year,
        A.ManualGPA AS GPA,
        A.ManualMajor AS Major,
        A.ManualFaculty AS Faculty,
        A.ManualUniversity AS University,
        A.ManualInternshipType AS InternshipType,
        A.ManualInternStartDate AS InternStartDate,
        A.ManualInternEndDate AS InternEndDate,
        A.ManualDurationMonths AS DurationMonths,
        A.ManualPreferredPosition AS PreferredPosition,
        A.ManualPreferredPositionBackup AS PreferredPositionBackup,
        A.ManualMobilePhone AS MobilePhone,
        A.ManualEmail AS Email,
        A.ManualCanCommute AS CanCommute,
        A.ManualCanTravelOutside AS CanTravelOutside,
        A.ManualFlexibleWork AS FlexibleWork,
        A.ManualReasonForInterest AS ReasonForInterest,
        A.Status,
        A.AssignedDate
    FROM JobSlotAssignments A
    LEFT JOIN JobApplications JA ON JA.ApplicationID = A.ApplicationID
    WHERE A.SlotID = @SlotID
      AND A.Status <> 'Cancelled'
    ORDER BY A.AssignedDate

    SELECT
        F.FileID,
        F.AssignmentID,
        F.FilePath,
        F.FileName,
        F.FileSize,
        F.FileType,
        F.SectionFile,
        F.UploadedDate
    FROM JobSlotAssignmentFiles F
    INNER JOIN JobSlotAssignments A ON A.AssignmentID = F.AssignmentID
    WHERE A.SlotID = @SlotID
      AND A.Status <> 'Cancelled'
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
    @SlotID                        INT = NULL, -- NULL only for a direct/self-submitted application with no batch chosen yet
    @ApplicationID                 INT = NULL, -- existing JobApplications row. If it already has a pending JobSlotAssignments row (self-submitted, SlotID still NULL), that row is updated with the chosen SlotID; otherwise a new JobSlotAssignments row is created for it.
    @JobID                         INT = NULL, -- job posting; used when creating a brand-new JobApplications row (no ApplicationID given) — always created with ApplicantID = NULL, not sourced from T_APPLICANTS
    @ManualTitle                   NVARCHAR(20) = NULL,
    @ManualFirstNameThai           NVARCHAR(200) = NULL,
    @ManualLastNameThai            NVARCHAR(200) = NULL,
    @ManualNickname                NVARCHAR(50) = NULL,
    @ManualAge                     INT = NULL,
    @ManualYear                    NVARCHAR(10) = NULL,
    @ManualGPA                     DECIMAL(3, 2) = NULL,
    @ManualMajor                   NVARCHAR(200) = NULL,
    @ManualFaculty                 NVARCHAR(200) = NULL,
    @ManualUniversity              NVARCHAR(200) = NULL,
    @ManualInternshipType          NVARCHAR(50) = NULL,
    @ManualInternStartDate         DATE = NULL,
    @ManualInternEndDate           DATE = NULL,
    @ManualDurationMonths          NVARCHAR(20) = NULL,
    @ManualPreferredPosition       NVARCHAR(100) = NULL,
    @ManualPreferredPositionBackup NVARCHAR(100) = NULL,
    @ManualMobilePhone             NVARCHAR(20) = NULL,
    @ManualEmail                   NVARCHAR(150) = NULL,
    @ManualCanCommute              BIT = NULL,
    @ManualCanTravelOutside        BIT = NULL,
    @ManualFlexibleWork            BIT = NULL,
    @ManualReasonForInterest       NVARCHAR(1000) = NULL,
    @AssignedByAdminID             INT = NULL,
    @UserID                        INT = NULL -- candidate's Users.UserId (auth_token JWT/cookie); NULL for a staff manual/walk-in entry
AS
BEGIN
    DECLARE @ExistingAssignmentID INT = NULL, @ExistingSlotID INT = NULL;

    IF @ApplicationID IS NOT NULL
    BEGIN
        SELECT TOP 1 @ExistingAssignmentID = AssignmentID, @ExistingSlotID = SlotID
        FROM JobSlotAssignments
        WHERE ApplicationID = @ApplicationID AND Status <> 'Cancelled'
    END

    IF @ExistingAssignmentID IS NOT NULL AND @ExistingSlotID IS NOT NULL
    BEGIN
        RAISERROR('This application has already been assigned to a batch.', 16, 1);
        RETURN;
    END

    IF @ExistingAssignmentID IS NOT NULL
    BEGIN
        -- Existing self-submitted application (JobSlotAssignments row already there, SlotID still NULL):
        -- just place it into the chosen batch, don't touch its data.
        IF @SlotID IS NULL
        BEGIN
            RAISERROR('SlotID is required to place an existing assignment into a batch.', 16, 1);
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

        UPDATE JobSlotAssignments
        SET SlotID = @SlotID, ModifiedAt = GETDATE(), ModifiedByAdminID = @AssignedByAdminID
        WHERE AssignmentID = @ExistingAssignmentID

        IF (@CurrentCount + 1) >= @Capacity
        BEGIN
            UPDATE JobSlots SET Status = 'Filled', ModifiedAt = GETDATE() WHERE SlotID = @SlotID
        END

        SELECT @ExistingAssignmentID AS AssignmentID
        RETURN
    END

    IF @ApplicationID IS NULL AND @ManualFirstNameThai IS NULL
    BEGIN
        RAISERROR('Either ApplicationID or a manual name must be provided.', 16, 1);
        RETURN;
    END

    DECLARE @Capacity2 INT, @CurrentCount2 INT;

    IF @SlotID IS NOT NULL
    BEGIN
        SELECT @Capacity2 = NumberOfPositions FROM JobSlots WHERE SlotID = @SlotID;
        SELECT @CurrentCount2 = COUNT(*) FROM JobSlotAssignments WHERE SlotID = @SlotID AND Status <> 'Cancelled';

        IF @Capacity2 IS NULL
        BEGIN
            RAISERROR('Slot not found.', 16, 1);
            RETURN;
        END

        IF @CurrentCount2 >= @Capacity2
        BEGIN
            RAISERROR('This batch is already full.', 16, 1);
            RETURN;
        END
    END

    IF @ApplicationID IS NULL
    BEGIN
        -- No existing application to reuse: record one directly. A SlotID chosen by an admin means
        -- this is a manual placement straight into a batch (Employment confirm); no SlotID means a
        -- self-submitted application still awaiting review (pending).
        INSERT INTO JobApplications (ApplicantID, JobID, Status, SubmissionDate)
        VALUES (NULL, @JobID, IIF(@SlotID IS NOT NULL, 'Employment confirm', 'pending'), GETDATE())

        SET @ApplicationID = CAST(SCOPE_IDENTITY() AS INT)
    END

    INSERT INTO JobSlotAssignments (
        SlotID, ApplicationID, ManualTitle, ManualFirstNameThai, ManualLastNameThai, ManualNickname,
        ManualAge, ManualYear, ManualGPA, ManualMajor, ManualFaculty, ManualUniversity,
        ManualInternshipType, ManualInternStartDate, ManualInternEndDate, ManualDurationMonths,
        ManualPreferredPosition, ManualPreferredPositionBackup, ManualMobilePhone, ManualEmail,
        ManualCanCommute, ManualCanTravelOutside, ManualFlexibleWork,
        ManualReasonForInterest,
        AssignedByAdminID, UserID
    )
    VALUES (
        @SlotID, @ApplicationID, @ManualTitle, @ManualFirstNameThai, @ManualLastNameThai, @ManualNickname,
        @ManualAge, @ManualYear, @ManualGPA, @ManualMajor, @ManualFaculty, @ManualUniversity,
        @ManualInternshipType, @ManualInternStartDate, @ManualInternEndDate, @ManualDurationMonths,
        @ManualPreferredPosition, @ManualPreferredPositionBackup, @ManualMobilePhone, @ManualEmail,
        @ManualCanCommute, @ManualCanTravelOutside, @ManualFlexibleWork,
        @ManualReasonForInterest,
        @AssignedByAdminID, @UserID
    )

    DECLARE @NewAssignmentID INT = CAST(SCOPE_IDENTITY() AS INT);

    IF @SlotID IS NOT NULL AND (@CurrentCount2 + 1) >= @Capacity2
    BEGIN
        UPDATE JobSlots SET Status = 'Filled', ModifiedAt = GETDATE() WHERE SlotID = @SlotID
    END

    SELECT @NewAssignmentID AS AssignmentID
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

GO
-- Dead: no reachable code path ever creates a JobApplications row with ApplicantID populated,
-- so this always returned zero rows. Trainee candidates come only from JobSlotAssignments now —
-- see sp_GetUnassignedTraineeAssignments.
IF OBJECT_ID('sp_GetTraineeSlotCandidates', 'P') IS NOT NULL
BEGIN
    DROP PROCEDURE sp_GetTraineeSlotCandidates
END

GO
-- Self-submitted trainee applications already sitting in JobSlotAssignments (SlotID still NULL)
-- waiting for an admin to place them into a batch via ApplicationID.
CREATE OR ALTER PROCEDURE sp_GetUnassignedTraineeAssignments
    @Department NVARCHAR(200) = NULL
AS
BEGIN
    SELECT
        A.AssignmentID,
        A.ApplicationID,
        JA.JobID,
        J.JobTitle,
        J.Department,
        JA.Status AS ApplicationStatus,
        JA.SubmissionDate,
        A.ManualTitle AS Title,
        A.ManualFirstNameThai AS FirstNameThai,
        A.ManualLastNameThai AS LastNameThai,
        A.ManualNickname AS Nickname,
        A.ManualAge AS Age,
        A.ManualYear AS Year,
        A.ManualGPA AS GPA,
        A.ManualMajor AS Major,
        A.ManualFaculty AS Faculty,
        A.ManualUniversity AS University,
        A.ManualInternshipType AS InternshipType,
        A.ManualInternStartDate AS InternStartDate,
        A.ManualInternEndDate AS InternEndDate,
        A.ManualDurationMonths AS DurationMonths,
        A.ManualPreferredPosition AS PreferredPosition,
        A.ManualPreferredPositionBackup AS PreferredPositionBackup,
        A.ManualMobilePhone AS MobilePhone,
        A.ManualEmail AS Email,
        A.ManualCanCommute AS CanCommute,
        A.ManualCanTravelOutside AS CanTravelOutside,
        A.ManualFlexibleWork AS FlexibleWork,
        A.ManualReasonForInterest AS ReasonForInterest
    FROM JobSlotAssignments A
    LEFT JOIN JobApplications JA ON JA.ApplicationID = A.ApplicationID
    LEFT JOIN Jobs J ON J.JobID = JA.JobID
    WHERE A.SlotID IS NULL
      AND A.Status <> 'Cancelled'
      AND J.EmployeeType = N'นักศึกษาฝึกงาน'
      AND JA.Status = 'Employment confirm'
      AND (@Department IS NULL OR J.Department = @Department)
    ORDER BY JA.SubmissionDate
END

GO
CREATE OR ALTER PROCEDURE sp_AddJobSlotAssignmentFile
    @AssignmentID INT,
    @FilePath     NVARCHAR(500),
    @FileName     NVARCHAR(300),
    @FileSize     BIGINT,
    @FileType     NVARCHAR(100) = NULL,
    @SectionFile  NVARCHAR(20)
AS
BEGIN
    INSERT INTO JobSlotAssignmentFiles (AssignmentID, FilePath, FileName, FileSize, FileType, SectionFile)
    VALUES (@AssignmentID, @FilePath, @FileName, @FileSize, @FileType, @SectionFile)
END

GO
-- Single candidate detail for a job, from whichever pipeline it came from:
-- ApplicantID given -> general pipeline (T_APPLICANTS). ApplicantID NULL -> manual/self-submit
-- pipeline (JobSlotAssignments). Pass @ApplicationID too when JobID alone could match more than
-- one manual application (e.g. several self-submits for the same JobID) — without it, the most
-- recent submission wins.
CREATE OR ALTER PROCEDURE sp_GetApplicationDetail
    @JobID          INT,
    @ApplicantID    INT = NULL,
    @ApplicationID  INT = NULL
AS
BEGIN
    IF @ApplicantID IS NOT NULL
    BEGIN
        SELECT TOP 1
            JA.ApplicationID,
            JA.JobID,
            JA.ApplicantID,
            JA.Status,
            JA.SubmissionDate,
            APP.Title,
            APP.FirstNameThai,
            APP.LastNameThai,
            APP.FirstNameEng,
            APP.LastNameEng,
            APP.Nickname,
            APP.MobilePhone,
            APP.Email
        FROM JobApplications JA
        INNER JOIN T_APPLICANTS APP ON APP.ApplicantID = JA.ApplicantID
        WHERE JA.ApplicantID = @ApplicantID
          AND JA.JobID = @JobID
          AND (@ApplicationID IS NULL OR JA.ApplicationID = @ApplicationID)
        ORDER BY JA.SubmissionDate DESC
    END
    ELSE
    BEGIN
        SELECT TOP 1
            JA.ApplicationID,
            JA.JobID,
            JA.ApplicantID,
            JA.Status,
            JA.SubmissionDate,
            A.ManualTitle AS Title,
            A.ManualFirstNameThai AS FirstNameThai,
            A.ManualLastNameThai AS LastNameThai,
            CAST(NULL AS NVARCHAR(100)) AS FirstNameEng,
            CAST(NULL AS NVARCHAR(100)) AS LastNameEng,
            A.ManualNickname AS Nickname,
            A.ManualMobilePhone AS MobilePhone,
            A.ManualEmail AS Email
        FROM JobApplications JA
        INNER JOIN JobSlotAssignments A ON A.ApplicationID = JA.ApplicationID
        WHERE JA.ApplicantID IS NULL
          AND JA.JobID = @JobID
          AND (@ApplicationID IS NULL OR JA.ApplicationID = @ApplicationID)
          AND A.Status <> 'Cancelled'
        ORDER BY JA.SubmissionDate DESC
    END
END
