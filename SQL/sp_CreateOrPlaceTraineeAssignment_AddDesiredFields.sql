-- ============================================================
-- เพิ่ม DesiredField1 = @ManualPreferredPosition
--      DesiredField2 = @ManualPreferredPositionBackup
-- ใน UPDATE และ INSERT ของ T_APPLICANTS
-- ============================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_CreateOrPlaceTraineeAssignment]
    @CompanyCode                   NVARCHAR(50) = NULL,
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
    IF @UserID IS NOT NULL AND NOT EXISTS (SELECT 1 FROM Users WHERE UserId = @UserID)
        SET @UserID = NULL

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
        UPDATE TraineeAssignments
        SET CompanyCode = @CompanyCode, DepartmentCode = @DepartmentCode,
            ModifiedAt = GETDATE(), ModifiedByAdminID = @AssignedByAdminID
        WHERE AssignmentID = @ExistingAssignmentID

        DECLARE @ExistingApplicantID INT = NULL
        SELECT @ExistingApplicantID = ApplicantID FROM JobApplications WHERE ApplicationID = @ApplicationID

        SELECT @ExistingAssignmentID AS AssignmentID, @IsOverQuota AS IsOverQuota, @ActiveOverlapCount AS ActiveOverlapCount, @Quota AS Quota,
               @ExistingApplicantID AS ApplicantID, @ApplicationID AS ApplicationID
        RETURN
    END

    IF @ApplicationID IS NULL AND @ManualFirstNameThai IS NULL
    BEGIN
        RAISERROR('ต้องมี ApplicationID หรือกรอกชื่อ Manual อย่างใดอย่างหนึ่ง', 16, 1);
        RETURN;
    END

    IF @ApplicationID IS NULL
    BEGIN
        DECLARE @ResolvedApplicantID INT = NULL
        IF @ManualMobilePhone IS NOT NULL AND @ManualEmail IS NOT NULL
        BEGIN
            SELECT TOP 1 @ResolvedApplicantID = ApplicantID
            FROM T_APPLICANTS
            WHERE MobilePhone = @ManualMobilePhone AND Email = @ManualEmail
            ORDER BY ApplicantID DESC

            IF @ResolvedApplicantID IS NOT NULL AND @ManualFirstNameThai IS NOT NULL AND @ManualLastNameThai IS NOT NULL
            BEGIN
                UPDATE T_APPLICANTS SET
                    Title = @ManualTitle, FirstNameThai = @ManualFirstNameThai, LastNameThai = @ManualLastNameThai, Nickname = @ManualNickname,
                    MobilePhone = @ManualMobilePhone, Email = @ManualEmail,
                    Age = @ManualAge, YearOfStudy = @ManualYear, GPA = @ManualGPA, Major = @ManualMajor, Faculty = @ManualFaculty, University = @ManualUniversity,
                    InternshipType = @ManualInternshipType, InternshipStartDate = @ManualInternStartDate, InternshipEndDate = @ManualInternEndDate, DurationMonths = @ManualDurationMonths,
                    PreferredPosition = @ManualPreferredPosition, PreferredPositionBackup = @ManualPreferredPositionBackup,
                    DesiredField1 = @ManualPreferredPosition, DesiredField2 = @ManualPreferredPositionBackup,
                    CanCommute = @ManualCanCommute, CanTravelOutside = @ManualCanTravelOutside, FlexibleWork = @ManualFlexibleWork, ReasonForInterest = @ManualReasonForInterest,
                    UserId = @UserID, ModifiedDate = GETDATE()
                WHERE ApplicantID = @ResolvedApplicantID
            END

            IF @ResolvedApplicantID IS NULL AND @ManualFirstNameThai IS NOT NULL AND @ManualLastNameThai IS NOT NULL
            BEGIN
                INSERT INTO T_APPLICANTS (
                    Title, FirstNameThai, LastNameThai, Nickname, MobilePhone, Email,
                    Age, YearOfStudy, GPA, Major, Faculty, University,
                    InternshipType, InternshipStartDate, InternshipEndDate, DurationMonths,
                    PreferredPosition, PreferredPositionBackup,
                    DesiredField1, DesiredField2,
                    CanCommute, CanTravelOutside, FlexibleWork, ReasonForInterest,
                    UserId, CreatedDate
                )
                VALUES (
                    @ManualTitle, @ManualFirstNameThai, @ManualLastNameThai, @ManualNickname, @ManualMobilePhone, @ManualEmail,
                    @ManualAge, @ManualYear, @ManualGPA, @ManualMajor, @ManualFaculty, @ManualUniversity,
                    @ManualInternshipType, @ManualInternStartDate, @ManualInternEndDate, @ManualDurationMonths,
                    @ManualPreferredPosition, @ManualPreferredPositionBackup,
                    @ManualPreferredPosition, @ManualPreferredPositionBackup,
                    @ManualCanCommute, @ManualCanTravelOutside, @ManualFlexibleWork, @ManualReasonForInterest,
                    @UserID, GETDATE()
                )

                SET @ResolvedApplicantID = CAST(SCOPE_IDENTITY() AS INT)
            END
        END

        DECLARE @NewStatus NVARCHAR(50) = IIF(@CompanyCode IS NOT NULL, 'Employment confirm', 'Pending HR Screening')

        IF @ResolvedApplicantID IS NOT NULL
            SELECT @ApplicationID = ApplicationID FROM JobApplications WHERE ApplicantID = @ResolvedApplicantID AND JobID = @JobID

        IF @ApplicationID IS NOT NULL
        BEGIN
            UPDATE JobApplications SET Status = @NewStatus WHERE ApplicationID = @ApplicationID
        END
        ELSE
        BEGIN
            INSERT INTO JobApplications (ApplicantID, JobID, Status, SubmissionDate)
            VALUES (@ResolvedApplicantID, @JobID, @NewStatus, GETDATE())

            SET @ApplicationID = CAST(SCOPE_IDENTITY() AS INT)
        END
    END

    IF @ResolvedApplicantID IS NULL
        SELECT @ResolvedApplicantID = ApplicantID FROM JobApplications WHERE ApplicationID = @ApplicationID

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

    SELECT @NewAssignmentID AS AssignmentID, @IsOverQuota AS IsOverQuota, @ActiveOverlapCount AS ActiveOverlapCount, @Quota AS Quota,
           @ResolvedApplicantID AS ApplicantID, @ApplicationID AS ApplicationID
END
GO
