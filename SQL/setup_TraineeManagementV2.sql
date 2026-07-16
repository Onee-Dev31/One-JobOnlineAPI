-- Trainee intake management v2: department-fixed-quota model, running in parallel to the
-- existing JobSlots/JobSlotAssignments (time-boxed batch) system. Neither JobSlots,
-- JobSlotAssignments, nor any procedure that reads them (sp_GetCandidateAllForJobsV2,
-- usp_TraineeApplicant_Upsert, sp_GetJobSlotAssignmentSeedData, sp_GetDepartmentDashboard) is
-- touched here — the old system keeps working untouched while this one is built out and tested.
-- No data migration from the old tables happens in this script; that is a separate follow-up
-- once this new system is verified end-to-end.

-- Table: TraineeQuota
-- Fixed headcount per (CompanyCode, DepartmentCode) — not tied to any time window, unlike the
-- old JobSlots.NumberOfPositions which was set per recruiting batch. DepartmentCode matches
-- HRMS_LINKED_SERVER..T_EMPLOYEE_SSO.COSTCENT (the same value JobSlots.Department stores) —
-- there is no local Department table in this DB to add a quota column to instead.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TraineeQuota')
BEGIN
    CREATE TABLE TraineeQuota (
        QuotaID              INT IDENTITY(1,1) PRIMARY KEY,
        CompanyCode           NVARCHAR(50)  NOT NULL,
        DepartmentCode        NVARCHAR(200) NOT NULL,
        DepartmentName        NVARCHAR(400) NULL, -- cached NAMECOSTCENT snapshot
        Quota                 INT NOT NULL DEFAULT 0,
        IsAcceptingTrainees   BIT NOT NULL DEFAULT 1,
        Notes                 NVARCHAR(500) NULL,
        CreatedAt             DATETIME2 NOT NULL DEFAULT GETDATE(),
        CreatedByAdminID      INT NULL FOREIGN KEY REFERENCES AdminUsers(AdminID),
        ModifiedAt            DATETIME2 NULL,
        ModifiedByAdminID     INT NULL FOREIGN KEY REFERENCES AdminUsers(AdminID),
        CONSTRAINT UQ_TraineeQuota_Company_Dept UNIQUE (CompanyCode, DepartmentCode)
    )
    PRINT 'Created TraineeQuota'
END

GO
-- Table: TraineeAssignments
-- Replaces JobSlotAssignments for the new flow: no SlotID, tied directly to a department.
-- CompanyCode/DepartmentCode stay NULL for a self-submitted application still awaiting an admin
-- to place it into a department (mirrors the old SlotID-NULL "pending" state).
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TraineeAssignments')
BEGIN
    CREATE TABLE TraineeAssignments (
        AssignmentID                  INT IDENTITY(1,1) PRIMARY KEY,
        CompanyCode                   NVARCHAR(50) NULL,
        DepartmentCode                NVARCHAR(200) NULL,
        ApplicationID                 INT NULL FOREIGN KEY REFERENCES JobApplications(ApplicationID),
        UserID                        INT NULL FOREIGN KEY REFERENCES Users(UserId),
        ManualTitle                   NVARCHAR(20) NULL,
        ManualFirstNameThai           NVARCHAR(200) NULL,
        ManualLastNameThai            NVARCHAR(200) NULL,
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
        ManualMobilePhone             NVARCHAR(20) NULL,
        ManualEmail                   NVARCHAR(150) NULL,
        ManualCanCommute              BIT NULL,
        ManualCanTravelOutside        BIT NULL,
        ManualFlexibleWork            BIT NULL,
        ManualReasonForInterest       NVARCHAR(1000) NULL,
        Status                        NVARCHAR(50) NOT NULL DEFAULT 'Assigned', -- Assigned / Cancelled
        AssignedDate                  DATETIME2 NOT NULL DEFAULT GETDATE(),
        AssignedByAdminID             INT NULL FOREIGN KEY REFERENCES AdminUsers(AdminID),
        ModifiedAt                    DATETIME2 NULL,
        ModifiedByAdminID             INT NULL FOREIGN KEY REFERENCES AdminUsers(AdminID),
        -- Composite FK is only enforced once both columns are non-null, so a pending
        -- (CompanyCode/DepartmentCode both NULL) row is unaffected.
        CONSTRAINT FK_TraineeAssignments_Quota
            FOREIGN KEY (CompanyCode, DepartmentCode) REFERENCES TraineeQuota (CompanyCode, DepartmentCode)
    )
    PRINT 'Created TraineeAssignments'
END

GO
-- Table: TraineeAssignmentFiles
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TraineeAssignmentFiles')
BEGIN
    CREATE TABLE TraineeAssignmentFiles (
        FileID          INT IDENTITY(1,1) PRIMARY KEY,
        AssignmentID    INT NOT NULL FOREIGN KEY REFERENCES TraineeAssignments(AssignmentID),
        FilePath        NVARCHAR(500) NOT NULL,
        FileName        NVARCHAR(300) NOT NULL,
        FileSize        BIGINT NOT NULL,
        FileType        NVARCHAR(100) NULL,
        SectionFile     NVARCHAR(20) NOT NULL,
        UploadedDate    DATETIME2 NOT NULL DEFAULT GETDATE()
    )
    PRINT 'Created TraineeAssignmentFiles'
END

GO
-- DEPRECATED (TraineeQuota retired): Quota/IsAcceptingTrainees in both overview endpoints are now
-- derived from live Job postings (vw_TraineeDepartmentRequired), not this table. This proc still
-- writes TraineeQuota — harmless, just no longer read by anything. Left in place, not removed, in
-- case something external still calls GET/PUT/DELETE /Trainee/quota; candidate for removal once
-- confirmed unused.
CREATE OR ALTER PROCEDURE sp_UpsertTraineeQuota
    @CompanyCode          NVARCHAR(50),
    @DepartmentCode       NVARCHAR(200),
    @DepartmentName       NVARCHAR(400) = NULL,
    @Quota                INT,
    @IsAcceptingTrainees  BIT = 1,
    @Notes                NVARCHAR(500) = NULL,
    @AdminID              INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (SELECT 1 FROM TraineeQuota WHERE CompanyCode = @CompanyCode AND DepartmentCode = @DepartmentCode)
    BEGIN
        UPDATE TraineeQuota
        SET DepartmentName = COALESCE(@DepartmentName, DepartmentName),
            Quota = @Quota,
            IsAcceptingTrainees = @IsAcceptingTrainees,
            Notes = @Notes,
            ModifiedAt = GETDATE(),
            ModifiedByAdminID = @AdminID
        WHERE CompanyCode = @CompanyCode AND DepartmentCode = @DepartmentCode
    END
    ELSE
    BEGIN
        INSERT INTO TraineeQuota (CompanyCode, DepartmentCode, DepartmentName, Quota, IsAcceptingTrainees, Notes, CreatedByAdminID)
        VALUES (@CompanyCode, @DepartmentCode, @DepartmentName, @Quota, @IsAcceptingTrainees, @Notes, @AdminID)
    END

    SELECT QuotaID, CompanyCode, DepartmentCode, DepartmentName, Quota, IsAcceptingTrainees, Notes,
           CreatedAt, CreatedByAdminID, ModifiedAt, ModifiedByAdminID
    FROM TraineeQuota
    WHERE CompanyCode = @CompanyCode AND DepartmentCode = @DepartmentCode
END

GO
-- DEPRECATED (TraineeQuota retired) — see sp_UpsertTraineeQuota above.
CREATE OR ALTER PROCEDURE sp_DeleteTraineeQuota
    @CompanyCode    NVARCHAR(50),
    @DepartmentCode NVARCHAR(200)
AS
BEGIN
    IF EXISTS (
        SELECT 1 FROM TraineeAssignments
        WHERE CompanyCode = @CompanyCode AND DepartmentCode = @DepartmentCode AND Status <> 'Cancelled'
    )
    BEGIN
        RAISERROR('มี assignment ที่ยัง active อยู่ในแผนกนี้ ลบโควตาไม่ได้', 16, 1);
        RETURN;
    END

    DELETE FROM TraineeQuota WHERE CompanyCode = @CompanyCode AND DepartmentCode = @DepartmentCode
END

GO
-- DEPRECATED (TraineeQuota retired) — see sp_UpsertTraineeQuota above. Still returns whatever rows
-- happen to be in TraineeQuota (frozen at whatever admins last saved); no longer what either
-- overview endpoint's Quota/Required actually reflects.
--
-- Lists every company/department that exists in HRMS (so unconfigured departments still show up,
-- with Quota = 0 / IsAcceptingTrainees = 1 as defaults), LEFT JOINed to the local quota row —
-- same "show everything, not just configured rows" approach as sp_GetTraineeOverview's dept list.
CREATE OR ALTER PROCEDURE sp_GetTraineeQuota
    @CompanyCode NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        Q.QuotaID,
        EMP.COMPANY_CODE AS CompanyCode,
        EMP.COSTCENT AS DepartmentCode,
        COALESCE(Q.DepartmentName, EMP.NAMECOSTCENT) AS DepartmentName,
        ISNULL(Q.Quota, 0) AS Quota,
        ISNULL(Q.IsAcceptingTrainees, 1) AS IsAcceptingTrainees,
        Q.Notes,
        Q.CreatedAt,
        Q.CreatedByAdminID,
        Q.ModifiedAt,
        Q.ModifiedByAdminID
    FROM (
        SELECT DISTINCT COSTCENT, COMPANY_CODE, NAMECOSTCENT
        FROM [HRMS_LINKED_SERVER].HRMS.dbo.T_EMPLOYEE_SSO
        WHERE COSTCENT IS NOT NULL AND COSTCENT <> ''
          AND NAMECOSTCENT IS NOT NULL AND NAMECOSTCENT <> ''
    ) EMP
    LEFT JOIN TraineeQuota Q
        ON Q.CompanyCode = EMP.COMPANY_CODE AND Q.DepartmentCode = EMP.COSTCENT
    WHERE (@CompanyCode IS NULL OR EMP.COMPANY_CODE = @CompanyCode)
    ORDER BY EMP.COMPANY_CODE, EMP.NAMECOSTCENT
END

GO
-- Result set 1: every department that exists in HRMS (so the landing page shows everything
-- immediately, not just departments with an open trainee posting). Quota/IsAcceptingTrainees are
-- derived from live Job postings via vw_TraineeDepartmentRequired (TraineeQuota is retired — see
-- sp_UpsertTraineeQuota below), same source sp_GetTraineeManagementOverview uses. Result set 2:
-- every active assignment already placed into a department.
CREATE OR ALTER PROCEDURE sp_GetTraineeOverview
    @Year INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        EMP.COMPANY_CODE AS CompanyCode,
        COALESCE(CI.Company_nameth, CI.company_name, EMP.COMPANY_CODE) AS CompanyName,
        EMP.COSTCENT AS DepartmentCode,
        EMP.NAMECOSTCENT AS DepartmentName,
        ISNULL(R.Required, 0) AS Quota,
        CASE WHEN ISNULL(R.OpenJobCount, 0) > 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsAcceptingTrainees
    FROM (
        SELECT DISTINCT COSTCENT, COMPANY_CODE, NAMECOSTCENT
        FROM [HRMS_LINKED_SERVER].HRMS.dbo.T_EMPLOYEE_SSO
        WHERE COSTCENT IS NOT NULL AND COSTCENT <> ''
          AND NAMECOSTCENT IS NOT NULL AND NAMECOSTCENT <> ''
    ) EMP
    LEFT JOIN vw_TraineeDepartmentRequired R
        ON R.CompanyCode = EMP.COMPANY_CODE AND R.DepartmentCode = EMP.COSTCENT
    LEFT JOIN CompanyInfo CI
        ON CI.Company_Code = EMP.COMPANY_CODE
    ORDER BY EMP.COMPANY_CODE, EMP.NAMECOSTCENT

    SELECT
        A.AssignmentID,
        A.CompanyCode,
        A.DepartmentCode,
        A.ApplicationID,
        JA.ApplicantID,
        A.ManualTitle AS Title,
        A.ManualFirstNameThai AS FirstNameThai,
        A.ManualLastNameThai AS LastNameThai,
        A.ManualInternStartDate AS InternStartDate,
        A.ManualInternEndDate AS InternEndDate,
        A.Status,
        A.AssignedDate
    FROM TraineeAssignments A
    LEFT JOIN JobApplications JA ON JA.ApplicationID = A.ApplicationID
    WHERE A.Status <> 'Cancelled'
      AND A.CompanyCode IS NOT NULL AND A.DepartmentCode IS NOT NULL
      AND (
            @Year IS NULL
            OR YEAR(A.ManualInternStartDate) = @Year
            OR YEAR(A.ManualInternEndDate) = @Year
          )
    ORDER BY A.ManualInternStartDate
END

GO
-- Creates a new assignment (CompanyCode/DepartmentCode given) or places an existing pending
-- self-submitted one (matched by ApplicationID, CompanyCode/DepartmentCode both still NULL) into
-- a department. Quota is enforced as a soft warning: exceeding it does not block the call unless
-- @ForceOverQuota = 0, in which case it raises an error the caller can retry with the flag set
-- after the admin confirms. "Exceeding" is computed per overlapping date range within the
-- department, not a running total, since each assignment now carries its own start/end dates.
CREATE OR ALTER PROCEDURE sp_CreateOrPlaceTraineeAssignment
    @CompanyCode                   NVARCHAR(50) = NULL,  -- NULL only for a direct/self-submitted application with no department chosen yet
    @DepartmentCode                NVARCHAR(200) = NULL,
    @ApplicationID                 INT = NULL,
    @JobID                         INT = NULL,
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
    @UserID                        INT = NULL,
    @ForceOverQuota                BIT = 0
AS
BEGIN
    DECLARE @ExistingAssignmentID INT = NULL, @ExistingCompanyCode NVARCHAR(50) = NULL, @ExistingDepartmentCode NVARCHAR(200) = NULL;

    IF @ApplicationID IS NOT NULL
    BEGIN
        SELECT TOP 1 @ExistingAssignmentID = AssignmentID, @ExistingCompanyCode = CompanyCode, @ExistingDepartmentCode = DepartmentCode
        FROM TraineeAssignments
        WHERE ApplicationID = @ApplicationID AND Status <> 'Cancelled'
    END

    IF @ExistingAssignmentID IS NOT NULL AND @ExistingCompanyCode IS NOT NULL AND @ExistingDepartmentCode IS NOT NULL
    BEGIN
        RAISERROR('Application ใบนี้ถูกมอบหมายให้แผนกไปแล้ว', 16, 1);
        RETURN;
    END

    DECLARE @Quota INT = NULL, @ActiveOverlapCount INT = 0, @IsOverQuota BIT = 0;

    IF @CompanyCode IS NOT NULL AND @DepartmentCode IS NOT NULL
    BEGIN
        IF @ManualInternStartDate IS NULL OR @ManualInternEndDate IS NULL
        BEGIN
            RAISERROR('ต้องระบุวันที่เริ่ม-สิ้นสุดฝึกงาน ก่อนมอบหมายเข้าแผนก', 16, 1);
            RETURN;
        END

        -- Quota is derived from live Job postings (vw_TraineeDepartmentRequired), not a
        -- pre-configured TraineeQuota row (retired) — a department with no qualifying posting
        -- yet just resolves to 0, same soft-warn-only behavior as sp_CreateTraineeManagementAssignment.
        SELECT @Quota = Required FROM vw_TraineeDepartmentRequired WHERE CompanyCode = @CompanyCode AND DepartmentCode = @DepartmentCode;
        SET @Quota = ISNULL(@Quota, 0);

        SELECT @ActiveOverlapCount = COUNT(*)
        FROM TraineeAssignments
        WHERE CompanyCode = @CompanyCode AND DepartmentCode = @DepartmentCode
          AND Status <> 'Cancelled'
          AND (@ExistingAssignmentID IS NULL OR AssignmentID <> @ExistingAssignmentID)
          AND ManualInternStartDate <= @ManualInternEndDate
          AND ManualInternEndDate >= @ManualInternStartDate;

        IF (@ActiveOverlapCount + 1) > @Quota
        BEGIN
            SET @IsOverQuota = 1;
            IF @ForceOverQuota = 0
            BEGIN
                RAISERROR('เกินโควตาของแผนกในช่วงเวลานี้ (มอบหมายแล้ว %d จากโควตา %d คน) ส่ง ForceOverQuota เพื่อยืนยันรับเกินโควตา', 16, 1, @ActiveOverlapCount, @Quota);
                RETURN;
            END
        END
    END

    IF @ExistingAssignmentID IS NOT NULL
    BEGIN
        -- Existing self-submitted application (no department chosen yet): place it, don't touch its data.
        UPDATE TraineeAssignments
        SET CompanyCode = @CompanyCode, DepartmentCode = @DepartmentCode,
            ModifiedAt = GETDATE(), ModifiedByAdminID = @AssignedByAdminID
        WHERE AssignmentID = @ExistingAssignmentID

        SELECT @ExistingAssignmentID AS AssignmentID, @IsOverQuota AS IsOverQuota, @ActiveOverlapCount AS ActiveOverlapCount, @Quota AS Quota
        RETURN
    END

    IF @ApplicationID IS NULL AND @ManualFirstNameThai IS NULL
    BEGIN
        RAISERROR('ต้องมี ApplicationID หรือกรอกชื่อ Manual อย่างใดอย่างหนึ่ง', 16, 1);
        RETURN;
    END

    IF @ApplicationID IS NULL
    BEGIN
        INSERT INTO JobApplications (ApplicantID, JobID, Status, SubmissionDate)
        VALUES (NULL, @JobID, IIF(@CompanyCode IS NOT NULL, 'Employment confirm', 'pending'), GETDATE())

        SET @ApplicationID = CAST(SCOPE_IDENTITY() AS INT)
    END

    INSERT INTO TraineeAssignments (
        CompanyCode, DepartmentCode, ApplicationID, ManualTitle, ManualFirstNameThai, ManualLastNameThai, ManualNickname,
        ManualAge, ManualYear, ManualGPA, ManualMajor, ManualFaculty, ManualUniversity,
        ManualInternshipType, ManualInternStartDate, ManualInternEndDate, ManualDurationMonths,
        ManualPreferredPosition, ManualPreferredPositionBackup, ManualMobilePhone, ManualEmail,
        ManualCanCommute, ManualCanTravelOutside, ManualFlexibleWork,
        ManualReasonForInterest,
        AssignedByAdminID, UserID
    )
    VALUES (
        @CompanyCode, @DepartmentCode, @ApplicationID, @ManualTitle, @ManualFirstNameThai, @ManualLastNameThai, @ManualNickname,
        @ManualAge, @ManualYear, @ManualGPA, @ManualMajor, @ManualFaculty, @ManualUniversity,
        @ManualInternshipType, @ManualInternStartDate, @ManualInternEndDate, @ManualDurationMonths,
        @ManualPreferredPosition, @ManualPreferredPositionBackup, @ManualMobilePhone, @ManualEmail,
        @ManualCanCommute, @ManualCanTravelOutside, @ManualFlexibleWork,
        @ManualReasonForInterest,
        @AssignedByAdminID, @UserID
    )

    DECLARE @NewAssignmentID INT = CAST(SCOPE_IDENTITY() AS INT);

    SELECT @NewAssignmentID AS AssignmentID, @IsOverQuota AS IsOverQuota, @ActiveOverlapCount AS ActiveOverlapCount, @Quota AS Quota
END

GO
-- Returns an assignment to the unassigned pool (mirrors sp_UnassignApplicantFromSlot's current
-- behavior: clears the placement rather than cancelling the record outright).
CREATE OR ALTER PROCEDURE sp_UnassignTraineeAssignment
    @AssignmentID INT
AS
BEGIN
    UPDATE TraineeAssignments
    SET Status = 'Assigned', CompanyCode = NULL, DepartmentCode = NULL, ModifiedAt = GETDATE()
    WHERE AssignmentID = @AssignmentID
END

GO
-- Self-submitted trainee applications sitting in TraineeAssignments with no department chosen yet.
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
        A.ManualReasonForInterest AS ReasonForInterest,
        (
            SELECT F.FileID, F.FilePath, F.FileName, F.FileSize, F.FileType, F.SectionFile, F.UploadedDate
            FROM TraineeAssignmentFiles F
            WHERE F.AssignmentID = A.AssignmentID
            FOR JSON PATH
        ) AS FilesList
    FROM TraineeAssignments A
    LEFT JOIN JobApplications JA ON JA.ApplicationID = A.ApplicationID
    LEFT JOIN Jobs J ON J.JobID = JA.JobID
    WHERE A.DepartmentCode IS NULL
      AND A.Status <> 'Cancelled'
      AND J.EmployeeType = N'นักศึกษาฝึกงาน'
      AND JA.Status = 'Employment confirm'
      AND (@Department IS NULL OR J.Department = @Department)
    ORDER BY JA.SubmissionDate
END

GO
CREATE OR ALTER PROCEDURE sp_AddTraineeAssignmentFile
    @AssignmentID INT,
    @FilePath     NVARCHAR(500),
    @FileName     NVARCHAR(300),
    @FileSize     BIGINT,
    @FileType     NVARCHAR(100) = NULL,
    @SectionFile  NVARCHAR(20)
AS
BEGIN
    INSERT INTO TraineeAssignmentFiles (AssignmentID, FilePath, FileName, FileSize, FileType, SectionFile)
    VALUES (@AssignmentID, @FilePath, @FileName, @FileSize, @FileType, @SectionFile)
END

GO
-- Read-only: prefill for a Part2-style continuation form from a TraineeAssignments row.
-- Not wired to usp_TraineeApplicant_Upsert yet (that proc still only reads JobSlotAssignments) —
-- kept here for parity so the new controller has the same surface as the old one.
CREATE OR ALTER PROCEDURE sp_GetTraineeAssignmentSeedData
    @AssignmentID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        A.AssignmentID,
        A.ManualTitle                   AS PrefixT,
        A.ManualFirstNameThai           AS NameFirstT,
        A.ManualLastNameThai            AS NameLastT,
        A.ManualNickname                AS NicknameT,
        A.ManualMobilePhone             AS Mobile,
        A.ManualEmail                   AS Email,
        A.ManualYear                    AS YearOfStudy,
        A.ManualMajor                   AS Major,
        A.ManualFaculty                 AS Faculty,
        A.ManualUniversity              AS School,
        A.ManualInternStartDate         AS StartDate,
        A.ManualInternEndDate           AS EndDate,
        A.ManualPreferredPosition       AS DesiredField1,
        A.ManualPreferredPositionBackup AS DesiredField2,
        A.ManualInternshipType          AS InternshipType
    FROM TraineeAssignments A
    WHERE A.AssignmentID = @AssignmentID

    SELECT
        F.FileID, F.AssignmentID, F.FilePath, F.FileName, F.FileSize, F.FileType, F.SectionFile, F.UploadedDate
    FROM TraineeAssignmentFiles F
    WHERE F.AssignmentID = @AssignmentID
END
GO

-- ============================================================================
-- Procs below back the /admin/trainee-management frontend page's exact
-- CompanyGroup[]/DepartmentGroup/Trainee contract (Controllers/TraineeManagementController.cs).
-- Same TraineeQuota/TraineeAssignments tables as above — no new tables, this is just a
-- purpose-shaped read/write surface for that one page.
-- ============================================================================

-- Required/IsOpen no longer come from the manually-edited TraineeQuota table (deprecated below —
-- see sp_UpsertTraineeQuotaByDepartment). Instead they're derived from live Job postings: "required"
-- per department = SUM(NumberOfPositions) of Jobs in the "นักศึกษาฝึกงาน" JobGroup that are still
-- open for intake; "isOpen" = at least one such posting exists. TraineeAssignments no longer needs a
-- TraineeQuota row to exist before it can be inserted, so the old composite FK is dropped: with
-- TraineeQuota no longer populated going forward, that FK would otherwise reject every new
-- assignment. TraineeQuota itself is left in place (not dropped) — historical data, harmless if unused.
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_TraineeAssignments_Quota')
BEGIN
    ALTER TABLE TraineeAssignments DROP CONSTRAINT FK_TraineeAssignments_Quota
    PRINT 'Dropped FK_TraineeAssignments_Quota'
END

GO
-- One row per (CompanyCode, DepartmentCode) that currently has at least one open "นักศึกษาฝึกงาน"
-- job posting. JobGroupID is resolved by GroupName (not hardcoded) since the ID can differ across
-- environments. "Open for intake" mirrors sp_GetAllJobsV2's active-job filter (JobStatus <> 'Time
-- Up', ClosingDate NULL-or-future), plus ApprovalStatus = 'Approved' so a still-pending-approval
-- posting doesn't count as an open trainee slot yet.
CREATE OR ALTER VIEW vw_TraineeDepartmentRequired
AS
    SELECT
        HR.COMPANY_CODE AS CompanyCode,
        J.Department AS DepartmentCode,
        SUM(J.NumberOfPositions) AS Required,
        COUNT(*) AS OpenJobCount
    FROM Jobs J
    INNER JOIN JobGroups JG ON JG.JobGroupID = J.JobGroupID AND JG.GroupName = N'นักศึกษาฝึกงาน'
    -- T_EMPLOYEE_SSO has many rows (one per employee) per COSTCENT, so it must be de-duplicated
    -- to one row per COSTCENT before joining, or the SUM/COUNT below fan out and over-count.
    LEFT JOIN (
        SELECT DISTINCT COSTCENT, COMPANY_CODE
        FROM [HRMS_LINKED_SERVER].HRMS.dbo.T_EMPLOYEE_SSO
        WHERE COSTCENT IS NOT NULL AND COSTCENT <> ''
    ) HR ON HR.COSTCENT = J.Department
    WHERE J.JobStatus <> 'Time Up'
      AND J.ApprovalStatus = 'Approved'
      AND (J.ClosingDate IS NULL OR J.ClosingDate >= GETDATE())
      AND J.Department IS NOT NULL
    GROUP BY HR.COMPANY_CODE, J.Department

GO
-- Per-posting counterpart to vw_TraineeDepartmentRequired's aggregate: lets the manual-trainee
-- form offer a dropdown of the individual open "นักศึกษาฝึกงาน" postings in a department, so the
-- admin can pick which one a walk-in should be attributed to (JobApplications.JobID).
CREATE OR ALTER PROCEDURE sp_GetOpenTraineeJobsByDepartment
    @DepartmentCode NVARCHAR(200)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT J.JobID, J.JobTitle, J.NumberOfPositions
    FROM Jobs J
    INNER JOIN JobGroups JG ON JG.JobGroupID = J.JobGroupID AND JG.GroupName = N'นักศึกษาฝึกงาน'
    WHERE J.Department = @DepartmentCode
      AND J.JobStatus <> 'Time Up'
      AND J.ApprovalStatus = 'Approved'
      AND (J.ClosingDate IS NULL OR J.ClosingDate >= GETDATE())
    ORDER BY J.PostedDate DESC
END
GO
-- DEPRECATED (as of the Jobs-derived Required/IsOpen pivot): required/isOpen are no longer read
-- from TraineeQuota — see vw_TraineeDepartmentRequired and sp_GetTraineeManagementOverview below.
-- Left functional (still safe to call, still writes TraineeQuota) only so nothing currently pointed
-- at PUT /TraineeManagement/departments/{code}/quota breaks immediately; not called by the overview
-- read path anymore. Candidate for removal once confirmed nothing external still calls it.
--
-- DepartmentCode (HRMS COSTCENT) is globally unique across companies (verified: 175 distinct
-- COSTCENT == 175 distinct COMPANY_CODE+COSTCENT pairs in T_EMPLOYEE_SSO), so quota can be
-- addressed by DepartmentCode alone; CompanyCode is resolved server-side from HRMS.
CREATE OR ALTER PROCEDURE sp_UpsertTraineeQuotaByDepartment
    @DepartmentCode      NVARCHAR(200),
    @Quota                INT,
    @IsAcceptingTrainees  BIT,
    @AdminID              INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CompanyCode NVARCHAR(50), @DepartmentName NVARCHAR(400);
    SELECT TOP 1 @CompanyCode = COMPANY_CODE, @DepartmentName = NAMECOSTCENT
    FROM [HRMS_LINKED_SERVER].HRMS.dbo.T_EMPLOYEE_SSO
    WHERE COSTCENT = @DepartmentCode;

    IF @CompanyCode IS NULL
    BEGIN
        RAISERROR('ไม่พบแผนกนี้ใน HRMS', 16, 1);
        RETURN;
    END

    IF EXISTS (SELECT 1 FROM TraineeQuota WHERE DepartmentCode = @DepartmentCode)
    BEGIN
        UPDATE TraineeQuota
        SET Quota = @Quota, IsAcceptingTrainees = @IsAcceptingTrainees,
            ModifiedAt = GETDATE(), ModifiedByAdminID = @AdminID
        WHERE DepartmentCode = @DepartmentCode
    END
    ELSE
    BEGIN
        INSERT INTO TraineeQuota (CompanyCode, DepartmentCode, DepartmentName, Quota, IsAcceptingTrainees, CreatedByAdminID)
        VALUES (@CompanyCode, @DepartmentCode, @DepartmentName, @Quota, @IsAcceptingTrainees, @AdminID)
    END

    SELECT QuotaID, CompanyCode, DepartmentCode, DepartmentName, Quota, IsAcceptingTrainees, Notes,
           CreatedAt, CreatedByAdminID, ModifiedAt, ModifiedByAdminID
    FROM TraineeQuota
    WHERE DepartmentCode = @DepartmentCode
END

GO
-- Result set 1: every HRMS company/department. Required/IsOpen are derived from live Job postings
-- via vw_TraineeDepartmentRequired (Required defaults to 0 / IsOpen to false when no qualifying
-- posting exists for that department), optionally narrowed by @CompanyCode/@DepartmentCode. Result
-- set 2: placed, non-cancelled trainees whose internship period overlaps @Year, same optional
-- filters (unchanged — still sourced from TraineeAssignments, unaffected by the Required/IsOpen
-- pivot). Mapped in C# into nested CompanyGroup[] -> DepartmentGroup[] -> Trainee[].
CREATE OR ALTER PROCEDURE sp_GetTraineeManagementOverview
    @Year           INT = NULL,
    @CompanyCode    NVARCHAR(50) = NULL,
    @DepartmentCode NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        EMP.COMPANY_CODE AS CompanyCode,
        COALESCE(CI.Company_nameth, CI.company_name, EMP.COMPANY_CODE) AS CompanyName,
        EMP.COSTCENT AS DepartmentCode,
        EMP.NAMECOSTCENT AS DepartmentName,
        ISNULL(R.Required, 0) AS Required,
        CASE WHEN ISNULL(R.OpenJobCount, 0) > 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsOpen
    FROM (
        SELECT DISTINCT COSTCENT, COMPANY_CODE, NAMECOSTCENT
        FROM [HRMS_LINKED_SERVER].HRMS.dbo.T_EMPLOYEE_SSO
        WHERE COSTCENT IS NOT NULL AND COSTCENT <> ''
          AND NAMECOSTCENT IS NOT NULL AND NAMECOSTCENT <> ''
    ) EMP
    LEFT JOIN vw_TraineeDepartmentRequired R
        ON R.CompanyCode = EMP.COMPANY_CODE AND R.DepartmentCode = EMP.COSTCENT
    LEFT JOIN CompanyInfo CI
        ON CI.Company_Code = EMP.COMPANY_CODE
    WHERE (@CompanyCode IS NULL OR EMP.COMPANY_CODE = @CompanyCode)
      AND (@DepartmentCode IS NULL OR EMP.COSTCENT = @DepartmentCode)
    ORDER BY EMP.COMPANY_CODE, EMP.NAMECOSTCENT;

    SELECT
        A.AssignmentID AS Id,
        A.CompanyCode,
        A.DepartmentCode,
        LTRIM(RTRIM(CONCAT(A.ManualFirstNameThai, N' ', A.ManualLastNameThai))) AS Name,
        A.ManualNickname AS Nickname,
        A.ManualUniversity AS University,
        CONVERT(char(10), A.ManualInternStartDate, 23) AS StartDate,
        CONVERT(char(10), A.ManualInternEndDate, 23) AS EndDate
    FROM TraineeAssignments A
    WHERE A.Status <> 'Cancelled'
      AND A.CompanyCode IS NOT NULL AND A.DepartmentCode IS NOT NULL
      AND (@CompanyCode IS NULL OR A.CompanyCode = @CompanyCode)
      AND (@DepartmentCode IS NULL OR A.DepartmentCode = @DepartmentCode)
      AND (
            @Year IS NULL
            OR (A.ManualInternStartDate <= DATEFROMPARTS(@Year, 12, 31)
                AND A.ManualInternEndDate >= DATEFROMPARTS(@Year, 1, 1))
          )
    ORDER BY A.CompanyCode, A.DepartmentCode, A.ManualInternStartDate;
END

GO
-- Creates a trainee assignment directly into a department (no pending/unassigned state — that
-- concept belongs to the separate self-apply flow in sp_CreateOrPlaceTraineeAssignment above).
-- @ApplicantID pulls name/nickname/university from an existing T_APPLICANTS row (a candidate who
-- already applied to one of the "นักศึกษาฝึกงาน" job postings); otherwise the manual name fields
-- are used. Quota is always a soft warning (IsOverQuota flag in the result) — creation is never
-- blocked, per the frontend spec.
CREATE OR ALTER PROCEDURE sp_CreateTraineeManagementAssignment
    @CompanyCode        NVARCHAR(50),
    @DepartmentCode     NVARCHAR(200),
    @ApplicantID        INT = NULL,
    @FirstNameThai      NVARCHAR(200) = NULL,
    @LastNameThai       NVARCHAR(200) = NULL,
    @Nickname           NVARCHAR(50) = NULL,
    @University         NVARCHAR(200) = NULL,
    @StartDate          DATE,
    @EndDate            DATE,
    @AssignedByAdminID  INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (
        SELECT 1 FROM [HRMS_LINKED_SERVER].HRMS.dbo.T_EMPLOYEE_SSO
        WHERE COMPANY_CODE = @CompanyCode AND COSTCENT = @DepartmentCode
    )
    BEGIN
        RAISERROR('ไม่พบแผนกนี้ในบริษัทที่ระบุ', 16, 1);
        RETURN;
    END

    IF @ApplicantID IS NULL AND (@FirstNameThai IS NULL OR @LastNameThai IS NULL)
    BEGIN
        RAISERROR('ต้องระบุ applicantId หรือกรอกชื่อ-นามสกุลนักศึกษา', 16, 1);
        RETURN;
    END

    DECLARE @Title NVARCHAR(20) = NULL, @FName NVARCHAR(200) = @FirstNameThai,
            @LName NVARCHAR(200) = @LastNameThai, @Nick NVARCHAR(50) = @Nickname,
            @Univ NVARCHAR(200) = @University;

    IF @ApplicantID IS NOT NULL
    BEGIN
        SELECT @Title = Title, @FName = FirstNameThai, @LName = LastNameThai,
               @Nick = Nickname, @Univ = University
        FROM T_APPLICANTS
        WHERE ApplicantID = @ApplicantID;

        IF @FName IS NULL
        BEGIN
            RAISERROR('ไม่พบผู้สมัคร ApplicantID ที่ระบุ', 16, 1);
            RETURN;
        END
    END

    DECLARE @Quota INT;
    SELECT @Quota = Required FROM vw_TraineeDepartmentRequired WHERE CompanyCode = @CompanyCode AND DepartmentCode = @DepartmentCode;
    SET @Quota = ISNULL(@Quota, 0);

    DECLARE @ActiveOverlapCount INT, @IsOverQuota BIT = 0;
    SELECT @ActiveOverlapCount = COUNT(*)
    FROM TraineeAssignments
    WHERE CompanyCode = @CompanyCode AND DepartmentCode = @DepartmentCode
      AND Status <> 'Cancelled'
      AND ManualInternStartDate <= @EndDate AND ManualInternEndDate >= @StartDate;

    IF (@ActiveOverlapCount + 1) > @Quota
        SET @IsOverQuota = 1;

    -- If this applicant already has a JobApplications row not yet claimed by another
    -- TraineeAssignments (e.g. one created by usp_TraineeApplicant_Upsert's rich-form
    -- submission), reuse it instead of inserting a duplicate — otherwise the files
    -- attached to that submission (T_APPLICANT_FILES, keyed by ApplicationID) end up
    -- orphaned from the assignment this proc creates.
    DECLARE @ApplicationID INT = NULL;

    IF @ApplicantID IS NOT NULL
    BEGIN
        SELECT TOP 1 @ApplicationID = JA.ApplicationID
        FROM JobApplications JA
        WHERE JA.ApplicantID = @ApplicantID
          AND NOT EXISTS (
              SELECT 1 FROM TraineeAssignments TA
              WHERE TA.ApplicationID = JA.ApplicationID AND TA.Status <> 'Cancelled'
          )
        ORDER BY JA.SubmissionDate DESC;
    END

    IF @ApplicationID IS NOT NULL
    BEGIN
        UPDATE JobApplications SET Status = 'Employment confirm' WHERE ApplicationID = @ApplicationID;
    END
    ELSE
    BEGIN
        INSERT INTO JobApplications (ApplicantID, JobID, Status, SubmissionDate)
        VALUES (@ApplicantID, NULL, 'Employment confirm', GETDATE());

        SET @ApplicationID = CAST(SCOPE_IDENTITY() AS INT);
    END

    INSERT INTO TraineeAssignments (
        CompanyCode, DepartmentCode, ApplicationID,
        ManualTitle, ManualFirstNameThai, ManualLastNameThai, ManualNickname, ManualUniversity,
        ManualInternStartDate, ManualInternEndDate,
        AssignedByAdminID
    )
    VALUES (
        @CompanyCode, @DepartmentCode, @ApplicationID,
        @Title, @FName, @LName, @Nick, @Univ,
        @StartDate, @EndDate,
        @AssignedByAdminID
    )

    DECLARE @NewAssignmentID INT = CAST(SCOPE_IDENTITY() AS INT);

    SELECT @NewAssignmentID AS Id, @IsOverQuota AS IsOverQuota, @ActiveOverlapCount AS ActiveOverlapCount, @Quota AS Quota
END

GO
-- True cancel (not the "return to unassigned pool" behavior of sp_UnassignTraineeAssignment
-- above) — this page has no unassigned-pool concept, DELETE means remove from the department.
CREATE OR ALTER PROCEDURE sp_DeleteTraineeManagementAssignment
    @AssignmentID INT
AS
BEGIN
    UPDATE TraineeAssignments
    SET Status = 'Cancelled', ModifiedAt = GETDATE()
    WHERE AssignmentID = @AssignmentID
END
GO

-- Admin-only orchestration target for POST /api/TraineeManagement/trainees/manual.
-- The API starts the SQL transaction and enlists this procedure plus file metadata inserts in it.
-- A real T_APPLICANTS row is deliberately created for a walk-in; ApplicantID and ApplicationID
-- remain distinct identifiers. @JobID is optional and NULL by default (a walk-in isn't required
-- to be tied to a posting), but the admin can pick one from
-- sp_GetOpenTraineeJobsByDepartment to attribute the walk-in to a specific open posting.
CREATE OR ALTER PROCEDURE sp_CreateManualTraineeManagement
    @CompanyCode NVARCHAR(50), @DepartmentCode NVARCHAR(200),
    @StartDate DATE, @EndDate DATE,
    @DesiredField1 NVARCHAR(200) = NULL, @DesiredField2 NVARCHAR(200) = NULL, @DesiredField3 NVARCHAR(200) = NULL,
    @InternshipType NVARCHAR(50) = NULL, @Reason NVARCHAR(500) = NULL, @ReasonOther NVARCHAR(500) = NULL,
    @PrefixT NVARCHAR(100) = NULL, @NameFirstT NVARCHAR(100), @NameLastT NVARCHAR(100), @NicknameT NVARCHAR(50) = NULL,
    @PrefixE NVARCHAR(100) = NULL, @NameFirstE NVARCHAR(100) = NULL, @NameLastE NVARCHAR(100) = NULL, @NicknameE NVARCHAR(50) = NULL,
    @Gender NVARCHAR(20) = NULL, @DateOfBirth DATE = NULL, @PlaceOfBirth NVARCHAR(200) = NULL,
    @Nationality NVARCHAR(100) = NULL, @Race NVARCHAR(100) = NULL, @Religion NVARCHAR(100) = NULL,
    @Height DECIMAL(5,2) = NULL, @Weight DECIMAL(5,2) = NULL, @IDCardNo NVARCHAR(20) = NULL,
    @IDIssuedBy NVARCHAR(200) = NULL, @IDExpiredDate DATE = NULL, @Address NVARCHAR(500) = NULL,
    @ProvinceID INT = NULL, @DistrictID INT = NULL, @SubDistrictID INT = NULL, @PostalCode NVARCHAR(10) = NULL,
    @Telephone NVARCHAR(20) = NULL, @Mobile NVARCHAR(20), @Email NVARCHAR(150),
    @FatherName NVARCHAR(200) = NULL, @FatherOccupation NVARCHAR(200) = NULL, @FatherStatus NVARCHAR(50) = NULL,
    @MotherName NVARCHAR(200) = NULL, @MotherOccupation NVARCHAR(200) = NULL, @MotherStatus NVARCHAR(50) = NULL,
    @SiblingCount INT = NULL, @SiblingOrder INT = NULL, @EmergencyName NVARCHAR(200) = NULL,
    @EmergencyRelation NVARCHAR(100) = NULL, @EmergencyAddress NVARCHAR(500) = NULL, @EmergencyPhone NVARCHAR(20) = NULL,
    @School NVARCHAR(300), @Faculty NVARCHAR(200) = NULL, @Major NVARCHAR(200) = NULL, @Minor NVARCHAR(200) = NULL,
    @YearOfStudy NVARCHAR(20) = NULL, @AdvisorName NVARCHAR(200) = NULL, @AdvisorPhone NVARCHAR(20) = NULL,
    @Activities NVARCHAR(1000) = NULL, @InfoSources NVARCHAR(200) = NULL,
    @InfoSourceStaffName NVARCHAR(200) = NULL, @InfoSourceDepartment NVARCHAR(200) = NULL, @InfoSourceOther NVARCHAR(200) = NULL,
    @AssignedByAdminID INT = NULL, @JobID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM T_APPLICANTS WHERE Email = @Email AND (NULLIF(@IDCardNo, '') IS NULL OR ISNULL(CitizenID, '') <> @IDCardNo))
        THROW 51001, N'อีเมลนี้มีอยู่ในระบบแล้ว', 1;

    IF @JobID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Jobs WHERE JobID = @JobID AND Department = @DepartmentCode)
    BEGIN
        RAISERROR('ไม่พบตำแหน่งงานนี้ในแผนกที่ระบุ', 16, 1);
        RETURN;
    END

    DECLARE @ApplicantResult TABLE (ApplicantID INT, ApplicationID INT);
    INSERT INTO @ApplicantResult
    EXEC usp_TraineeApplicant_Upsert
        @StartDate=@StartDate, @EndDate=@EndDate, @DesiredField1=@DesiredField1, @DesiredField2=@DesiredField2,
        @DesiredField3=@DesiredField3, @InternshipType=@InternshipType, @Reason=@Reason, @ReasonOther=@ReasonOther,
        @PrefixT=@PrefixT, @NameFirstT=@NameFirstT, @NameLastT=@NameLastT, @NicknameT=@NicknameT,
        @PrefixE=@PrefixE, @NameFirstE=@NameFirstE, @NameLastE=@NameLastE, @NicknameE=@NicknameE,
        @Gender=@Gender, @DateOfBirth=@DateOfBirth, @PlaceOfBirth=@PlaceOfBirth, @Nationality=@Nationality,
        @Race=@Race, @Religion=@Religion, @Height=@Height, @Weight=@Weight, @IDCardNo=@IDCardNo,
        @IDIssuedBy=@IDIssuedBy, @IDExpiredDate=@IDExpiredDate, @Address=@Address, @ProvinceID=@ProvinceID,
        @DistrictID=@DistrictID, @SubDistrictID=@SubDistrictID, @PostalCode=@PostalCode, @Telephone=@Telephone,
        @Mobile=@Mobile, @Email=@Email, @FatherName=@FatherName, @FatherOccupation=@FatherOccupation,
        @FatherStatus=@FatherStatus, @MotherName=@MotherName, @MotherOccupation=@MotherOccupation,
        @MotherStatus=@MotherStatus, @SiblingCount=@SiblingCount, @SiblingOrder=@SiblingOrder,
        @EmergencyName=@EmergencyName, @EmergencyRelation=@EmergencyRelation, @EmergencyAddress=@EmergencyAddress,
        @EmergencyPhone=@EmergencyPhone, @School=@School, @Faculty=@Faculty, @Major=@Major, @Minor=@Minor,
        @YearOfStudy=@YearOfStudy, @AdvisorName=@AdvisorName, @AdvisorPhone=@AdvisorPhone, @Activities=@Activities,
        @InfoSources=@InfoSources, @InfoSourceStaffName=@InfoSourceStaffName,
        @InfoSourceDepartment=@InfoSourceDepartment, @InfoSourceOther=@InfoSourceOther,
        @Status=N'Employment confirm', @JobID=@JobID;

    DECLARE @ApplicantID INT, @ApplicationID INT;
    SELECT @ApplicantID=ApplicantID, @ApplicationID=ApplicationID FROM @ApplicantResult;
    DECLARE @AssignmentResult TABLE (Id INT, IsOverQuota BIT, ActiveOverlapCount INT, Quota INT);
    INSERT INTO @AssignmentResult
    EXEC sp_CreateTraineeManagementAssignment @CompanyCode=@CompanyCode, @DepartmentCode=@DepartmentCode,
        @ApplicantID=@ApplicantID, @StartDate=@StartDate, @EndDate=@EndDate, @AssignedByAdminID=@AssignedByAdminID;

    SELECT A.Id, @ApplicationID AS TraineeApplicationId, @ApplicantID AS ApplicantId,
           A.IsOverQuota, A.ActiveOverlapCount, A.Quota
    FROM @AssignmentResult A;
END
GO
