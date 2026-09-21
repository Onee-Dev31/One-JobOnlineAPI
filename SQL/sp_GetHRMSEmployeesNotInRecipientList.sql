CREATE OR ALTER PROCEDURE [dbo].[sp_GetHRMSEmployeesNotInRecipientList]
    @CompanyCode NVARCHAR(20) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        e.CODEMPID,
        CONCAT(e.NAMFIRSTT, ' ', e.NAMLASTT) AS FullName,
        e.NICKNAME,
        e.EMAIL,
        e.AD_USER,
        e.POST,
        e.NAMECOSTCENT,
        e.COMPANY_CODE
    FROM [HRMS_LINKED_SERVER].HRMS.dbo.T_EMPLOYEE_SSO e
    WHERE (@CompanyCode IS NULL OR e.COMPANY_CODE = @CompanyCode)
      AND e.POST LIKE N'%บุคคล%'
      AND NOT EXISTS (
          SELECT 1
          FROM [dbo].[T_EMAIL_HR_RECIPIENTS_MORE] h
          WHERE h.UserAD = e.AD_USER
      );
END;
GO
