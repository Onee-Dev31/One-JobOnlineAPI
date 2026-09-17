-- ============================================================
-- Step 1: เพิ่ม column ใน T_APPLICANTS (ถ้ายังไม่มี)
-- ============================================================
-- หมายเหตุ: DesiredField1 = PreferredPosition, DesiredField2 = PreferredPositionBackup
--           (สำหรับ trainee flow ที่มาจาก sp_CreateOrPlaceTraineeAssignment)
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('T_APPLICANTS') AND name = 'DesiredField1')
    ALTER TABLE T_APPLICANTS ADD DesiredField1 NVARCHAR(200) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('T_APPLICANTS') AND name = 'DesiredField2')
    ALTER TABLE T_APPLICANTS ADD DesiredField2 NVARCHAR(200) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('T_APPLICANTS') AND name = 'DesiredField3')
    ALTER TABLE T_APPLICANTS ADD DesiredField3 NVARCHAR(200) NULL;

GO

-- ============================================================
-- Step 2: ALTER SP เพิ่ม DesiredField1/2/3
-- ============================================================
ALTER PROCEDURE [dbo].[InsertOrUpdateApplicantDataNew]
    @JsonInput NVARCHAR(MAX),
    @EducationList NVARCHAR(MAX),
    @WorkExperienceList NVARCHAR(MAX),
    @SkillsList NVARCHAR(MAX),
    @FilesList NVARCHAR(MAX),
    @RelationshipList NVARCHAR(MAX),
    @JobID INT,
    @ApplicantID INT OUTPUT,
    @ApplicantEmail NVARCHAR(100) OUTPUT,
    @HRManagerEmails NVARCHAR(MAX) OUTPUT,
    @JobManagerEmails NVARCHAR(MAX) OUTPUT,
    @JobTitle NVARCHAR(200) OUTPUT,
    @CompanyName NVARCHAR(200) OUTPUT,
    @OutJobID INT OUTPUT,
    @Salary DECIMAL(18,2) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION;

    BEGIN TRY
        IF ISJSON(@JsonInput) = 0
            THROW 50001, 'Invalid JSON input.', 1;

        DECLARE @UserId INT;
        DECLARE @Email NVARCHAR(100) = JSON_VALUE(@JsonInput, '$.Email');
        DECLARE @JsonJobID NVARCHAR(50) = JSON_VALUE(@JsonInput, '$.JobID');
		DECLARE @JobOtherName NVARCHAR(200);
        DECLARE @ExistingApplicantID INT;
        DECLARE @JobStatus NVARCHAR(50);

        DECLARE @JobStartDate DATE = dbo.fn_ParseThaiDate(JSON_VALUE(@JsonInput, '$.JobStartDate'));
        DECLARE @CheckDraft NVARCHAR(50) = JSON_VALUE(@JsonInput, '$.CheckDraft');
        SET @Salary = TRY_CAST(JSON_VALUE(@JsonInput, '$.Salary') AS DECIMAL(18,2));
		SET @JobOtherName = JSON_VALUE(@JsonInput, '$.JobOtherName');

        BEGIN TRY
            DECLARE @UserIdStr NVARCHAR(10) = JSON_VALUE(@JsonInput, '$.UserId');
            SET @UserId = CASE WHEN @UserIdStr IS NOT NULL AND @UserIdStr <> '' THEN CAST(@UserIdStr AS INT) ELSE NULL END;
        END TRY
        BEGIN CATCH
            SET @UserId = NULL;
            THROW 50004, 'Invalid UserId format in JSON input.', 1;
        END CATCH;

        IF @JsonJobID IS NOT NULL
        BEGIN
            DECLARE @ParsedJobID INT;
            BEGIN TRY
                SET @ParsedJobID = CAST(@JsonJobID AS INT);
                IF @ParsedJobID <> @JobID
                    THROW 50005, 'JobID in JSON does not match the provided JobID parameter.', 1;
            END TRY
            BEGIN CATCH
                THROW 50006, 'Invalid JobID format in JSON input.', 1;
            END CATCH;
        END

        IF NOT EXISTS (SELECT 1 FROM Jobs WHERE JobID = @JobID)
            THROW 50003, 'Invalid JobID. Job does not exist.', 1;

        SELECT @ExistingApplicantID = ApplicantID, @JobStatus = Status
        FROM [dbo].[fn_GetApplicantStatusNew](@UserId, @JobID);

        SELECT @JobTitle = JobTitle, @CompanyName = Location
        FROM Jobs WHERE JobID = @JobID;

        DECLARE @InputApplicantID INT = JSON_VALUE(@JsonInput, '$.ApplicantID');
        IF @InputApplicantID IS NOT NULL
        BEGIN
            IF EXISTS (SELECT 1 FROM T_APPLICANTS WHERE ApplicantID = @InputApplicantID)
                SET @ExistingApplicantID = @InputApplicantID;
        END

		DECLARE @CitizenIDExpiresON NVARCHAR(10) = JSON_VALUE(@JsonInput, '$.CitizenIDExpiresON');
		DECLARE @CitizenIDExpiresON_Date DATE;

		IF @CitizenIDExpiresON IS NOT NULL
		BEGIN
			DECLARE @year INT = CAST(LEFT(@CitizenIDExpiresON, 4) AS INT);
			DECLARE @month INT = CAST(SUBSTRING(@CitizenIDExpiresON, 6, 2) AS INT);
			DECLARE @day INT = CAST(RIGHT(@CitizenIDExpiresON, 2) AS INT);

			IF @year > 2500
				SET @year = @year - 543;

			SET @CitizenIDExpiresON_Date = DATEFROMPARTS(@year, @month, @day);
		END

		DECLARE @BirthDateStr NVARCHAR(10) = JSON_VALUE(@JsonInput, '$.BirthDate');
		DECLARE @BirthDate DATE = NULL;

		IF @BirthDateStr IS NOT NULL AND LTRIM(RTRIM(@BirthDateStr)) <> ''
		BEGIN
			DECLARE @yearBirth INT = CAST(LEFT(@BirthDateStr, 4) AS INT);
			DECLARE @monthBirth INT = CAST(SUBSTRING(@BirthDateStr, 6, 2) AS INT);
			DECLARE @dayBirth INT = CAST(RIGHT(@BirthDateStr, 2) AS INT);

			IF @yearBirth > 2500
				SET @yearBirth = @yearBirth - 543;

			SET @BirthDate = DATEFROMPARTS(@yearBirth, @monthBirth, @dayBirth);
		END

        -- CASE: Update existing Applicant
        IF @ExistingApplicantID IS NOT NULL
        BEGIN
            UPDATE T_APPLICANTS
            SET
                Title = COALESCE(json.Title, T_APPLICANTS.Title),
				TitleENG = COALESCE(json.TitleENG, T_APPLICANTS.TitleENG),
				Gender = COALESCE(json.Gender, T_APPLICANTS.Gender),
                FirstNameThai = COALESCE(json.FirstNameThai, T_APPLICANTS.FirstNameThai),
                LastNameThai = COALESCE(json.LastNameThai, T_APPLICANTS.LastNameThai),
                FirstNameEng = COALESCE(json.FirstNameEng, T_APPLICANTS.FirstNameEng),
                LastNameEng = COALESCE(json.LastNameEng, T_APPLICANTS.LastNameEng),
                BirthDate = COALESCE(json.BirthDate, T_APPLICANTS.BirthDate),
                Weight = COALESCE(json.Weight, T_APPLICANTS.Weight),
                Height = COALESCE(json.Height, T_APPLICANTS.Height),
                CitizenID = COALESCE(json.CitizenID, T_APPLICANTS.CitizenID),
                CitizenIDIssuedBy = COALESCE(json.CitizenIDIssuedBy, T_APPLICANTS.CitizenIDIssuedBy),
                CitizenIDExpiresON = COALESCE(@CitizenIDExpiresON_Date, T_APPLICANTS.CitizenIDExpiresON),
                CurrentAddress = COALESCE(json.CurrentAddress, T_APPLICANTS.CurrentAddress),
                CurrentProvinceID = COALESCE(json.CurrentProvinceID, T_APPLICANTS.CurrentProvinceID),
                CurrentDistrictID = COALESCE(json.CurrentDistrictID, T_APPLICANTS.CurrentDistrictID),
                CurrentSubDistrictID = COALESCE(json.CurrentSubDistrictID, T_APPLICANTS.CurrentSubDistrictID),
                CurrentPostalCode = COALESCE(json.CurrentPostalCode, T_APPLICANTS.CurrentPostalCode),
                MobilePhone = COALESCE(json.MobilePhone, T_APPLICANTS.MobilePhone),
                Email = COALESCE(json.Email, T_APPLICANTS.Email),
                LINE = COALESCE(json.LINE, T_APPLICANTS.LINE),
                SiblingsAll = COALESCE(json.SiblingsAll, T_APPLICANTS.SiblingsAll),
                NumberAY    = COALESCE(json.NumberAY, T_APPLICANTS.NumberAY),
                RegisteredAddress = COALESCE(json.RegisteredAddress, T_APPLICANTS.RegisteredAddress),
                RegisteredProvinceID = COALESCE(json.RegisteredProvinceID, T_APPLICANTS.RegisteredProvinceID),
                RegisteredDistrictID = COALESCE(json.RegisteredDistrictID, T_APPLICANTS.RegisteredDistrictID),
                RegisteredSubDistrictID = COALESCE(json.RegisteredSubDistrictID, T_APPLICANTS.RegisteredSubDistrictID),
                RegisteredPostalCode = COALESCE(json.RegisteredPostalCode, T_APPLICANTS.RegisteredPostalCode),
                MinitaryService = COALESCE(json.MinitaryService, T_APPLICANTS.MinitaryService),
                ReasonMinitary = COALESCE(json.ReasonMinitary, T_APPLICANTS.ReasonMinitary),
                MaritalStatus = COALESCE(json.MaritalStatus, T_APPLICANTS.MaritalStatus),
                QuestionnaireVehiclesMotorcycle = COALESCE(json.QuestionnaireVehiclesMotorcycle, T_APPLICANTS.QuestionnaireVehiclesMotorcycle),
                MotorcycleLicense = COALESCE(json.MotorcycleLicense, T_APPLICANTS.MotorcycleLicense),
                QuestionnaireVehiclesCar = COALESCE(json.QuestionnaireVehiclesCar, T_APPLICANTS.QuestionnaireVehiclesCar),
                CarLicense = COALESCE(json.CarLicense, T_APPLICANTS.CarLicense),
                QuestionnaireDisabilities = COALESCE(json.QuestionnaireDisabilities, T_APPLICANTS.QuestionnaireDisabilities),
                ReasonDisabilities = COALESCE(json.ReasonDisabilities, T_APPLICANTS.ReasonDisabilities),
                QuestionnaireConvicted = COALESCE(json.QuestionnaireConvicted, T_APPLICANTS.QuestionnaireConvicted),
                QuestionnaireFiredjob = COALESCE(json.QuestionnaireFiredjob, T_APPLICANTS.QuestionnaireFiredjob),
                QuestionnaireApplyjob = COALESCE(json.QuestionnaireApplyjob, T_APPLICANTS.QuestionnaireApplyjob),
                QuestionnaireWorkShifts = COALESCE(json.QuestionnaireWorkShifts, T_APPLICANTS.QuestionnaireWorkShifts),
                QuestionnaireCheckInformation = COALESCE(json.QuestionnaireCheckInformation, T_APPLICANTS.QuestionnaireCheckInformation),
                QuestionnaireRelative = COALESCE(json.QuestionnaireRelative, T_APPLICANTS.QuestionnaireRelative),
                ReasonPosition = COALESCE(json.ReasonPosition, T_APPLICANTS.ReasonPosition),
                SpouseFullName = COALESCE(json.SpouseFullName, T_APPLICANTS.SpouseFullName),
                SpouseOccupation = COALESCE(json.SpouseOccupation, T_APPLICANTS.SpouseOccupation),
                SpouseCompanyType = COALESCE(json.SpouseCompanyType, T_APPLICANTS.SpouseCompanyType),
                SpouseCompanyAddress = COALESCE(json.SpouseCompanyAddress, T_APPLICANTS.SpouseCompanyAddress),
                SpouseMobilePhone = COALESCE(json.SpouseMobilePhone, T_APPLICANTS.SpouseMobilePhone),
                SpouseLINE = COALESCE(json.SpouseLINE, T_APPLICANTS.SpouseLINE),
                SpouseEmail = COALESCE(json.SpouseEmail, T_APPLICANTS.SpouseEmail),
                SpouseAliveStatus = COALESCE(json.SpouseAliveStatus, T_APPLICANTS.SpouseAliveStatus),
                MaleChildren = COALESCE(json.MaleChildren, T_APPLICANTS.MaleChildren),
                FemaleChildren = COALESCE(json.FemaleChildren, T_APPLICANTS.FemaleChildren),
                UserId = COALESCE(json.UserId, T_APPLICANTS.UserId),
                Nickname = COALESCE(json.Nickname, T_APPLICANTS.Nickname),
				NicknameE = COALESCE(json.NicknameE, T_APPLICANTS.NicknameE),
				ThaiNationality = COALESCE(json.ThaiNationality, T_APPLICANTS.ThaiNationality),
                UserType = COALESCE(json.UserType, T_APPLICANTS.UserType),
                CodeMPID = COALESCE(json.CodeMPID, T_APPLICANTS.CodeMPID),
                -- [เพิ่ม] DesiredField
                DesiredField1 = COALESCE(json.DesiredField1, T_APPLICANTS.DesiredField1),
                DesiredField2 = COALESCE(json.DesiredField2, T_APPLICANTS.DesiredField2),
                DesiredField3 = COALESCE(json.DesiredField3, T_APPLICANTS.DesiredField3),
                ModifiedDate = GETDATE()
            FROM OPENJSON(@JsonInput)
            WITH (
                Title NVARCHAR(50) '$.Title',
				TitleENG NVARCHAR(50) '$.TitleENG',
				Gender CHAR(1) '$.Gender',
                FirstNameThai NVARCHAR(100) '$.FirstNameThai',
                LastNameThai NVARCHAR(100) '$.LastNameThai',
                FirstNameEng NVARCHAR(100) '$.FirstNameEng',
                LastNameEng NVARCHAR(100) '$.LastNameEng',
                BirthDate DATE '$.BirthDate',
                Weight DECIMAL(5,2) '$.Weight',
                Height DECIMAL(5,2) '$.Height',
                CitizenID NVARCHAR(20) '$.CitizenID',
                CitizenIDIssuedBy NVARCHAR(200) '$.CitizenIDIssuedBy',
                CitizenIDExpiresON DATE '$.CitizenIDExpiresON',
                CurrentAddress NVARCHAR(500) '$.CurrentAddress',
                CurrentProvinceID INT '$.CurrentProvinceID',
                CurrentDistrictID INT '$.CurrentDistrictID',
                CurrentSubDistrictID INT '$.CurrentSubDistrictID',
                CurrentPostalCode NVARCHAR(10) '$.CurrentPostalCode',
                MobilePhone NVARCHAR(20) '$.MobilePhone',
                Email NVARCHAR(100) '$.Email',
                LINE NVARCHAR(150) '$.LINE',
                SiblingsAll NVARCHAR(10) '$.SiblingsAll',
                NumberAY NVARCHAR(10) '$.NumberAY',
                RegisteredAddress NVARCHAR(500) '$.RegisteredAddress',
                RegisteredProvinceID INT '$.RegisteredProvinceID',
                RegisteredDistrictID INT '$.RegisteredDistrictID',
                RegisteredSubDistrictID INT '$.RegisteredDistrictID',
                RegisteredPostalCode NVARCHAR(10) '$.RegisteredPostalCode',
                MinitaryService NVARCHAR(50) '$.MinitaryService',
                ReasonMinitary NVARCHAR(500) '$.ReasonMinitary',
                MaritalStatus NVARCHAR(50) '$.MaritalStatus',
                QuestionnaireVehiclesMotorcycle NVARCHAR(10) '$.QuestionnaireVehiclesMotorcycle',
                MotorcycleLicense NVARCHAR(500) '$.MotorcycleLicense',
                QuestionnaireVehiclesCar NVARCHAR(10) '$.QuestionnaireVehiclesCar',
                CarLicense NVARCHAR(500) '$.CarLicense',
                QuestionnaireDisabilities NVARCHAR(10) '$.QuestionnaireDisabilities',
                ReasonDisabilities NVARCHAR(500) '$.ReasonDisabilities',
                QuestionnaireConvicted NVARCHAR(10) '$.QuestionnaireConvicted',
                QuestionnaireFiredjob NVARCHAR(10) '$.QuestionnaireFiredjob',
                QuestionnaireApplyjob NVARCHAR(10) '$.QuestionnaireApplyjob',
                QuestionnaireWorkShifts NVARCHAR(10) '$.QuestionnaireWorkShifts',
                QuestionnaireCheckInformation NVARCHAR(10) '$.QuestionnaireCheckInformation',
                QuestionnaireRelative NVARCHAR(10) '$.QuestionnaireRelative',
                ReasonPosition NVARCHAR(500) '$.ReasonPosition',
                SpouseFullName NVARCHAR(200) '$.SpouseFullName',
                SpouseOccupation NVARCHAR(200) '$.SpouseOccupation',
                SpouseCompanyType NVARCHAR(200) '$.SpouseCompanyType',
                SpouseCompanyAddress NVARCHAR(500) '$.SpouseCompanyAddress',
                SpouseMobilePhone NVARCHAR(150) '$.SpouseMobilePhone',
                SpouseLINE NVARCHAR(150) '$.SpouseLINE',
                SpouseEmail NVARCHAR(150) '$.SpouseEmail',
                SpouseAliveStatus NVARCHAR(20) '$.SpouseAliveStatus',
                MaleChildren INT '$.MaleChildren',
                FemaleChildren INT '$.FemaleChildren',
                UserId INT '$.UserId',
                Nickname NVARCHAR(50) '$.Nickname',
				NicknameE NVARCHAR(50) '$.NicknameE',
				ThaiNationality BIT '$.ThaiNationality',
                UserType INT '$.UserType',
                CodeMPID NVARCHAR(8) '$.CodeMPID',
                -- [เพิ่ม] DesiredField
                DesiredField1 NVARCHAR(200) '$.DesiredField1',
                DesiredField2 NVARCHAR(200) '$.DesiredField2',
                DesiredField3 NVARCHAR(200) '$.DesiredField3'
            ) AS json
            WHERE ApplicantID = @ExistingApplicantID;

            SET @ApplicantID = @ExistingApplicantID;

			IF NOT EXISTS (SELECT 1 FROM JobApplications WHERE ApplicantID = @ApplicantID AND JobID = @JobID)
            BEGIN
                INSERT INTO JobApplications (ApplicantID, JobID, Status, SubmissionDate, Salary, JobOtherName)
                VALUES (@ApplicantID, @JobID, 'New Candidate', GETDATE(), @Salary, @JobOtherName);
            END
            ELSE
            BEGIN
                IF EXISTS (
                    SELECT 1 FROM JobApplications
                    WHERE ApplicantID = @ApplicantID AND JobID = @JobID AND Status = 'New Candidate'
                )
                BEGIN
                    UPDATE JobApplications
                    SET Salary = COALESCE(@Salary, Salary)
                    WHERE ApplicantID = @ApplicantID AND JobID = @JobID;
                END
                ELSE
                IF @CheckDraft IS NULL OR @CheckDraft <> 'Draft'
                BEGIN
					IF @JobStartDate IS NOT NULL
					   AND @JobStatus IN ('Waiting HR Re-check', 'Employment confirm')
					BEGIN
						UPDATE JobApplications
						SET Status         = 'Employment confirm',
							JobStartDate   = @JobStartDate,
							SubmissionDate = GETDATE(),
							Salary         = COALESCE(@Salary, Salary)
						WHERE ApplicantID  = @ApplicantID
						  AND JobID        = @JobID
						  AND Status IN ('Waiting HR Re-check', 'Waiting candidate Info', 'Nagotiate Success', 'Employment confirm')
					END
                    ELSE
                    BEGIN
                        UPDATE JobApplications
                        SET Status         = 'Waiting HR Re-check',
                            SubmissionDate = GETDATE(),
                            Salary         = @Salary
                        WHERE ApplicantID  = @ApplicantID
                          AND JobID        = @JobID
                          AND Status IN ('Waiting HR Re-check', 'Waiting candidate Info', 'Nagotiate Success')
                    END
                END
                ELSE
                BEGIN
                    UPDATE JobApplications
                    SET JobStartDate = @JobStartDate,
                        Salary       = @Salary
                    WHERE ApplicantID = @ApplicantID AND JobID = @JobID;
                END
            END
        END
        ELSE
        BEGIN
            -- Insert new Applicant
            INSERT INTO T_APPLICANTS (
                Title, TitleENG, Gender, FirstNameThai, LastNameThai, FirstNameEng, LastNameEng, BirthDate, Weight, Height,
                CitizenID, CitizenIDIssuedBy, CitizenIDExpiresON,
                CurrentAddress, CurrentProvinceID, CurrentDistrictID, CurrentSubDistrictID, CurrentPostalCode,
                MobilePhone, Email, LINE,
                RegisteredAddress, RegisteredProvinceID, RegisteredDistrictID, RegisteredSubDistrictID, RegisteredPostalCode,
                MinitaryService, ReasonMinitary, MaritalStatus,
                QuestionnaireVehiclesMotorcycle, MotorcycleLicense,
                QuestionnaireVehiclesCar, CarLicense,
                QuestionnaireDisabilities, ReasonDisabilities,
                QuestionnaireConvicted, QuestionnaireFiredjob, QuestionnaireApplyjob,
                QuestionnaireWorkShifts, QuestionnaireCheckInformation, QuestionnaireRelative,
                ReasonPosition,
                SpouseFullName, SpouseOccupation, SpouseCompanyType, SpouseCompanyAddress,
                SpouseMobilePhone, SpouseLINE, SpouseEmail, SpouseAliveStatus,
                MaleChildren, FemaleChildren, UserId, Nickname, UserType, CreatedDate,
                SiblingsAll, NumberAY, CodeMPID, NicknameE, ThaiNationality,
                -- [เพิ่ม] DesiredField
                DesiredField1, DesiredField2, DesiredField3
            )
            SELECT
                Title, TitleENG, Gender, FirstNameThai, LastNameThai, FirstNameEng, LastNameEng, BirthDate, Weight, Height,
                CitizenID, CitizenIDIssuedBy, @CitizenIDExpiresON_Date,
                CurrentAddress, CurrentProvinceID, CurrentDistrictID, CurrentSubDistrictID, CurrentPostalCode,
                MobilePhone, Email, LINE,
                RegisteredAddress, RegisteredProvinceID, RegisteredDistrictID, RegisteredSubDistrictID, RegisteredPostalCode,
                MinitaryService, ReasonMinitary, MaritalStatus,
                QuestionnaireVehiclesMotorcycle, MotorcycleLicense,
                QuestionnaireVehiclesCar, CarLicense,
                QuestionnaireDisabilities, ReasonDisabilities,
                QuestionnaireConvicted, QuestionnaireFiredjob, QuestionnaireApplyjob,
                QuestionnaireWorkShifts, QuestionnaireCheckInformation, QuestionnaireRelative,
                ReasonPosition,
                SpouseFullName, SpouseOccupation, SpouseCompanyType, SpouseCompanyAddress,
                SpouseMobilePhone, SpouseLINE, SpouseEmail, SpouseAliveStatus,
                MaleChildren, FemaleChildren, UserId, Nickname, UserType, GETDATE(),
                SiblingsAll, NumberAY, CodeMPID, NicknameE, ThaiNationality,
                -- [เพิ่ม] DesiredField
                DesiredField1, DesiredField2, DesiredField3
            FROM OPENJSON(@JsonInput)
            WITH (
                Title NVARCHAR(50) '$.Title',
				TitleENG NVARCHAR(50) '$.TitleENG',
				Gender CHAR(1) '$.Gender',
                FirstNameThai NVARCHAR(100) '$.FirstNameThai',
                LastNameThai NVARCHAR(100) '$.LastNameThai',
                FirstNameEng NVARCHAR(100) '$.FirstNameEng',
                LastNameEng NVARCHAR(100) '$.LastNameEng',
                BirthDate NVARCHAR(10) '$.BirthDate',
                Weight DECIMAL(5,2) '$.Weight',
                Height DECIMAL(5,2) '$.Height',
                CitizenID NVARCHAR(20) '$.CitizenID',
                CitizenIDIssuedBy NVARCHAR(200) '$.CitizenIDIssuedBy',
                CitizenIDExpiresON NVARCHAR(10) '$.CitizenIDExpiresON',
                CurrentAddress NVARCHAR(500) '$.CurrentAddress',
                CurrentProvinceID INT '$.CurrentProvinceID',
                CurrentDistrictID INT '$.CurrentDistrictID',
                CurrentSubDistrictID INT '$.CurrentSubDistrictID',
                CurrentPostalCode NVARCHAR(10) '$.CurrentPostalCode',
                MobilePhone NVARCHAR(20) '$.MobilePhone',
                Email NVARCHAR(100) '$.Email',
                LINE NVARCHAR(150) '$.LINE',
                RegisteredAddress NVARCHAR(500) '$.RegisteredAddress',
                RegisteredProvinceID INT '$.RegisteredProvinceID',
                RegisteredDistrictID INT '$.RegisteredDistrictID',
                RegisteredSubDistrictID INT '$.RegisteredSubDistrictID',
                RegisteredPostalCode NVARCHAR(10) '$.RegisteredPostalCode',
                MinitaryService NVARCHAR(50) '$.MinitaryService',
                ReasonMinitary NVARCHAR(500) '$.ReasonMinitary',
                MaritalStatus NVARCHAR(50) '$.MaritalStatus',
                QuestionnaireVehiclesMotorcycle NVARCHAR(10) '$.QuestionnaireVehiclesMotorcycle',
                MotorcycleLicense NVARCHAR(500) '$.MotorcycleLicense',
                QuestionnaireVehiclesCar NVARCHAR(10) '$.QuestionnaireVehiclesCar',
                CarLicense NVARCHAR(500) '$.CarLicense',
                QuestionnaireDisabilities NVARCHAR(10) '$.QuestionnaireDisabilities',
                ReasonDisabilities NVARCHAR(500) '$.ReasonDisabilities',
                QuestionnaireConvicted NVARCHAR(10) '$.QuestionnaireConvicted',
                QuestionnaireFiredjob NVARCHAR(10) '$.QuestionnaireFiredjob',
                QuestionnaireApplyjob NVARCHAR(10) '$.QuestionnaireApplyjob',
                QuestionnaireWorkShifts NVARCHAR(10) '$.QuestionnaireWorkShifts',
                QuestionnaireCheckInformation NVARCHAR(10) '$.QuestionnaireCheckInformation',
                QuestionnaireRelative NVARCHAR(10) '$.QuestionnaireRelative',
                ReasonPosition NVARCHAR(500) '$.ReasonPosition',
                SpouseFullName NVARCHAR(200) '$.SpouseFullName',
                SpouseOccupation NVARCHAR(200) '$.SpouseOccupation',
                SpouseCompanyType NVARCHAR(200) '$.SpouseCompanyType',
                SpouseCompanyAddress NVARCHAR(500) '$.SpouseCompanyAddress',
                SpouseMobilePhone NVARCHAR(150) '$.SpouseMobilePhone',
                SpouseLINE NVARCHAR(150) '$.SpouseLINE',
                SpouseEmail NVARCHAR(150) '$.SpouseEmail',
                SpouseAliveStatus NVARCHAR(20) '$.SpouseAliveStatus',
                MaleChildren INT '$.MaleChildren',
                FemaleChildren INT '$.FemaleChildren',
                UserId INT '$.UserId',
                Nickname NVARCHAR(50) '$.Nickname',
                UserType INT '$.UserType',
                SiblingsAll NVARCHAR(10) '$.SiblingsAll',
                NumberAY NVARCHAR(10) '$.NumberAY',
                CodeMPID NVARCHAR(8) '$.CodeMPID',
				NicknameE NVARCHAR(50) '$.NicknameE',
				ThaiNationality BIT '$.ThaiNationality',
                -- [เพิ่ม] DesiredField
                DesiredField1 NVARCHAR(200) '$.DesiredField1',
                DesiredField2 NVARCHAR(200) '$.DesiredField2',
                DesiredField3 NVARCHAR(200) '$.DesiredField3'
            );

            SET @ApplicantID = SCOPE_IDENTITY();

            INSERT INTO JobApplications (ApplicantID, JobID, Status, SubmissionDate, Salary, JobOtherName)
            VALUES (@ApplicantID, @JobID, 'New Candidate', GETDATE(), @Salary, @JobOtherName);
        END

        -------------------------------------------------------------------
        -- Relationship (MERGE)
        -------------------------------------------------------------------
        IF ISJSON(@RelationshipList) = 1 AND EXISTS (SELECT 1 FROM OPENJSON(@RelationshipList))
        BEGIN
            MERGE T_RELATIONSHIP AS target
            USING (
                SELECT
                    @ApplicantID AS APPLICANT_ID,
					@JobID AS JobID,
                    NAME, AGE, CAREER, COMPANY, MOBILE,
                    RELATION_TYPE, RELATION_DESCRIPTION, ALIVE_STATUS, ADDRESS
                FROM OPENJSON(@RelationshipList)
                WITH (
                    NAME NVARCHAR(1000) '$.NAME',
                    AGE NVARCHAR(4) '$.AGE',
                    CAREER NVARCHAR(1000) '$.CAREER',
                    COMPANY NVARCHAR(1000) '$.COMPANY',
                    MOBILE NVARCHAR(20) '$.MOBILE',
                    RELATION_TYPE NVARCHAR(500) '$.RELATION_TYPE',
                    RELATION_DESCRIPTION NVARCHAR(50) '$.RELATION_DESCRIPTION',
                    ALIVE_STATUS NVARCHAR(550) '$.ALIVE_STATUS',
                    ADDRESS NVARCHAR(MAX) '$.ADDRESS'
                )
            ) AS source
            ON target.APPLICANT_ID = source.APPLICANT_ID
				AND target.JobID = source.JobID
               AND target.RELATION_TYPE = source.RELATION_TYPE
               AND target.NAMESURNAME = source.NAME
            WHEN MATCHED THEN
                UPDATE SET
                    AGE = source.AGE, CAREER = source.CAREER, COMPANY = source.COMPANY,
                    MOBILE = source.MOBILE, RELATION_DESCRIPTION = source.RELATION_DESCRIPTION,
                    ALIVE_STATUS = source.ALIVE_STATUS, ADDRESS = source.ADDRESS,
                    MODIFIED_DATE = GETDATE()
            WHEN NOT MATCHED THEN
                INSERT (APPLICANT_ID, JobID, NAMESURNAME, AGE, CAREER, COMPANY, MOBILE, RELATION_TYPE, RELATION_DESCRIPTION, ALIVE_STATUS, ADDRESS, CREATED_DATE)
                VALUES (source.APPLICANT_ID, source.JobID, source.NAME, source.AGE, source.CAREER, source.COMPANY, source.MOBILE, source.RELATION_TYPE, source.RELATION_DESCRIPTION, source.ALIVE_STATUS, source.ADDRESS, GETDATE());
        END

        -------------------------------------------------------------------
        -- Education (Replace All)
        -------------------------------------------------------------------
        IF ISJSON(@EducationList) = 1 AND EXISTS (SELECT 1 FROM OPENJSON(@EducationList))
        BEGIN
            DELETE FROM Education WHERE ApplicantID = @ApplicantID;
            INSERT INTO Education (ApplicantID, EducationLevel, InstitutionName, StartYear, EndYear, Major, GPA, CreatedDate, ProvinceEducation, Faculty)
            SELECT @ApplicantID, EducationLevel, InstitutionName, StartYear, EndYear, Major, GPA, GETDATE(), ProvinceEducation, Faculty
            FROM OPENJSON(@EducationList)
            WITH (
                EducationLevel NVARCHAR(50), InstitutionName NVARCHAR(200),
                StartYear INT, EndYear INT, Major NVARCHAR(100), GPA FLOAT,
                ProvinceEducation NVARCHAR(250), Faculty NVARCHAR(200)
            );
        END

        -------------------------------------------------------------------
        -- WorkExperience (Replace All)
        -------------------------------------------------------------------
        IF ISJSON(@WorkExperienceList) = 1 AND EXISTS (SELECT 1 FROM OPENJSON(@WorkExperienceList))
        BEGIN
            DELETE FROM WorkExperience WHERE ApplicantID = @ApplicantID;
            INSERT INTO WorkExperience (ApplicantID, CompanyName, CompanyType, Position, StartDate, EndDate, Responsibilities, ReasonForLeaving, CreatedDate, FinalSalary)
            SELECT @ApplicantID, CompanyName, CompanyType, Position, StartDate, EndDate, Responsibilities, ReasonForLeaving, GETDATE(), FinalSalary
            FROM OPENJSON(@WorkExperienceList)
            WITH (
                CompanyName NVARCHAR(200), CompanyType NVARCHAR(200), Position NVARCHAR(100),
                StartDate DATE, EndDate DATE, Responsibilities NVARCHAR(MAX),
                ReasonForLeaving NVARCHAR(MAX), FinalSalary NVARCHAR(10)
            );
        END

        -------------------------------------------------------------------
        -- Skills (Replace All)
        -------------------------------------------------------------------
        IF ISJSON(@SkillsList) = 1 AND EXISTS (SELECT 1 FROM OPENJSON(@SkillsList))
        BEGIN
            DELETE FROM SkillsAndCertifications WHERE ApplicantID = @ApplicantID;
            INSERT INTO SkillsAndCertifications (ApplicantID, SkillType, SkillDescription, SkillScore, CreatedDate)
            SELECT @ApplicantID, SkillType, SkillDescription, SkillScore, GETDATE()
            FROM OPENJSON(@SkillsList)
            WITH (
                SkillType NVARCHAR(50), SkillDescription NVARCHAR(200), SkillScore FLOAT
            );
        END

        -------------------------------------------------------------------
        -- Files (Insert + Update only, no delete)
        -------------------------------------------------------------------
		IF ISJSON(@FilesList) = 1 AND EXISTS (SELECT 1 FROM OPENJSON(@FilesList))
		BEGIN
			CREATE TABLE #ParsedFilesTemp (
				FileName NVARCHAR(255), FileSize BIGINT, FileType NVARCHAR(50),
				SectionFile NVARCHAR(50), NewFilePath NVARCHAR(500)
			);

			INSERT INTO #ParsedFilesTemp (FileName, FileSize, FileType, SectionFile, NewFilePath)
			SELECT
				FileName, FileSize, FileType, SectionFile,
				'C:/AppFiles/Applicants/applicant_' + CAST(@ApplicantID AS NVARCHAR(36)) + '/' + FileName
			FROM OPENJSON(@FilesList)
			WITH (
				FileName NVARCHAR(255) '$.FileName', FileSize BIGINT '$.FileSize',
				FileType NVARCHAR(50) '$.FileType', SectionFile NVARCHAR(50) '$.SectionFile'
			);

			UPDATE af
			SET af.FilePath = pf.NewFilePath, af.FileSize = pf.FileSize,
				af.SectionFile = pf.SectionFile, af.UploadedDate = GETDATE(), af.JobID = @JobID
			FROM ApplicantFiles af
			INNER JOIN #ParsedFilesTemp pf
				ON af.ApplicantID = @ApplicantID AND LOWER(af.FileName) = LOWER(pf.FileName) AND af.JobID = @JobID;

			INSERT INTO ApplicantFiles (ApplicantID, FilePath, FileName, FileSize, FileType, UploadedDate, SectionFile, JobID)
			SELECT @ApplicantID, pf.NewFilePath, pf.FileName, pf.FileSize, pf.FileType, GETDATE(), pf.SectionFile, @JobID
			FROM #ParsedFilesTemp pf
			WHERE NOT EXISTS (
				SELECT 1 FROM ApplicantFiles af
				WHERE af.ApplicantID = @ApplicantID AND LOWER(af.FileName) = LOWER(pf.FileName) AND af.JobID = @JobID
			);

			DROP TABLE #ParsedFilesTemp;
		END

        SELECT @HRManagerEmails = COALESCE(STRING_AGG(EMAIL, ','), '')
        FROM AdminUsers WHERE role = '2';

        SET @JobManagerEmails = '';
        SET @OutJobID = @JobID;

        SELECT @JobTitle = JobTitle, @CompanyName = Location
        FROM Jobs WHERE JobID = @JobID;

        SET @ApplicantEmail = @Email;

        COMMIT TRANSACTION;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
        RAISERROR (@ErrorMessage, 16, 1);
    END CATCH
END
GO

-- ============================================================
-- Step 3: แก้ sp_CreateOrPlaceTraineeAssignment
--         map PreferredPosition  → DesiredField1
--              PreferredPositionBackup → DesiredField2
--
-- SP นี้ไม่มีในไฟล์โปรเจกต์ ต้องรัน ALTER ใน SSMS โดยตรง
-- ด้านล่างคือ SQL ที่ต้องเพิ่ม/แก้ใน SP body
-- ============================================================

-- ---- ใน UPDATE T_APPLICANTS SET block ----
-- เพิ่มบรรทัดนี้ต่อท้าย SET columns:
--
--     DesiredField1 = @PreferredPosition,
--     DesiredField2 = @PreferredPositionBackup,
--
-- (ชื่อ parameter ให้ดูจาก SP จริง อาจเป็น @ManualPreferredPosition / @ManualPreferredPositionBackup)

-- ---- ใน INSERT INTO T_APPLICANTS (...) VALUES (...) block ----
-- เพิ่ม column:
--     DesiredField1, DesiredField2,
-- เพิ่ม value:
--     @PreferredPosition, @PreferredPositionBackup,

-- ============================================================
-- ตัวอย่าง snippet สำหรับ UPDATE (copy-paste เข้า SP):
-- ============================================================
/*
UPDATE T_APPLICANTS
SET
    ...columns เดิม...,
    DesiredField1 = @PreferredPosition,
    DesiredField2 = @PreferredPositionBackup
WHERE ApplicantID = @ApplicantID
*/

-- ============================================================
-- ตัวอย่าง snippet สำหรับ INSERT (copy-paste เข้า SP):
-- ============================================================
/*
INSERT INTO T_APPLICANTS (
    ...columns เดิม...,
    DesiredField1,
    DesiredField2
)
VALUES (
    ...values เดิม...,
    @PreferredPosition,
    @PreferredPositionBackup
)
*/
