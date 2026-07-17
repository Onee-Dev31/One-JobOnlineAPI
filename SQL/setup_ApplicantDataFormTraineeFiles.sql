-- GET /api/ApplicantNew/GetApplicantDataForForm (sp_GetApplicantDataAllForForm) built its FilesList
-- from ApplicantFiles only. Applicants who applied to a "นักศึกษาฝึกงาน" job posting can have their
-- real uploaded documents in TraineeAssignmentFiles instead (attached via the department-assignment
-- flow in TraineeController -> TraineeAssignments, see setup_TraineeApplicationsFromApplicants.sql
-- for the same gap already fixed on the trainee-specific endpoints) -- so FilesList came back null
-- even though the applicant had files, e.g. ApplicantID=3/JobID=1 (resume + transcript sitting in
-- TraineeAssignmentFiles via TraineeAssignments.ApplicationID = JobApplications.ApplicationID = 4).
-- UNION that source in, correlated to the specific JobApplications row (ja.ApplicationID) rather than
-- just ApplicantID, so files from a different application aren't pulled in.
--
-- SectionFile values from TraineeAssignmentFiles are 'resume'/'transcript'/'idCard'/'houseReg', NOT
-- 'Section1'/'Section2' like ApplicantFiles -- left as-is (not relabeled) since they're genuinely a
-- different document type; frontend needs to widen its SectionFile filter to see them (see
-- frontend_applicantnew_filelist_traineefiles_prompt.md).
--
-- FilePath is stored as whatever absolute path INetworkShareService.GetBasePath() resolved to at
-- upload time (Path.Combine(GetBasePath(), ...), see TraineeController.SaveFilesAsync /
-- FileProcessingService.ProcessFilesAsync) -- that's a UNC network path with a host baked in
-- ("//10.2.0.11/AppFiles/...") when the API runs as Windows dev against FileStorage:NetworkPath, or a
-- local drive path ("C:/AppFiles/...") under FileStorage:ProductionPath. Frontend prepends its own API
-- base URL, so any of these prefixes double up into a broken URL. Normalize by keeping only from
-- "AppFiles/" onward, regardless of what host/drive preceded it -- same fix applied everywhere else
-- FilePath is read from ApplicantFiles/TraineeAssignmentFiles (sp_GetTraineeApplicationByID and the
-- trainee-management overview/seed-data procs in setup_TraineeManagementV2.sql).
CREATE OR ALTER PROCEDURE [dbo].[sp_GetApplicantDataAllForForm]
    @ApplicantID INT = NULL,
    @JobID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @JobID IS NOT NULL AND @JobID <> '' AND @JobID <> 0
    BEGIN
        SELECT TOP 1
            a.ApplicantID,
            a.UserId,
            a.Title,
            a.TitleENG,
            a.Gender,
            a.FirstNameThai,
            a.LastNameThai,
            a.FirstNameEng,
            a.LastNameEng,
            a.Nickname,
            a.ThaiNationality,
            a.NicknameE,
            a.BirthDate,
            a.Weight,
            a.Height,
            a.CitizenID,
            a.CitizenIDIssuedBy,
            a.CitizenIDExpiresON,
            a.CurrentAddress,
            a.CurrentProvinceID,
            a.CurrentDistrictID,
            a.CurrentSubDistrictID,
            a.CurrentPostalCode,
            a.MobilePhone,
            a.LINE,
            a.Email,
            a.RegisteredAddress,
            a.RegisteredProvinceID,
            a.RegisteredDistrictID,
            a.RegisteredSubDistrictID,
            a.RegisteredPostalCode,
            a.MaritalStatus,
            a.SiblingsAll,
            a.NumberAY,
            a.SpouseFullName,
            a.SpouseOccupation,
            a.SpouseCompanyType,
            a.SpouseCompanyAddress,
            a.SpouseMobilePhone,
            a.SpouseLINE,
            a.SpouseEmail,
            a.SpouseAliveStatus,
            a.MaleChildren,
            a.FemaleChildren,
            a.ReasonPosition,
            a.MinitaryService,
            a.ReasonMinitary,
            a.MaritalStatus,
            a.QuestionnaireVehiclesMotorcycle,
            a.MotorcycleLicense,
            a.QuestionnaireDisabilities,
            a.QuestionnaireConvicted,
            a.QuestionnaireFiredjob,
            a.QuestionnaireApplyjob,
            a.QuestionnaireWorkShifts,
            a.QuestionnaireCheckInformation,
            a.QuestionnaireRelative,
            a.QuestionnaireVehiclesCar,
            a.CarLicense,
            a.ReasonDisabilities,
            a.CodeMPID,
            ja.Salary,
            -- "ตำแหน่งอื่นที่สนใจ" for a trainee job posting isn't collected via JobApplications.JobOtherName
            -- (that column is only ever populated by the regular job-application form) -- it's the backup
            -- position captured on JobSlotAssignments (batch/self-apply Part1 flow) or TraineeAssignments
            -- (admin department-assignment flow), both keyed by ja.ApplicationID. Prefer JobSlotAssignments
            -- since it's the earlier stage of the flow, fall back to TraineeAssignments.
            CASE
                WHEN c.EmployeeType = N'นักศึกษาฝึกงาน' THEN COALESCE(
                    (SELECT TOP 1 jsa.ManualPreferredPositionBackup FROM JobSlotAssignments jsa
                     WHERE jsa.ApplicationID = ja.ApplicationID AND jsa.Status <> 'Cancelled'
                     ORDER BY jsa.AssignmentID DESC),
                    (SELECT TOP 1 ta.ManualPreferredPositionBackup FROM TraineeAssignments ta
                     WHERE ta.ApplicationID = ja.ApplicationID AND ta.Status <> 'Cancelled'
                     ORDER BY ta.AssignmentID DESC)
                )
                ELSE ja.JobOtherName
            END AS JobOtherName,

            -- Current address
            p.ProvinceNameThai,
            d.DistrictNameThai,
            sd.SubDistrictNameThai,
            sd.PostalCode,

            -- Registered address
            rp.ProvinceNameThai AS RegisteredProvinceThai,
            rd.DistrictNameThai AS RegisteredDistrictThai,
            rsd.SubDistrictNameThai AS RegisteredSubDistrictThai,

            ja.JobID,
            ja.JobStartDate,
            c.JobTitle,
            c.Location,
            c.Department,
            CASE
                WHEN ISNULL(c.OpenFor, '') = '' THEN em.CODEMPID
                ELSE c.OpenFor
            END AS OpenFor,

            files.FilesList,
            edu.EducationList,
            work.WorkExperienceList,
            skills.SkillsList,
            rel.RelationshipList

        FROM T_APPLICANTS a
        LEFT JOIN JobApplications ja ON a.ApplicantID = ja.ApplicantID
        LEFT JOIN Jobs c ON ja.JobID = c.JobID

        -- Current Address Join
        LEFT JOIN Provinces p ON a.CurrentProvinceID = p.ProvinceCode
        LEFT JOIN Districts d ON a.CurrentDistrictID = d.DistrictID
        LEFT JOIN SubDistricts sd ON a.CurrentSubDistrictID = sd.SubDistrictID

        -- Registered Address Join
        LEFT JOIN Provinces rp ON a.RegisteredProvinceID = rp.ProvinceCode
        LEFT JOIN Districts rd ON a.RegisteredDistrictID = rd.DistrictID
        LEFT JOIN SubDistricts rsd ON a.RegisteredSubDistrictID = rsd.SubDistrictID

        LEFT JOIN [HRMS_LINKED_SERVER].HRMS.Dbo.T_EMPLOYEE em ON c.CreatedByRole = em.AD_USER

        -- JSON Aggregates
        OUTER APPLY (
            SELECT (
                SELECT * FROM (
                    SELECT
                        f.FileID,
                        CASE
                            WHEN CHARINDEX('AppFiles/', REPLACE(f.FilePath, '\', '/')) > 0
                                THEN SUBSTRING(REPLACE(f.FilePath, '\', '/'), CHARINDEX('AppFiles/', REPLACE(f.FilePath, '\', '/')), 4000)
                            ELSE REPLACE(f.FilePath, '\', '/')
                        END AS FilePath,
                        f.FileName, f.SectionFile, f.JobID
                    FROM ApplicantFiles f
                    WHERE f.ApplicantID = a.ApplicantID
                        AND (@JobID IS NULL OR f.JobID = @JobID)

                    UNION ALL

                    SELECT
                        F.FileID,
                        CASE
                            WHEN CHARINDEX('AppFiles/', REPLACE(F.FilePath, '\', '/')) > 0
                                THEN SUBSTRING(REPLACE(F.FilePath, '\', '/'), CHARINDEX('AppFiles/', REPLACE(F.FilePath, '\', '/')), 4000)
                            ELSE REPLACE(F.FilePath, '\', '/')
                        END AS FilePath,
                        F.FileName, F.SectionFile, ja.JobID
                    FROM TraineeAssignments TA
                    INNER JOIN TraineeAssignmentFiles F ON F.AssignmentID = TA.AssignmentID
                    WHERE TA.ApplicationID = ja.ApplicationID
                        AND TA.Status <> 'Cancelled'
                ) allFiles
                FOR JSON PATH
            ) AS FilesList
        ) files
        OUTER APPLY (
            -- Trainee-flow applicants (usp_TraineeApplicant_Upsert) never write to Education --
            -- their education is stored flat on T_APPLICANTS (University/Faculty/Major/GPA) instead.
            -- Union that in whenever present so EducationList isn't null for them.
            SELECT (
                SELECT * FROM (
                    SELECT EducationLevel, InstitutionName,
                           StartYear + 543 AS StartYear, EndYear + 543 AS EndYear,
                           Major, GPA, ProvinceEducation, Faculty
                    FROM Education e
                    WHERE e.ApplicantID = a.ApplicantID

                    UNION ALL

                    SELECT
                        NULL AS EducationLevel,
                        ISNULL(a.University, a.School) AS InstitutionName,
                        NULL AS StartYear,
                        NULL AS EndYear,
                        a.Major,
                        a.GPA,
                        NULL AS ProvinceEducation,
                        a.Faculty
                    WHERE a.University IS NOT NULL OR a.Faculty IS NOT NULL
                ) allEdu
                FOR JSON PATH
            ) AS EducationList
        ) edu
        OUTER APPLY (
            SELECT (
                SELECT
                    CompanyName,
                    CompanyType,
                    Position,
                    DATEADD(YEAR, 543, StartDate) AS StartDate,
                    DATEADD(YEAR, 543, EndDate)   AS EndDate,
                    Responsibilities,
                    ReasonForLeaving,
                    FinalSalary,
                    CASE
                        WHEN EndDate IS NULL OR EndDate > GETDATE()
                             THEN CAST(1 AS bit) ELSE CAST(0 AS bit)
                    END AS IsCurrentJob
                FROM WorkExperience w
                WHERE w.ApplicantID = a.ApplicantID
                ORDER BY ISNULL(EndDate, GETDATE()) DESC
                FOR JSON PATH
            ) AS WorkExperienceList
        ) work
        OUTER APPLY (
            SELECT (SELECT SkillType, SkillDescription, SkillScore
                    FROM SkillsAndCertifications s
                    WHERE s.ApplicantID = a.ApplicantID
                    FOR JSON PATH) AS SkillsList
        ) skills
        OUTER APPLY (
            SELECT (SELECT r.RELATION_TYPE, r.NAMESURNAME, r.RELATION_DESCRIPTION,
                           r.CAREER, r.COMPANY, r.AGE, r.MOBILE, r.ALIVE_STATUS, r.ADDRESS
                    FROM T_RELATIONSHIP r
                    WHERE r.APPLICANT_ID = a.ApplicantID
                        AND (@JobID IS NULL OR r.JobID = @JobID)
                    FOR JSON PATH) AS RelationshipList
        ) rel

        WHERE (@ApplicantID IS NULL OR a.ApplicantID = @ApplicantID)
          AND (@JobID IS NULL OR ja.JobID = @JobID)
        ORDER BY a.ApplicantID DESC;
    END
    ELSE
    BEGIN
        SELECT
            a.ApplicantID,
            a.UserId,
            a.Title,
            a.TitleENG,
            a.Gender,
            a.FirstNameThai,
            a.LastNameThai,
            a.FirstNameEng,
            a.LastNameEng,
            a.Nickname,
            a.ThaiNationality,
            a.NicknameE,
            a.BirthDate,
            a.Weight,
            a.Height,
            a.CitizenID,
            a.CitizenIDIssuedBy,
            a.CitizenIDExpiresON,
            a.CurrentAddress,
            a.CurrentProvinceID,
            a.CurrentDistrictID,
            a.CurrentSubDistrictID,
            a.CurrentPostalCode,
            a.MobilePhone,
            a.LINE,
            a.Email,
            a.SiblingsAll,
            a.NumberAY,
            a.RegisteredAddress,
            a.RegisteredProvinceID,
            a.RegisteredDistrictID,
            a.RegisteredSubDistrictID,
            a.RegisteredPostalCode,
            a.MaritalStatus,
            a.SpouseFullName,
            a.SpouseOccupation,
            a.SpouseCompanyType,
            a.SpouseCompanyAddress,
            a.SpouseMobilePhone,
            a.SpouseLINE,
            a.SpouseEmail,
            a.SpouseAliveStatus,
            a.MaleChildren,
            a.FemaleChildren,
            a.ReasonPosition,
            a.MinitaryService,
            a.ReasonMinitary,
            a.MaritalStatus,
            a.QuestionnaireVehiclesMotorcycle,
            a.MotorcycleLicense,
            a.QuestionnaireDisabilities,
            a.QuestionnaireConvicted,
            a.QuestionnaireFiredjob,
            a.QuestionnaireApplyjob,
            a.QuestionnaireWorkShifts,
            a.QuestionnaireCheckInformation,
            a.QuestionnaireRelative,
            a.QuestionnaireVehiclesCar,
            a.CarLicense,
            a.ReasonDisabilities,
            a.CodeMPID,
            ja.Salary,
            -- "ตำแหน่งอื่นที่สนใจ" for a trainee job posting isn't collected via JobApplications.JobOtherName
            -- (that column is only ever populated by the regular job-application form) -- it's the backup
            -- position captured on JobSlotAssignments (batch/self-apply Part1 flow) or TraineeAssignments
            -- (admin department-assignment flow), both keyed by ja.ApplicationID. Prefer JobSlotAssignments
            -- since it's the earlier stage of the flow, fall back to TraineeAssignments.
            CASE
                WHEN c.EmployeeType = N'นักศึกษาฝึกงาน' THEN COALESCE(
                    (SELECT TOP 1 jsa.ManualPreferredPositionBackup FROM JobSlotAssignments jsa
                     WHERE jsa.ApplicationID = ja.ApplicationID AND jsa.Status <> 'Cancelled'
                     ORDER BY jsa.AssignmentID DESC),
                    (SELECT TOP 1 ta.ManualPreferredPositionBackup FROM TraineeAssignments ta
                     WHERE ta.ApplicationID = ja.ApplicationID AND ta.Status <> 'Cancelled'
                     ORDER BY ta.AssignmentID DESC)
                )
                ELSE ja.JobOtherName
            END AS JobOtherName,
            p.ProvinceNameThai,
            d.DistrictNameThai,
            sd.SubDistrictNameThai,
            sd.PostalCode,

            rp.ProvinceNameThai AS RegisteredProvinceThai,
            rd.DistrictNameThai AS RegisteredDistrictThai,
            rsd.SubDistrictNameThai AS RegisteredSubDistrictThai,

            ja.JobID,
            ja.JobStartDate,
            c.JobTitle,
            c.Location,
            c.Department,
            CASE
                WHEN ISNULL(c.OpenFor, '') = '' THEN em.CODEMPID
                ELSE c.OpenFor
            END AS OpenFor,

            files.FilesList,
            edu.EducationList,
            work.WorkExperienceList,
            skills.SkillsList,
            rel.RelationshipList

        FROM T_APPLICANTS a
        LEFT JOIN JobApplications ja ON a.ApplicantID = ja.ApplicantID
        LEFT JOIN Jobs c ON ja.JobID = c.JobID

        -- Current Address Join
        LEFT JOIN Provinces p ON a.CurrentProvinceID = p.ProvinceCode
        LEFT JOIN Districts d ON a.CurrentDistrictID = d.DistrictID
        LEFT JOIN SubDistricts sd ON a.CurrentSubDistrictID = sd.SubDistrictID

        -- Registered Address Join
        LEFT JOIN Provinces rp ON a.RegisteredProvinceID = rp.ProvinceCode
        LEFT JOIN Districts rd ON a.RegisteredDistrictID = rd.DistrictID
        LEFT JOIN SubDistricts rsd ON a.RegisteredSubDistrictID = rsd.SubDistrictID

        LEFT JOIN [HRMS_LINKED_SERVER].HRMS.Dbo.T_EMPLOYEE em ON c.CreatedByRole = em.AD_USER

        -- JSON Aggregates
        OUTER APPLY (
            SELECT (
                SELECT * FROM (
                    SELECT
                        f.FileID,
                        CASE
                            WHEN CHARINDEX('AppFiles/', REPLACE(f.FilePath, '\', '/')) > 0
                                THEN SUBSTRING(REPLACE(f.FilePath, '\', '/'), CHARINDEX('AppFiles/', REPLACE(f.FilePath, '\', '/')), 4000)
                            ELSE REPLACE(f.FilePath, '\', '/')
                        END AS FilePath,
                        f.FileName, f.SectionFile, f.JobID
                    FROM ApplicantFiles f
                    WHERE f.ApplicantID = a.ApplicantID
                        AND (@JobID IS NULL OR f.JobID = @JobID)

                    UNION ALL

                    SELECT
                        F.FileID,
                        CASE
                            WHEN CHARINDEX('AppFiles/', REPLACE(F.FilePath, '\', '/')) > 0
                                THEN SUBSTRING(REPLACE(F.FilePath, '\', '/'), CHARINDEX('AppFiles/', REPLACE(F.FilePath, '\', '/')), 4000)
                            ELSE REPLACE(F.FilePath, '\', '/')
                        END AS FilePath,
                        F.FileName, F.SectionFile, ja.JobID
                    FROM TraineeAssignments TA
                    INNER JOIN TraineeAssignmentFiles F ON F.AssignmentID = TA.AssignmentID
                    WHERE TA.ApplicationID = ja.ApplicationID
                        AND TA.Status <> 'Cancelled'
                ) allFiles
                FOR JSON PATH
            ) AS FilesList
        ) files
        OUTER APPLY (
            -- Trainee-flow applicants (usp_TraineeApplicant_Upsert) never write to Education --
            -- their education is stored flat on T_APPLICANTS (University/Faculty/Major/GPA) instead.
            -- Union that in whenever present so EducationList isn't null for them.
            SELECT (
                SELECT * FROM (
                    SELECT EducationLevel, InstitutionName,
                           StartYear + 543 AS StartYear, EndYear + 543 AS EndYear,
                           Major, GPA, ProvinceEducation, Faculty
                    FROM Education e
                    WHERE e.ApplicantID = a.ApplicantID

                    UNION ALL

                    SELECT
                        NULL AS EducationLevel,
                        ISNULL(a.University, a.School) AS InstitutionName,
                        NULL AS StartYear,
                        NULL AS EndYear,
                        a.Major,
                        a.GPA,
                        NULL AS ProvinceEducation,
                        a.Faculty
                    WHERE a.University IS NOT NULL OR a.Faculty IS NOT NULL
                ) allEdu
                FOR JSON PATH
            ) AS EducationList
        ) edu
        OUTER APPLY (
            SELECT (
                SELECT
                    CompanyName,
                    CompanyType,
                    Position,
                    DATEADD(YEAR, 543, StartDate) AS StartDate,
                    DATEADD(YEAR, 543, EndDate)   AS EndDate,
                    Responsibilities,
                    ReasonForLeaving,
                    FinalSalary,
                    CASE
                        WHEN EndDate IS NULL OR EndDate > GETDATE()
                             THEN CAST(1 AS bit) ELSE CAST(0 AS bit)
                    END AS IsCurrentJob
                FROM WorkExperience w
                WHERE w.ApplicantID = a.ApplicantID
                ORDER BY ISNULL(EndDate, GETDATE()) DESC
                FOR JSON PATH
            ) AS WorkExperienceList
        ) work
        OUTER APPLY (
            SELECT (SELECT SkillType, SkillDescription, SkillScore
                    FROM SkillsAndCertifications s
                    WHERE s.ApplicantID = a.ApplicantID
                    FOR JSON PATH) AS SkillsList
        ) skills
        OUTER APPLY (
            SELECT (SELECT r.RELATION_TYPE, r.NAMESURNAME, r.RELATION_DESCRIPTION,
                           r.CAREER, r.COMPANY, r.AGE, r.MOBILE, r.ALIVE_STATUS, r.ADDRESS
                    FROM T_RELATIONSHIP r
                    WHERE r.APPLICANT_ID = a.ApplicantID
                        AND (@JobID IS NULL OR r.JobID = @JobID)
                    FOR JSON PATH) AS RelationshipList
        ) rel

        WHERE (@ApplicantID IS NULL OR a.ApplicantID = @ApplicantID)
          AND (@JobID IS NULL OR ja.JobID = @JobID);
    END
END
GO
