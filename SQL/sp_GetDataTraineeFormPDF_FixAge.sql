-- Fix: duplicate Age column — Dapper reads first (computed, NULL when BirthDate is NULL)
-- Consolidate to COALESCE(a.Age, computed) so stored value takes priority

ALTER PROCEDURE [dbo].[sp_GetDataTraineeFormPDF]
    @ApplicantID INT,
    @JobID       INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        CASE
            WHEN NULLIF(LTRIM(RTRIM(a.Title)), N'') IS NULL THEN N''
            WHEN LTRIM(RTRIM(a.Title)) = N'ไม่ระบุ' THEN N''
            ELSE a.Title
        END AS Title,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(a.TitleENG)), N'') IS NULL THEN N''
            WHEN LTRIM(RTRIM(a.TitleENG)) = N'Not specified' THEN N''
            ELSE a.TitleENG
        END AS TitleENG,
        a.FirstNameThai,
        a.LastNameThai,
        ISNULL(a.Nickname, '')                                       AS Nickname,
        ISNULL(a.FirstNameEng, '')                                   AS FirstNameEng,
        ISNULL(a.LastNameEng, '')                                    AS LastNameEng,
        ISNULL(a.NicknameE, '')                                      AS NicknameE,

        CONCAT(
            CASE
                WHEN NULLIF(LTRIM(RTRIM(a.Title)), N'') IS NULL THEN N''
                WHEN LTRIM(RTRIM(a.Title)) = N'ไม่ระบุ' THEN N''
                ELSE a.Title
            END,
            a.FirstNameThai,
            N' ',
            a.LastNameThai
        )                                                            AS FullNameThai,

        CONVERT(NVARCHAR(10), a.BirthDate, 103)                      AS DateOfBirth,

        COALESCE(
            a.Age,
            DATEDIFF(YEAR, a.BirthDate, GETDATE())
            - CASE
                WHEN DATEADD(YEAR, DATEDIFF(YEAR, a.BirthDate, GETDATE()), a.BirthDate) > CAST(GETDATE() AS DATE)
                    THEN 1
                ELSE 0
              END
        )                                                            AS Age,

        ISNULL(a.PlaceOfBirth, '')                                   AS PlaceOfBirth,
        ISNULL(a.Nationality, '')                                    AS Nationality,
        ISNULL(a.Race, '')                                           AS Race,
        ISNULL(a.Religion, '')                                       AS Religion,
        ISNULL(CAST(a.Height AS NVARCHAR(20)), '')                   AS Height,
        ISNULL(CAST(a.Weight AS NVARCHAR(20)), '')                   AS Weight,
        ISNULL(a.CitizenID, '')                                      AS IDCardNo,
        ISNULL(a.CitizenIDIssuedBy, '')                              AS IDIssuedBy,
        CONVERT(NVARCHAR(10), a.CitizenIDExpiresON, 103)             AS IDExpiredDate,

        ISNULL(a.CurrentAddress, '')                                 AS CurrentAddress,
        a.CurrentProvinceID,
        a.CurrentDistrictID,
        a.CurrentSubDistrictID,
        ISNULL(a.CurrentPostalCode, '')                              AS CurrentPostalCode,

        ISNULL(p.ProvinceNameThai, '')                               AS CurrentProvinceName,
        ISNULL(d2.DistrictNameThai, '')                              AS CurrentDistrictName,
        ISNULL(sd.SubDistrictNameThai, '')                           AS CurrentSubDistrictName,

        CONCAT_WS(
            N' ',
            NULLIF(LTRIM(RTRIM(a.CurrentAddress)), N''),
            NULLIF(LTRIM(RTRIM(sd.SubDistrictNameThai)), N''),
            NULLIF(LTRIM(RTRIM(d2.DistrictNameThai)), N''),
            NULLIF(LTRIM(RTRIM(p.ProvinceNameThai)), N''),
            NULLIF(LTRIM(RTRIM(a.CurrentPostalCode)), N'')
        )                                                            AS FullAddress,

        ISNULL(a.HomePhone, '')                                      AS HomePhone,
        ISNULL(a.MobilePhone, '')                                    AS MobilePhone,
        ISNULL(a.Email, '')                                          AS Email,

        ISNULL(a.FatherName, '')                                     AS FatherName,
        ISNULL(a.FatherOccupation, '')                               AS FatherOccupation,
        ISNULL(a.FatherStatus, '')                                   AS FatherStatus,
        ISNULL(a.MotherName, '')                                     AS MotherName,
        ISNULL(a.MotherOccupation, '')                               AS MotherOccupation,
        ISNULL(a.MotherStatus, '')                                   AS MotherStatus,
        ISNULL(CAST(a.SiblingsAll AS NVARCHAR(10)), '')              AS SiblingsAll,
        ISNULL(CAST(a.SiblingOrder AS NVARCHAR(10)), '')             AS SiblingOrder,

        ISNULL(a.EmergencyName, '')                                  AS EmergencyName,
        ISNULL(a.EmergencyRelation, '')                              AS EmergencyRelation,
        ISNULL(a.EmergencyAddress, '')                               AS EmergencyAddress,
        ISNULL(a.EmergencyPhone, '')                                 AS EmergencyPhone,

        ISNULL(a.University, '')                                     AS School,
        ISNULL(a.Faculty, '')                                        AS Faculty,
        ISNULL(a.Major, '')                                          AS Major,
        ISNULL(a.Minor, '')                                          AS Minor,
        ISNULL(a.YearOfStudy, '')                                    AS YearOfStudy,
        ISNULL(a.AdvisorName, '')                                    AS AdvisorName,
        ISNULL(a.AdvisorPhone, '')                                   AS AdvisorPhone,
        ISNULL(a.Activities, '')                                     AS Activities,

        ISNULL(a.InfoSources, '')                                    AS InfoSources,
        ISNULL(a.InfoSourceStaffName, '')                            AS InfoSourceStaffName,
        ISNULL(a.InfoSourceDepartment, '')                           AS InfoSourceDepartment,
        ISNULL(a.InfoSourceOther, '')                                AS InfoSourceOther,

        ISNULL(a.DesiredField1, '')                                  AS DesiredField1,
        ISNULL(a.DesiredField2, '')                                  AS DesiredField2,
        ISNULL(a.DesiredField3, '')                                  AS DesiredField3,
        ISNULL(a.InternshipType, '')                                 AS InternshipType,
        ISNULL(a.ReasonPosition, '')                                 AS Reason,
        ISNULL(a.ReasonOther, '')                                    AS ReasonOther,
        a.GPA,

        CONVERT(NVARCHAR(10), a.InternshipStartDate, 103)            AS StartDate,
        CONVERT(NVARCHAR(10), a.InternshipEndDate, 103)              AS EndDate,
        a.InternshipStartDate                                        AS StartDateRaw,
        a.InternshipEndDate                                          AS EndDateRaw,
        ja.Remark
    FROM dbo.T_APPLICANTS a

    INNER JOIN dbo.JobApplications ja
        ON ja.ApplicantID = a.ApplicantID
       AND ja.JobID = @JobID

    LEFT JOIN dbo.Provinces p
        ON p.ProvinceID = a.CurrentProvinceID

    LEFT JOIN dbo.Districts d2
        ON d2.DistrictID = a.CurrentDistrictID

    LEFT JOIN dbo.SubDistricts sd
        ON sd.SubDistrictID = a.CurrentSubDistrictID

    WHERE a.ApplicantID = @ApplicantID;
END
GO
