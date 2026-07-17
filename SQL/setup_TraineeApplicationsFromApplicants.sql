-- GET /api/TraineeApplications (list) and GET /api/TraineeApplications/{id} used to read the
-- legacy TraineeApplications table via sp_GetTraineeApplications / sp_GetTraineeApplicationByID.
-- usp_TraineeApplicant_Upsert stopped writing to that table when trainee applications moved to
-- T_APPLICANTS + JobApplications (see setup_TraineeApplicationsApplicantLink.sql), so both procs
-- have queried a table nothing writes to anymore -- GetAll always returns [], GetById always 404s.
-- Point them at T_APPLICANTS + JobApplications (+ T_APPLICANT_FILES) instead, matching where
-- usp_TraineeApplicant_Upsert actually writes. The legacy TraineeApplications table/proc bodies
-- are left alone elsewhere (nothing to migrate, no rows in that table).

CREATE OR ALTER PROCEDURE sp_GetTraineeApplications
    @Status NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.ApplicantID AS TraineeApplicationID,
        a.ApplicantID,
        b.ApplicationID,
        b.JobID,
        a.InternshipStartDate AS StartDate,
        a.InternshipEndDate AS EndDate,
        a.DesiredField1,
        a.DesiredField2,
        a.DesiredField3,
        a.InternshipType,
        a.DurationMonths,
        a.ReasonPosition AS Reason,
        a.ReasonOther,
        a.Title AS PrefixT,
        a.FirstNameThai AS NameFirstT,
        a.LastNameThai AS NameLastT,
        a.Nickname AS NicknameT,
        a.TitleENG AS PrefixE,
        a.FirstNameEng AS NameFirstE,
        a.LastNameEng AS NameLastE,
        a.NicknameE,
        CASE a.Gender WHEN 'M' THEN 'male' WHEN 'F' THEN 'female' WHEN 'O' THEN 'other' ELSE NULL END AS Gender,
        a.BirthDate AS DateOfBirth,
        a.Age,
        a.PlaceOfBirth,
        a.Nationality,
        a.Race,
        a.Religion,
        a.Height,
        a.Weight,
        a.CitizenID AS IDCardNo,
        a.CitizenIDIssuedBy AS IDIssuedBy,
        a.CitizenIDExpiresON AS IDExpiredDate,
        a.CurrentAddress AS Address,
        a.CurrentProvinceID AS ProvinceID,
        a.CurrentDistrictID AS DistrictID,
        a.CurrentSubDistrictID AS SubDistrictID,
        a.CurrentPostalCode AS PostalCode,
        a.HomePhone AS Telephone,
        a.MobilePhone AS Mobile,
        a.Email,
        a.FatherName,
        a.FatherOccupation,
        a.FatherStatus,
        a.MotherName,
        a.MotherOccupation,
        a.MotherStatus,
        a.SiblingsAll AS SiblingCount,
        a.SiblingOrder,
        a.EmergencyName,
        a.EmergencyRelation,
        a.EmergencyAddress,
        a.EmergencyPhone,
        a.School,
        a.Faculty,
        a.Major,
        a.Minor,
        a.YearOfStudy,
        a.GPA,
        a.AdvisorName,
        a.AdvisorPhone,
        a.Activities,
        a.InfoSources,
        a.InfoSourceStaffName,
        a.InfoSourceDepartment,
        a.InfoSourceOther,
        b.Status,
        a.CreatedDate AS CreatedAt,
        a.ModifiedDate AS UpdatedAt,
        fc.FileCount,
        CASE WHEN fc.FileCount > 0 THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS HasFiles
    FROM T_APPLICANTS a
    INNER JOIN JobApplications b ON b.ApplicantID = a.ApplicantID
    LEFT JOIN Jobs j ON j.JobID = b.JobID
    -- Lightweight count only (list view) -- same two sources sp_GetTraineeApplicationByID's
    -- files result set unions (T_APPLICANT_FILES by ApplicantID, TraineeAssignmentFiles via
    -- TraineeAssignments by ApplicationID), just counted instead of returned in full.
    OUTER APPLY (
        SELECT COUNT(*) AS FileCount
        FROM (
            SELECT FileID FROM T_APPLICANT_FILES WHERE ApplicantID = a.ApplicantID
            UNION ALL
            SELECT F.FileID
            FROM TraineeAssignments TA
            INNER JOIN TraineeAssignmentFiles F ON F.AssignmentID = TA.AssignmentID
            WHERE TA.ApplicationID = b.ApplicationID AND TA.Status <> 'Cancelled'
        ) allFiles
    ) fc
    WHERE (
        j.EmployeeType = N'นักศึกษาฝึกงาน'
        OR (b.JobID IS NULL AND (a.InternshipStartDate IS NOT NULL OR a.InternshipType IS NOT NULL))
    )
    AND (@Status IS NULL OR b.Status = @Status)
    ORDER BY a.CreatedDate DESC
END
GO

CREATE OR ALTER PROCEDURE sp_GetTraineeApplicationByID
    @AssignmentID INT = NULL,
    @ApplicationID INT = NULL,
    @ApplicantID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Same AssignmentID -> ApplicationID resolution usp_TraineeApplicant_Upsert uses when a
    -- submission continues a Part1 JobSlotAssignments row.
    IF @ApplicationID IS NULL AND @AssignmentID IS NOT NULL
        SELECT @ApplicationID = ApplicationID FROM JobSlotAssignments WHERE AssignmentID = @AssignmentID

    DECLARE @ResolvedApplicantID INT

    SELECT TOP 1
        @ResolvedApplicantID = a.ApplicantID,
        @ApplicationID = b.ApplicationID
    FROM T_APPLICANTS a
    INNER JOIN JobApplications b ON b.ApplicantID = a.ApplicantID
    WHERE (@ApplicationID IS NOT NULL AND b.ApplicationID = @ApplicationID)
       OR (@ApplicationID IS NULL AND @ApplicantID IS NOT NULL AND a.ApplicantID = @ApplicantID)

    SELECT
        a.ApplicantID AS TraineeApplicationID,
        a.ApplicantID,
        b.ApplicationID,
        b.JobID,
        a.InternshipStartDate AS StartDate,
        a.InternshipEndDate AS EndDate,
        a.DesiredField1,
        a.DesiredField2,
        a.DesiredField3,
        a.InternshipType,
        a.DurationMonths,
        a.ReasonPosition AS Reason,
        a.ReasonOther,
        a.Title AS PrefixT,
        a.FirstNameThai AS NameFirstT,
        a.LastNameThai AS NameLastT,
        a.Nickname AS NicknameT,
        a.TitleENG AS PrefixE,
        a.FirstNameEng AS NameFirstE,
        a.LastNameEng AS NameLastE,
        a.NicknameE,
        CASE a.Gender WHEN 'M' THEN 'male' WHEN 'F' THEN 'female' WHEN 'O' THEN 'other' ELSE NULL END AS Gender,
        a.BirthDate AS DateOfBirth,
        a.Age,
        a.PlaceOfBirth,
        a.Nationality,
        a.Race,
        a.Religion,
        a.Height,
        a.Weight,
        a.CitizenID AS IDCardNo,
        a.CitizenIDIssuedBy AS IDIssuedBy,
        a.CitizenIDExpiresON AS IDExpiredDate,
        a.CurrentAddress AS Address,
        a.CurrentProvinceID AS ProvinceID,
        a.CurrentDistrictID AS DistrictID,
        a.CurrentSubDistrictID AS SubDistrictID,
        a.CurrentPostalCode AS PostalCode,
        a.HomePhone AS Telephone,
        a.MobilePhone AS Mobile,
        a.Email,
        a.FatherName,
        a.FatherOccupation,
        a.FatherStatus,
        a.MotherName,
        a.MotherOccupation,
        a.MotherStatus,
        a.SiblingsAll AS SiblingCount,
        a.SiblingOrder,
        a.EmergencyName,
        a.EmergencyRelation,
        a.EmergencyAddress,
        a.EmergencyPhone,
        -- Admin manual-entry trainees (sp_CreateManualTraineeManagement) can leave T_APPLICANTS.School
        -- NULL; the university they typed lands on TraineeAssignments.ManualUniversity instead. Fall
        -- back to that so the view doesn't show an empty School for those rows.
        COALESCE(a.School, (
            SELECT TOP 1 ta.ManualUniversity
            FROM TraineeAssignments ta
            WHERE ta.ApplicationID = b.ApplicationID AND ta.Status <> 'Cancelled'
            ORDER BY ta.AssignmentID DESC
        )) AS School,
        a.Faculty,
        a.Major,
        a.Minor,
        a.YearOfStudy,
        a.GPA,
        a.AdvisorName,
        a.AdvisorPhone,
        a.Activities,
        a.InfoSources,
        a.InfoSourceStaffName,
        a.InfoSourceDepartment,
        a.InfoSourceOther,
        b.Status,
        a.CreatedDate AS CreatedAt,
        a.ModifiedDate AS UpdatedAt
    FROM T_APPLICANTS a
    INNER JOIN JobApplications b ON b.ApplicantID = a.ApplicantID
    WHERE a.ApplicantID = @ResolvedApplicantID

    -- Files can land in either of two tables depending on which flow the applicant went
    -- through: T_APPLICANT_FILES (Part2 form upload, keyed by ApplicantID) or
    -- TraineeAssignmentFiles (walk-in/department-assignment upload in TraineeController,
    -- keyed by TraineeAssignments.AssignmentID). sp_CreateTraineeManagementAssignment already
    -- links TraineeAssignments.ApplicationID back to this same JobApplications.ApplicationID
    -- when reusing a Part2 submission, so join through that to surface both.
    --
    -- FilePath is stored as whatever absolute path INetworkShareService.GetBasePath() resolved to at
    -- upload time -- a UNC network path with a host baked in ("//10.2.0.11/AppFiles/...") under
    -- Windows dev, or a local drive path ("C:/AppFiles/...") in Production. Frontend prepends its own
    -- API base URL, so that prefix doubles up into a broken URL. Normalize to keep only from
    -- "AppFiles/" onward regardless of what preceded it (see setup_ApplicantDataFormTraineeFiles.sql
    -- for the same fix applied to sp_GetApplicantDataAllForForm).
    SELECT
        f.FileID,
        @ResolvedApplicantID AS TraineeApplicationID,
        CASE
            WHEN CHARINDEX('AppFiles/', REPLACE(f.FilePath, '\', '/')) > 0
                THEN SUBSTRING(REPLACE(f.FilePath, '\', '/'), CHARINDEX('AppFiles/', REPLACE(f.FilePath, '\', '/')), 4000)
            ELSE REPLACE(f.FilePath, '\', '/')
        END AS FilePath,
        f.FileName,
        f.FileSize,
        f.FileType,
        f.SectionFile,
        f.UploadedDate AS CreatedDate,
        f.UploadedDate
    FROM T_APPLICANT_FILES f
    WHERE f.ApplicantID = @ResolvedApplicantID

    UNION ALL

    SELECT
        F.FileID,
        @ResolvedApplicantID AS TraineeApplicationID,
        CASE
            WHEN CHARINDEX('AppFiles/', REPLACE(F.FilePath, '\', '/')) > 0
                THEN SUBSTRING(REPLACE(F.FilePath, '\', '/'), CHARINDEX('AppFiles/', REPLACE(F.FilePath, '\', '/')), 4000)
            ELSE REPLACE(F.FilePath, '\', '/')
        END AS FilePath,
        F.FileName,
        F.FileSize,
        F.FileType,
        F.SectionFile,
        F.UploadedDate AS CreatedDate,
        F.UploadedDate
    FROM TraineeAssignments TA
    INNER JOIN TraineeAssignmentFiles F ON F.AssignmentID = TA.AssignmentID
    WHERE TA.ApplicationID = @ApplicationID
      AND TA.Status <> 'Cancelled'
END
GO
