-- Fix: UPDATE branch เขียนทับทุก column ด้วย param โดยตรง
-- แก้เป็น COALESCE(@Param, Column) เพื่อคงค่าเดิมถ้า param เป็น NULL

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
            Title                = COALESCE(@PrefixT,             Title),
            FirstNameThai        = COALESCE(@NameFirstT,          FirstNameThai),
            LastNameThai         = COALESCE(@NameLastT,           LastNameThai),
            Nickname             = COALESCE(@NicknameT,           Nickname),
            TitleENG             = COALESCE(@PrefixE,             TitleENG),
            FirstNameEng         = COALESCE(@NameFirstE,          FirstNameEng),
            LastNameEng          = COALESCE(@NameLastE,           LastNameEng),
            NicknameE            = COALESCE(@NicknameE,           NicknameE),
            Gender               = COALESCE(@GenderCode,          Gender),
            BirthDate            = COALESCE(@DateOfBirth,         BirthDate),
            Height               = COALESCE(@Height,              Height),
            Weight               = COALESCE(@Weight,              Weight),
            CitizenID            = COALESCE(@IDCardNo,            CitizenID),
            CitizenIDIssuedBy    = COALESCE(@IDIssuedBy,          CitizenIDIssuedBy),
            CitizenIDExpiresON   = COALESCE(@IDExpiredDate,       CitizenIDExpiresON),
            CurrentAddress       = COALESCE(@Address,             CurrentAddress),
            CurrentProvinceID    = COALESCE(@ProvinceID,          CurrentProvinceID),
            CurrentDistrictID    = COALESCE(@DistrictID,          CurrentDistrictID),
            CurrentSubDistrictID = COALESCE(@SubDistrictID,       CurrentSubDistrictID),
            CurrentPostalCode    = COALESCE(@PostalCode,          CurrentPostalCode),
            MobilePhone          = COALESCE(@Mobile,              MobilePhone),
            Email                = COALESCE(@Email,               Email),
            HomePhone            = COALESCE(@Telephone,           HomePhone),
            ReasonPosition       = COALESCE(@Reason,              ReasonPosition),
            ReasonOther          = COALESCE(@ReasonOther,         ReasonOther),
            InternshipStartDate  = COALESCE(@StartDate,           InternshipStartDate),
            InternshipEndDate    = COALESCE(@EndDate,             InternshipEndDate),
            InternshipType       = COALESCE(@InternshipType,      InternshipType),
            DesiredField1        = COALESCE(@DesiredField1,       DesiredField1),
            DesiredField2        = COALESCE(@DesiredField2,       DesiredField2),
            DesiredField3        = COALESCE(@DesiredField3,       DesiredField3),
            PlaceOfBirth         = COALESCE(@PlaceOfBirth,        PlaceOfBirth),
            Nationality          = COALESCE(@Nationality,         Nationality),
            Race                 = COALESCE(@Race,                Race),
            Religion             = COALESCE(@Religion,            Religion),
            FatherName           = COALESCE(@FatherName,          FatherName),
            FatherOccupation     = COALESCE(@FatherOccupation,    FatherOccupation),
            FatherStatus         = COALESCE(@FatherStatus,        FatherStatus),
            MotherName           = COALESCE(@MotherName,          MotherName),
            MotherOccupation     = COALESCE(@MotherOccupation,    MotherOccupation),
            MotherStatus         = COALESCE(@MotherStatus,        MotherStatus),
            SiblingsAll          = COALESCE(@SiblingCount,        SiblingsAll),
            SiblingOrder         = COALESCE(@SiblingOrder,        SiblingOrder),
            EmergencyName        = COALESCE(@EmergencyName,       EmergencyName),
            EmergencyRelation    = COALESCE(@EmergencyRelation,   EmergencyRelation),
            EmergencyAddress     = COALESCE(@EmergencyAddress,    EmergencyAddress),
            EmergencyPhone       = COALESCE(@EmergencyPhone,      EmergencyPhone),
            School               = COALESCE(@School,              School),
            Faculty              = COALESCE(@Faculty,             Faculty),
            Major                = COALESCE(@Major,               Major),
            Minor                = COALESCE(@Minor,               Minor),
            YearOfStudy          = COALESCE(@YearOfStudy,         YearOfStudy),
            AdvisorName          = COALESCE(@AdvisorName,         AdvisorName),
            AdvisorPhone         = COALESCE(@AdvisorPhone,        AdvisorPhone),
            Activities           = COALESCE(@Activities,          Activities),
            InfoSources          = COALESCE(@InfoSources,         InfoSources),
            InfoSourceStaffName  = COALESCE(@InfoSourceStaffName, InfoSourceStaffName),
            InfoSourceDepartment = COALESCE(@InfoSourceDepartment,InfoSourceDepartment),
            InfoSourceOther      = COALESCE(@InfoSourceOther,     InfoSourceOther),
            UserID               = ISNULL(@UserID, UserID),
            ModifiedDate         = GETDATE()
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
        SELECT @ApplicationID = ApplicationID
        FROM JobApplications
        WHERE ApplicantID = @ApplicantID
          AND JobID = @JobID

        IF @ApplicationID IS NOT NULL
        BEGIN
            UPDATE JobApplications
            SET Status = @Status
            WHERE ApplicationID = @ApplicationID
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
