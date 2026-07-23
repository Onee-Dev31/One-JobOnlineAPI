-- Search applicants by Thai/English name, returning every job application (with job info)
-- each matching applicant has. Used by the "manual reject" page: HR searches a name, picks
-- the matching applicant, then sees all their applications to reject the ones that aren't
-- Employment confirm yet.
CREATE OR ALTER PROCEDURE sp_SearchApplicantsByName
    @Name NVARCHAR(200) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @SearchPattern NVARCHAR(210) = '%' + LTRIM(RTRIM(@Name)) + '%';

    SELECT
        a.ApplicantID,
        a.Title,
        a.FirstNameThai,
        a.LastNameThai,
        a.FirstNameEng,
        a.LastNameEng,
        a.Email,
        a.MobilePhone,

        b.ApplicationID,
        b.JobID,
        b.Status,
        b.Remark,
        b.RankOfSelect,
        b.SubmissionDate,

        c.JobTitle,
        c.Department AS DepartmentCode,
        dn.DEPARTMENTNAME AS DepartmentName,
        c.Location
    FROM T_APPLICANTS a
    INNER JOIN JobApplications b ON b.ApplicantID = a.ApplicantID
    INNER JOIN Jobs c ON c.JobID = b.JobID
    OUTER APPLY dbo.fn_GetDepartmentNameFromCoscent(c.Department) dn
    WHERE @Name IS NOT NULL AND LTRIM(RTRIM(@Name)) <> ''
      AND (
            a.FirstNameThai LIKE @SearchPattern
            OR a.LastNameThai LIKE @SearchPattern
            OR CONCAT(a.FirstNameThai, N' ', a.LastNameThai) LIKE @SearchPattern
            OR a.FirstNameEng LIKE @SearchPattern
            OR a.LastNameEng LIKE @SearchPattern
            OR CONCAT(a.FirstNameEng, ' ', a.LastNameEng) LIKE @SearchPattern
          )
    ORDER BY a.FirstNameThai, a.LastNameThai, b.ApplicationID;
END
GO
