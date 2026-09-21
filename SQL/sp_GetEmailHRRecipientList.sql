CREATE OR ALTER PROCEDURE [dbo].[sp_GetEmailHRRecipientList]
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        h.ID,
        h.IsActive,
        e.CODEMPID,
        CONCAT(e.NAMFIRSTT, ' ', e.NAMLASTT) AS FullName,
        e.NICKNAME,
        e.AD_USER AS UserAD,
        e.POST,
        e.NAMECOSTCENT,
        e.EMAIL,
        e.COMPANY_CODE,
        e.COMPANY_NAME,
        h.Responsibility,

        (
            SELECT STRING_AGG(jg.GroupName, ', ')
                WITHIN GROUP (ORDER BY jg.SortOrder)
            FROM STRING_SPLIT(h.Responsibility, ',') s
            INNER JOIN [JobOnlineDB].[dbo].[JobGroups] jg
                ON jg.JobGroupID = TRY_CAST(LTRIM(RTRIM(s.value)) AS INT)
        ) AS ResponsibilityName

    FROM [dbo].[T_EMAIL_HR_RECIPIENTS_MORE] h
    LEFT JOIN [HRMS_LINKED_SERVER].HRMS.dbo.T_EMPLOYEE_SSO e
        ON h.UserAD = e.AD_USER;
END;
GO
