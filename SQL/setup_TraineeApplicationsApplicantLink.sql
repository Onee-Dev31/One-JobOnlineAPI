-- บันทึกใบสมัครฝึกงาน (หน้า /apply/trainee/part2) ตรงเข้า T_APPLICANTS + JobApplications
-- เลิกใช้ตาราง TraineeApplications สำหรับ flow นี้แล้ว (คงตาราง/ proc เดิมไว้เฉยๆ ไม่ลบ ไม่แก้)
--
-- 1) เพิ่มคอลัมน์เฉพาะของ trainee ใน T_APPLICANTS (nullable ทั้งหมด)
-- 2) สร้างตาราง T_APPLICANT_FILES เก็บไฟล์แนบ (idCard/houseReg/resume) คีย์ด้วย ApplicantID
-- 3) สร้าง usp_TraineeApplicant_Upsert เขียนตรงเข้า T_APPLICANTS + JobApplications
--    dedupe ด้วย CitizenID ก่อน แล้ว fallback เป็น Mobile+Email, resubmit JobID เดิม = update แถว JobApplications เดิม

IF COL_LENGTH('T_APPLICANTS', 'InternshipStartDate') IS NULL
    ALTER TABLE T_APPLICANTS ADD InternshipStartDate DATE NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'InternshipEndDate') IS NULL
    ALTER TABLE T_APPLICANTS ADD InternshipEndDate DATE NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'DesiredField1') IS NULL
    ALTER TABLE T_APPLICANTS ADD DesiredField1 NVARCHAR(200) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'DesiredField2') IS NULL
    ALTER TABLE T_APPLICANTS ADD DesiredField2 NVARCHAR(200) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'DesiredField3') IS NULL
    ALTER TABLE T_APPLICANTS ADD DesiredField3 NVARCHAR(200) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'ReasonOther') IS NULL
    ALTER TABLE T_APPLICANTS ADD ReasonOther NVARCHAR(500) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'PlaceOfBirth') IS NULL
    ALTER TABLE T_APPLICANTS ADD PlaceOfBirth NVARCHAR(200) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'Nationality') IS NULL
    ALTER TABLE T_APPLICANTS ADD Nationality NVARCHAR(100) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'Race') IS NULL
    ALTER TABLE T_APPLICANTS ADD Race NVARCHAR(100) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'Religion') IS NULL
    ALTER TABLE T_APPLICANTS ADD Religion NVARCHAR(100) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'HomePhone') IS NULL
    ALTER TABLE T_APPLICANTS ADD HomePhone NVARCHAR(20) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'FatherName') IS NULL
    ALTER TABLE T_APPLICANTS ADD FatherName NVARCHAR(200) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'FatherOccupation') IS NULL
    ALTER TABLE T_APPLICANTS ADD FatherOccupation NVARCHAR(200) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'FatherStatus') IS NULL
    ALTER TABLE T_APPLICANTS ADD FatherStatus NVARCHAR(50) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'MotherName') IS NULL
    ALTER TABLE T_APPLICANTS ADD MotherName NVARCHAR(200) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'MotherOccupation') IS NULL
    ALTER TABLE T_APPLICANTS ADD MotherOccupation NVARCHAR(200) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'MotherStatus') IS NULL
    ALTER TABLE T_APPLICANTS ADD MotherStatus NVARCHAR(50) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'SiblingOrder') IS NULL
    ALTER TABLE T_APPLICANTS ADD SiblingOrder INT NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'EmergencyName') IS NULL
    ALTER TABLE T_APPLICANTS ADD EmergencyName NVARCHAR(200) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'EmergencyRelation') IS NULL
    ALTER TABLE T_APPLICANTS ADD EmergencyRelation NVARCHAR(100) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'EmergencyAddress') IS NULL
    ALTER TABLE T_APPLICANTS ADD EmergencyAddress NVARCHAR(500) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'EmergencyPhone') IS NULL
    ALTER TABLE T_APPLICANTS ADD EmergencyPhone NVARCHAR(20) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'School') IS NULL
    ALTER TABLE T_APPLICANTS ADD School NVARCHAR(300) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'Faculty') IS NULL
    ALTER TABLE T_APPLICANTS ADD Faculty NVARCHAR(200) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'Major') IS NULL
    ALTER TABLE T_APPLICANTS ADD Major NVARCHAR(200) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'Minor') IS NULL
    ALTER TABLE T_APPLICANTS ADD Minor NVARCHAR(200) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'YearOfStudy') IS NULL
    ALTER TABLE T_APPLICANTS ADD YearOfStudy NVARCHAR(20) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'AdvisorName') IS NULL
    ALTER TABLE T_APPLICANTS ADD AdvisorName NVARCHAR(200) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'AdvisorPhone') IS NULL
    ALTER TABLE T_APPLICANTS ADD AdvisorPhone NVARCHAR(20) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'Activities') IS NULL
    ALTER TABLE T_APPLICANTS ADD Activities NVARCHAR(1000) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'InfoSources') IS NULL
    ALTER TABLE T_APPLICANTS ADD InfoSources NVARCHAR(200) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'InfoSourceStaffName') IS NULL
    ALTER TABLE T_APPLICANTS ADD InfoSourceStaffName NVARCHAR(200) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'InfoSourceDepartment') IS NULL
    ALTER TABLE T_APPLICANTS ADD InfoSourceDepartment NVARCHAR(200) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'InfoSourceOther') IS NULL
    ALTER TABLE T_APPLICANTS ADD InfoSourceOther NVARCHAR(200) NULL
GO
IF COL_LENGTH('T_APPLICANTS', 'InternshipType') IS NULL
    ALTER TABLE T_APPLICANTS ADD InternshipType NVARCHAR(50) NULL
GO

IF OBJECT_ID('T_APPLICANT_FILES', 'U') IS NULL
BEGIN
    CREATE TABLE T_APPLICANT_FILES (
        FileID INT IDENTITY(1,1) PRIMARY KEY,
        ApplicantID INT NOT NULL,
        ApplicationID INT NULL,
        FilePath NVARCHAR(500) NOT NULL,
        FileName NVARCHAR(255) NOT NULL,
        FileSize BIGINT NULL,
        FileType NVARCHAR(100) NULL,
        SectionFile NVARCHAR(50) NULL,
        UploadedDate DATETIME NOT NULL DEFAULT GETDATE(),
        CONSTRAINT FK_T_APPLICANT_FILES_Applicant FOREIGN KEY (ApplicantID) REFERENCES T_APPLICANTS(ApplicantID)
    )
END
GO

-- Backstop for usp_TraineeApplicant_Upsert's ApplicantID+JobID reuse check below: that check is a
-- SELECT-then-INSERT, not atomic on its own, so two near-simultaneous submits for the same
-- applicant+job can both miss the existing row and both insert. This closes the race window.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_JobApplications_ApplicantID_JobID')
BEGIN
    CREATE UNIQUE NONCLUSTERED INDEX UQ_JobApplications_ApplicantID_JobID
    ON JobApplications (ApplicantID, JobID)
    WHERE ApplicantID IS NOT NULL
END
GO

-- T_APPLICANTS has a filtered unique index (UQ_T_APPLICANTS_CodeMPID), so any proc that
-- INSERT/UPDATE/DELETEs it must be compiled with QUOTED_IDENTIFIER ON (this setting is captured
-- at CREATE PROCEDURE time and baked into the proc regardless of the caller's session).
SET QUOTED_IDENTIFIER ON;
GO

-- sp_CreateManualTraineeManagement (SQL/setup_TraineeManagementV2.sql) is a deliberate
-- duplicate of this proc's applicant-upsert logic, folded in as a self-contained proc for the
-- admin manual-entry flow. If a bug fix or new field belongs in both the self-apply/Part2 flow
-- (this proc, called from TraineeApplicationsController.cs) and the admin manual-entry flow,
-- apply it to both.
ALTER PROCEDURE [dbo].[usp_TraineeApplicant_Upsert]
    @AssignmentID        INT            = NULL,
    @StartDate           DATE           = NULL,
    @EndDate             DATE           = NULL,
    @DesiredField1       NVARCHAR(200)  = NULL,
    @DesiredField2       NVARCHAR(200)  = NULL,
    @DesiredField3       NVARCHAR(200)  = NULL,
    @InternshipType      NVARCHAR(50)   = NULL,
    @Reason              NVARCHAR(500)  = NULL,
    @ReasonOther         NVARCHAR(500)  = NULL,
    @PrefixT             NVARCHAR(100)  = NULL,
    @NameFirstT          NVARCHAR(100),
    @NameLastT           NVARCHAR(100),
    @NicknameT           NVARCHAR(50)   = NULL,
    @PrefixE             NVARCHAR(100)  = NULL,
    @NameFirstE          NVARCHAR(100)  = NULL,
    @NameLastE           NVARCHAR(100)  = NULL,
    @NicknameE           NVARCHAR(50)   = NULL,
    @Gender              NVARCHAR(20)   = NULL,
    @DateOfBirth         DATE           = NULL,
    @PlaceOfBirth        NVARCHAR(200)  = NULL,
    @Nationality         NVARCHAR(100)  = NULL,
    @Race                NVARCHAR(100)  = NULL,
    @Religion            NVARCHAR(100)  = NULL,
    @Height              DECIMAL(5,2)   = NULL,
    @Weight              DECIMAL(5,2)   = NULL,
    @IDCardNo            NVARCHAR(20)   = NULL,
    @IDIssuedBy          NVARCHAR(200)  = NULL,
    @IDExpiredDate       DATE           = NULL,
    @Address             NVARCHAR(500)  = NULL,
    @ProvinceID          INT            = NULL,
    @DistrictID          INT            = NULL,
    @SubDistrictID       INT            = NULL,
    @PostalCode          NVARCHAR(10)   = NULL,
    @Telephone           NVARCHAR(20)   = NULL,
    @Mobile              NVARCHAR(20),
    @Email               NVARCHAR(150),
    @FatherName          NVARCHAR(200)  = NULL,
    @FatherOccupation    NVARCHAR(200)  = NULL,
    @FatherStatus        NVARCHAR(50)   = NULL,
    @MotherName          NVARCHAR(200)  = NULL,
    @MotherOccupation    NVARCHAR(200)  = NULL,
    @MotherStatus        NVARCHAR(50)   = NULL,
    @SiblingCount        INT            = NULL,
    @SiblingOrder        INT            = NULL,
    @EmergencyName       NVARCHAR(200)  = NULL,
    @EmergencyRelation   NVARCHAR(100)  = NULL,
    @EmergencyAddress    NVARCHAR(500)  = NULL,
    @EmergencyPhone      NVARCHAR(20)   = NULL,
    @School              NVARCHAR(300),
    @Faculty             NVARCHAR(200)  = NULL,
    @Major               NVARCHAR(200)  = NULL,
    @Minor               NVARCHAR(200)  = NULL,
    @YearOfStudy         NVARCHAR(20)   = NULL,
    @AdvisorName         NVARCHAR(200)  = NULL,
    @AdvisorPhone        NVARCHAR(20)   = NULL,
    @Activities          NVARCHAR(1000) = NULL,
    @InfoSources         NVARCHAR(200)  = NULL,
    @InfoSourceStaffName NVARCHAR(200)  = NULL,
    @InfoSourceDepartment NVARCHAR(200) = NULL,
    @InfoSourceOther     NVARCHAR(200)  = NULL,
    @Status              NVARCHAR(50)   = 'Pending HR Screening',
    @JobID               INT            = NULL,
    @UserID              INT            = NULL
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

    -- 1. ค้นหาด้วย UserID ก่อน (1 UserID = 1 แถวใน T_APPLICANTS)
    IF @UserID IS NOT NULL
        SELECT TOP 1 @ApplicantID = ApplicantID FROM T_APPLICANTS
        WHERE UserID = @UserID ORDER BY ApplicantID DESC

    -- 2. Fallback: CitizenID
    IF @ApplicantID IS NULL AND @IDCardNo IS NOT NULL AND LTRIM(RTRIM(@IDCardNo)) <> ''
        SELECT @ApplicantID = ApplicantID FROM T_APPLICANTS WHERE CitizenID = @IDCardNo

    -- 3. Fallback: Mobile + Email
    IF @ApplicantID IS NULL
        SELECT TOP 1 @ApplicantID = ApplicantID FROM T_APPLICANTS
        WHERE MobilePhone = @Mobile AND Email = @Email ORDER BY ApplicantID DESC

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
            UserID = ISNULL(@UserID, UserID),
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
            InfoSources, InfoSourceStaffName, InfoSourceDepartment, InfoSourceOther,
            UserID
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
            @InfoSources, @InfoSourceStaffName, @InfoSourceDepartment, @InfoSourceOther,
            @UserID
        )

        SET @ApplicantID = CAST(SCOPE_IDENTITY() AS INT)
    END

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
