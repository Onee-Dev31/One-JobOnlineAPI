-- Concept: whenever sp_AssignApplicantToSlot writes a manual/self-apply Part1 row into
-- JobSlotAssignments, also create the matching T_APPLICANTS record immediately (instead of
-- waiting for the candidate to reach Part2 / usp_TraineeApplicant_Upsert), and link it back via
-- JobApplications.ApplicantID.
--
-- 1) Add columns to T_APPLICANTS for JobSlotAssignments.Manual* fields with no existing match
-- 2) sp_AssignApplicantToSlot: insert T_APPLICANTS + set JobApplications.ApplicantID when manual
--    name fields are provided (walk-in entry or self-apply Part1 — both always send a name;
--    reusing an already-vetted ApplicationID with no manual name is left untouched, it already
--    has an ApplicantID from a prior full submission)
-- 3) usp_TraineeApplicant_Upsert (Part2): resolve @ApplicantID via @AssignmentID first, before the
--    CitizenID dedupe, so Part2 updates the row Part1 already created instead of duplicating it

IF COL_LENGTH('T_APPLICANTS', 'Age') IS NULL
    ALTER TABLE T_APPLICANTS ADD Age INT NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'GPA') IS NULL
    ALTER TABLE T_APPLICANTS ADD GPA DECIMAL(3, 2) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'University') IS NULL
    ALTER TABLE T_APPLICANTS ADD University NVARCHAR(200) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'PreferredPosition') IS NULL
    ALTER TABLE T_APPLICANTS ADD PreferredPosition NVARCHAR(100) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'PreferredPositionBackup') IS NULL
    ALTER TABLE T_APPLICANTS ADD PreferredPositionBackup NVARCHAR(100) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'CanCommute') IS NULL
    ALTER TABLE T_APPLICANTS ADD CanCommute BIT NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'CanTravelOutside') IS NULL
    ALTER TABLE T_APPLICANTS ADD CanTravelOutside BIT NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'FlexibleWork') IS NULL
    ALTER TABLE T_APPLICANTS ADD FlexibleWork BIT NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'ReasonForInterest') IS NULL
    ALTER TABLE T_APPLICANTS ADD ReasonForInterest NVARCHAR(1000) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'DurationMonths') IS NULL
    ALTER TABLE T_APPLICANTS ADD DurationMonths NVARCHAR(20) NULL
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

    -- Mirror this row into T_APPLICANTS so it exists from Part1 onward, not just after Part2.
    -- Only when manual name fields were actually supplied here (walk-in entry or self-apply
    -- Part1) — reusing an already-vetted @ApplicationID with no manual data means T_APPLICANTS
    -- already has this applicant from a prior full submission, so there's nothing to mirror.
    IF @ManualFirstNameThai IS NOT NULL AND @ManualLastNameThai IS NOT NULL
    BEGIN
        INSERT INTO T_APPLICANTS (
            Title, FirstNameThai, LastNameThai, Nickname, MobilePhone, Email, Age,
            YearOfStudy, GPA, Major, Faculty, University,
            InternshipType, InternshipStartDate, InternshipEndDate, DurationMonths,
            PreferredPosition, PreferredPositionBackup,
            CanCommute, CanTravelOutside, FlexibleWork, ReasonForInterest,
            CreatedDate
        )
        VALUES (
            @ManualTitle, @ManualFirstNameThai, @ManualLastNameThai, @ManualNickname, @ManualMobilePhone, @ManualEmail, @ManualAge,
            @ManualYear, @ManualGPA, @ManualMajor, @ManualFaculty, @ManualUniversity,
            @ManualInternshipType, @ManualInternStartDate, @ManualInternEndDate, @ManualDurationMonths,
            @ManualPreferredPosition, @ManualPreferredPositionBackup,
            @ManualCanCommute, @ManualCanTravelOutside, @ManualFlexibleWork, @ManualReasonForInterest,
            GETDATE()
        )

        DECLARE @NewApplicantID INT = CAST(SCOPE_IDENTITY() AS INT);

        UPDATE JobApplications SET ApplicantID = @NewApplicantID WHERE ApplicationID = @ApplicationID
    END

    IF @SlotID IS NOT NULL AND (@CurrentCount2 + 1) >= @Capacity2
    BEGIN
        UPDATE JobSlots SET Status = 'Filled', ModifiedAt = GETDATE() WHERE SlotID = @SlotID
    END

    SELECT @NewAssignmentID AS AssignmentID
END

GO

-- T_APPLICANTS has a filtered unique index (UQ_T_APPLICANTS_CodeMPID), so any proc that
-- INSERT/UPDATE/DELETEs it must be compiled with QUOTED_IDENTIFIER ON (this setting is captured
-- at CREATE PROCEDURE time and baked into the proc regardless of the caller's session).
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE usp_TraineeApplicant_Upsert
    @AssignmentID INT = NULL, -- Part1 JobSlotAssignments.AssignmentID, when this submission continues one
    @StartDate DATE = NULL,
    @EndDate DATE = NULL,
    @DesiredField1 NVARCHAR(200) = NULL,
    @DesiredField2 NVARCHAR(200) = NULL,
    @DesiredField3 NVARCHAR(200) = NULL,
    @InternshipType NVARCHAR(50) = NULL,
    @Reason NVARCHAR(500) = NULL,
    @ReasonOther NVARCHAR(500) = NULL,
    @PrefixT NVARCHAR(100) = NULL,
    @NameFirstT NVARCHAR(100),
    @NameLastT NVARCHAR(100),
    @NicknameT NVARCHAR(50) = NULL,
    @PrefixE NVARCHAR(100) = NULL,
    @NameFirstE NVARCHAR(100) = NULL,
    @NameLastE NVARCHAR(100) = NULL,
    @NicknameE NVARCHAR(50) = NULL,
    @Gender NVARCHAR(20) = NULL,
    @DateOfBirth DATE = NULL,
    @PlaceOfBirth NVARCHAR(200) = NULL,
    @Nationality NVARCHAR(100) = NULL,
    @Race NVARCHAR(100) = NULL,
    @Religion NVARCHAR(100) = NULL,
    @Height DECIMAL(5,2) = NULL,
    @Weight DECIMAL(5,2) = NULL,
    @IDCardNo NVARCHAR(20) = NULL,
    @IDIssuedBy NVARCHAR(200) = NULL,
    @IDExpiredDate DATE = NULL,
    @Address NVARCHAR(500) = NULL,
    @ProvinceID INT = NULL,
    @DistrictID INT = NULL,
    @SubDistrictID INT = NULL,
    @PostalCode NVARCHAR(10) = NULL,
    @Telephone NVARCHAR(20) = NULL,
    @Mobile NVARCHAR(20),
    @Email NVARCHAR(150),
    @FatherName NVARCHAR(200) = NULL,
    @FatherOccupation NVARCHAR(200) = NULL,
    @FatherStatus NVARCHAR(50) = NULL,
    @MotherName NVARCHAR(200) = NULL,
    @MotherOccupation NVARCHAR(200) = NULL,
    @MotherStatus NVARCHAR(50) = NULL,
    @SiblingCount INT = NULL,
    @SiblingOrder INT = NULL,
    @EmergencyName NVARCHAR(200) = NULL,
    @EmergencyRelation NVARCHAR(100) = NULL,
    @EmergencyAddress NVARCHAR(500) = NULL,
    @EmergencyPhone NVARCHAR(20) = NULL,
    @School NVARCHAR(300),
    @Faculty NVARCHAR(200) = NULL,
    @Major NVARCHAR(200) = NULL,
    @Minor NVARCHAR(200) = NULL,
    @YearOfStudy NVARCHAR(20) = NULL,
    @AdvisorName NVARCHAR(200) = NULL,
    @AdvisorPhone NVARCHAR(20) = NULL,
    @Activities NVARCHAR(1000) = NULL,
    @InfoSources NVARCHAR(200) = NULL,
    @InfoSourceStaffName NVARCHAR(200) = NULL,
    @InfoSourceDepartment NVARCHAR(200) = NULL,
    @InfoSourceOther NVARCHAR(200) = NULL,
    @Status NVARCHAR(50) = 'pending',
    @JobID INT
AS
BEGIN
    SET NOCOUNT ON

    DECLARE @GenderCode CHAR(1) = CASE
        WHEN @Gender IS NULL THEN NULL
        WHEN @Gender IN ('male', N'ชาย') THEN 'M'
        WHEN @Gender IN ('female', N'หญิง') THEN 'F'
        ELSE 'O'
    END

    DECLARE @ApplicantID INT

    -- Prefer the ApplicantID already linked from Part1 (sp_AssignApplicantToSlot mirrors its
    -- manual entry into T_APPLICANTS and sets JobApplications.ApplicantID) so this continues that
    -- same row instead of duplicating it — Part1 has no CitizenID to dedupe on below.
    IF @AssignmentID IS NOT NULL
        SELECT @ApplicantID = ja.ApplicantID
        FROM JobSlotAssignments jsa
        INNER JOIN JobApplications ja ON ja.ApplicationID = jsa.ApplicationID
        WHERE jsa.AssignmentID = @AssignmentID

    IF @ApplicantID IS NULL AND @IDCardNo IS NOT NULL AND LTRIM(RTRIM(@IDCardNo)) <> ''
        SELECT @ApplicantID = ApplicantID FROM T_APPLICANTS WHERE CitizenID = @IDCardNo

    IF @ApplicantID IS NOT NULL
    BEGIN
        UPDATE T_APPLICANTS SET
            Title = @PrefixT, FirstNameThai = @NameFirstT, LastNameThai = @NameLastT, Nickname = @NicknameT,
            TitleENG = @PrefixE, FirstNameEng = @NameFirstE, LastNameEng = @NameLastE, NicknameE = @NicknameE,
            Gender = @GenderCode, BirthDate = @DateOfBirth, Height = @Height, Weight = @Weight,
            CitizenID = @IDCardNo, CitizenIDIssuedBy = @IDIssuedBy, CitizenIDExpiresON = @IDExpiredDate,
            CurrentAddress = @Address, CurrentProvinceID = @ProvinceID, CurrentDistrictID = @DistrictID,
            CurrentSubDistrictID = @SubDistrictID, CurrentPostalCode = @PostalCode,
            MobilePhone = @Mobile, Email = @Email, HomePhone = @Telephone,
            ReasonPosition = @Reason, ReasonOther = @ReasonOther,
            InternshipStartDate = @StartDate, InternshipEndDate = @EndDate, InternshipType = @InternshipType,
            DesiredField1 = @DesiredField1, DesiredField2 = @DesiredField2, DesiredField3 = @DesiredField3,
            PlaceOfBirth = @PlaceOfBirth, Nationality = @Nationality, Race = @Race, Religion = @Religion,
            FatherName = @FatherName, FatherOccupation = @FatherOccupation, FatherStatus = @FatherStatus,
            MotherName = @MotherName, MotherOccupation = @MotherOccupation, MotherStatus = @MotherStatus,
            SiblingsAll = @SiblingCount, SiblingOrder = @SiblingOrder,
            EmergencyName = @EmergencyName, EmergencyRelation = @EmergencyRelation,
            EmergencyAddress = @EmergencyAddress, EmergencyPhone = @EmergencyPhone,
            School = @School, Faculty = @Faculty, Major = @Major, Minor = @Minor, YearOfStudy = @YearOfStudy,
            AdvisorName = @AdvisorName, AdvisorPhone = @AdvisorPhone, Activities = @Activities,
            InfoSources = @InfoSources, InfoSourceStaffName = @InfoSourceStaffName,
            InfoSourceDepartment = @InfoSourceDepartment, InfoSourceOther = @InfoSourceOther,
            ModifiedDate = GETDATE()
        WHERE ApplicantID = @ApplicantID
    END
    ELSE
    BEGIN
        INSERT INTO T_APPLICANTS (
            Title, FirstNameThai, LastNameThai, FirstNameEng, LastNameEng, Nickname, TitleENG, NicknameE,
            Gender, BirthDate, Height, Weight, CitizenID, CitizenIDIssuedBy, CitizenIDExpiresON,
            CurrentAddress, CurrentProvinceID, CurrentDistrictID, CurrentSubDistrictID, CurrentPostalCode,
            MobilePhone, Email, HomePhone, ReasonPosition, ReasonOther,
            InternshipStartDate, InternshipEndDate, InternshipType, DesiredField1, DesiredField2, DesiredField3,
            PlaceOfBirth, Nationality, Race, Religion,
            FatherName, FatherOccupation, FatherStatus, MotherName, MotherOccupation, MotherStatus,
            SiblingsAll, SiblingOrder,
            EmergencyName, EmergencyRelation, EmergencyAddress, EmergencyPhone,
            School, Faculty, Major, Minor, YearOfStudy, AdvisorName, AdvisorPhone, Activities,
            InfoSources, InfoSourceStaffName, InfoSourceDepartment, InfoSourceOther
        )
        VALUES (
            @PrefixT, @NameFirstT, @NameLastT, @NameFirstE, @NameLastE, @NicknameT, @PrefixE, @NicknameE,
            @GenderCode, @DateOfBirth, @Height, @Weight, @IDCardNo, @IDIssuedBy, @IDExpiredDate,
            @Address, @ProvinceID, @DistrictID, @SubDistrictID, @PostalCode,
            @Mobile, @Email, @Telephone, @Reason, @ReasonOther,
            @StartDate, @EndDate, @InternshipType, @DesiredField1, @DesiredField2, @DesiredField3,
            @PlaceOfBirth, @Nationality, @Race, @Religion,
            @FatherName, @FatherOccupation, @FatherStatus, @MotherName, @MotherOccupation, @MotherStatus,
            @SiblingCount, @SiblingOrder,
            @EmergencyName, @EmergencyRelation, @EmergencyAddress, @EmergencyPhone,
            @School, @Faculty, @Major, @Minor, @YearOfStudy, @AdvisorName, @AdvisorPhone, @Activities,
            @InfoSources, @InfoSourceStaffName, @InfoSourceDepartment, @InfoSourceOther
        )

        SET @ApplicantID = CAST(SCOPE_IDENTITY() AS INT)
    END

    -- If this submission continues a Part1 assignment, reuse the JobApplications row already
    -- linked from JobSlotAssignments (created ApplicantID = NULL by sp_AssignApplicantToSlot's
    -- self-apply path) instead of the ApplicantID+JobID dedupe below, which would never match it
    -- and would leave that row's ApplicantID NULL forever while creating an orphaned duplicate.
    DECLARE @ApplicationID INT
    IF @AssignmentID IS NOT NULL
        SELECT @ApplicationID = ApplicationID FROM JobSlotAssignments WHERE AssignmentID = @AssignmentID

    IF @ApplicationID IS NOT NULL
    BEGIN
        UPDATE JobApplications SET ApplicantID = @ApplicantID, JobID = @JobID, Status = @Status WHERE ApplicationID = @ApplicationID
    END
    ELSE
    BEGIN
        SELECT @ApplicationID = ApplicationID FROM JobApplications WHERE ApplicantID = @ApplicantID AND JobID = @JobID

        IF @ApplicationID IS NOT NULL
        BEGIN
            UPDATE JobApplications SET Status = @Status WHERE ApplicationID = @ApplicationID
        END
        ELSE
        BEGIN
            INSERT INTO JobApplications (ApplicantID, JobID, Status, SubmissionDate)
            VALUES (@ApplicantID, @JobID, @Status, GETDATE())

            SET @ApplicationID = CAST(SCOPE_IDENTITY() AS INT)
        END
    END

    SELECT @ApplicantID AS ApplicantID, @ApplicationID AS ApplicationID
END
GO
