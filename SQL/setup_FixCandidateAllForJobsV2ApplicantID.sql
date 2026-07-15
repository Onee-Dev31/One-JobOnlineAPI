-- Bug: GET /ApplicantNew/GetCandidateForJobs returned ApplicantID = NULL for candidates
-- sourced from the JobSlotAssignments/self-apply branch (trainee flow), even for non-trainee
-- jobs and even after JobApplications.ApplicantID was already populated (see
-- setup_JobSlotAssignmentsApplicantSync.sql). Root cause: this branch's SELECT hardcoded
-- "NULL AS ApplicantID" instead of reading it from JobApplications. Fix: select b.ApplicantID.

CREATE OR ALTER PROCEDURE [dbo].[sp_GetCandidateAllForJobsV2]
    @Department NVARCHAR(100) = NULL,
    @JobID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT DISTINCT
        a.ApplicantID,
        a.Title,
        a.TitleENG,
        a.Gender,
        a.FirstNameThai,
        a.LastNameThai,
        a.FirstNameEng,
        a.LastNameEng,
        a.BirthDate,
        a.Height,
        a.Weight,
        a.MobilePhone,
        a.LINE,
        a.Email,
        a.CreatedDate,
        a.UserId,
        a.CodeMPID,

        b.ApplicationID,
        b.JobID,
        b.Status,
        b.InterviewDate,
        b.Result,
        b.Remark,
        b.JobStartDate,
        b.RankOfSelect,

        c.JobTitle,
        c.Location,
        c.Department,
        c.JobStatus,
        c.PostedDate,
        c.ClosingDate,
        c.OpenFor,

        files.FilesList,

        CASE
            WHEN d.REQ_STATUS = 'New' THEN 'Pending'
            ELSE d.REQ_STATUS
        END AS REQ_STATUS,

        CASE
            WHEN eq.Status = 'Sent' THEN 'Pending'
            ELSE ISNULL(eq.Status, 'N/A')
        END AS EMAIL_STATUS,

        eq.JobID AS JobIDEmail,

        emp.NAMECOSTCENT AS HeadNameCostCenter,
        emp.POST AS HeadPost,
        emp.TITLETHAI AS HeadTitleThai,
        emp.NAMFIRSTT AS HeadFirstNameThai,
        emp.NAMLASTT AS HeadLastNameThai,
        emp.TITLEENG AS HeadTitleEng,
        emp.NAMFIRSTE AS HeadFirstNameEng,
        emp.NAMLASTE AS HeadLastNameEng,

        ISNULL(it.IsClicked, 0) AS IsClicked

    FROM T_APPLICANTS a
    INNER JOIN JobApplications b
        ON a.ApplicantID = b.ApplicantID
    LEFT JOIN Jobs c
        ON b.JobID = c.JobID
    LEFT JOIN T_EMP_IT_REQ d
        ON d.ApplicantID = a.ApplicantID

    OUTER APPLY (
        SELECT TOP 1 *
        FROM EmailSendQueue eq2
        WHERE eq2.RecipientEmail = a.Email
          AND eq2.JobID = b.JobID
        ORDER BY eq2.CreatedDate DESC
    ) eq

    OUTER APPLY (
        SELECT
            (
                SELECT
                    af.FileID,
                    REPLACE(af.FilePath, 'C:/', '') AS FilePath,
                    af.FileName,
                    af.SectionFile
                FROM ApplicantFiles af
                WHERE af.ApplicantID = a.ApplicantID
                  AND af.JobID = b.JobID
                FOR JSON PATH
            ) AS FilesList
    ) files

    LEFT JOIN [HRMS_LINKED_SERVER].HRMS.Dbo.T_EMPLOYEE emp
        ON (
            (c.OpenFor IS NOT NULL AND LTRIM(RTRIM(c.OpenFor)) <> '' AND emp.CODEMPID = c.OpenFor)
            OR
            ((c.OpenFor IS NULL OR LTRIM(RTRIM(c.OpenFor)) = '') AND emp.AD_USER = c.CreatedByRole)
        )

    LEFT JOIN T_NewEmployeeITServiceLog it
        ON it.ApplicantID = a.ApplicantID
       AND it.JobID = b.JobID

    WHERE
        (@Department IS NULL OR c.Department = @Department)
        AND (@JobID IS NULL OR c.JobID = @JobID)

    UNION ALL

    SELECT DISTINCT
        b.ApplicantID AS ApplicantID,
        js.ManualTitle AS Title,
        NULL AS TitleENG,
        NULL AS Gender,
        js.ManualFirstNameThai AS FirstNameThai,
        js.ManualLastNameThai AS LastNameThai,
        NULL AS FirstNameEng,
        NULL AS LastNameEng,
        NULL AS BirthDate,
        NULL AS Height,
        NULL AS Weight,
        js.ManualMobilePhone AS MobilePhone,
        NULL AS LINE,
        js.ManualEmail AS Email,
        js.AssignedDate AS CreatedDate,
        NULL AS UserId,
        NULL AS CodeMPID,

        b.ApplicationID,
        b.JobID,
        CASE
			WHEN b.Status = 'Pending' THEN 'Pending HR Screening'
			ELSE b.Status
		END AS Status,
        b.InterviewDate,
        b.Result,
        b.Remark,
        b.JobStartDate,
        b.RankOfSelect,

        ISNULL(c.JobTitle, js.ManualPreferredPosition) AS JobTitle,
        c.Location,
        c.Department,
        c.JobStatus,
        c.PostedDate,
        c.ClosingDate,
        c.OpenFor,

        NULL AS FilesList,

        'N/A' AS REQ_STATUS,
        'N/A' AS EMAIL_STATUS,
        NULL AS JobIDEmail,

        emp.NAMECOSTCENT AS HeadNameCostCenter,
        emp.POST AS HeadPost,
        emp.TITLETHAI AS HeadTitleThai,
        emp.NAMFIRSTT AS HeadFirstNameThai,
        emp.NAMLASTT AS HeadLastNameThai,
        emp.TITLEENG AS HeadTitleEng,
        emp.NAMFIRSTE AS HeadFirstNameEng,
        emp.NAMLASTE AS HeadLastNameEng,

        0 AS IsClicked

    FROM JobSlotAssignments js
    INNER JOIN JobApplications b
        ON js.ApplicationID = b.ApplicationID
    LEFT JOIN Jobs c
        ON b.JobID = c.JobID

    LEFT JOIN [HRMS_LINKED_SERVER].HRMS.Dbo.T_EMPLOYEE emp
        ON (
            (c.OpenFor IS NOT NULL AND LTRIM(RTRIM(c.OpenFor)) <> '' AND emp.CODEMPID = c.OpenFor)
            OR
            ((c.OpenFor IS NULL OR LTRIM(RTRIM(c.OpenFor)) = '') AND emp.AD_USER = c.CreatedByRole)
        )

    WHERE
        (@Department IS NULL OR c.Department = @Department)
        AND (@JobID IS NULL OR c.JobID = @JobID);
END
GO
