-- ============================================================
-- Dev version: GetEmailHRByCompanyCode + Responsibility filter
-- ลบ hardcode JobGroupID = 7 → ใช้ Responsibility ใน table แทน
-- ============================================================
ALTER PROCEDURE [dbo].[GetEmailHRByCompanyCode]
    @CompanyCode NVARCHAR(10) = NULL,
    @JobID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @JobGroupID INT;

    SELECT @JobGroupID = JobGroupID
    FROM dbo.Jobs
    WHERE JobID = @JobID;

    SELECT
        e.Email,
        e.nickname
    FROM [EVA_LINKED_SERVER].[Evaluate-Dev-One].[dbo].[T_HR_ROLE] r
    LEFT JOIN [HRMS_LINKED_SERVER].HRMS.dbo.T_EMPLOYEE e
        ON e.CODEMPID = r.EMPLOYEE_NO
    WHERE r.COMPANY_CODE = @CompanyCode
      AND e.Email IS NOT NULL

    UNION

    SELECT
        m.Email,
        m.Name AS nickname
    FROM dbo.T_EMAIL_HR_RECIPIENTS_MORE m
    WHERE @CompanyCode NOT IN ('ACT','FLD','GEM','GIN','GSO','GTH','GTV','GSI')
      AND m.IsActive = 1
      AND m.Email IS NOT NULL
      AND (
          m.Responsibility IS NULL
          OR m.Responsibility = 'all'
          OR EXISTS (
              SELECT 1
              FROM STRING_SPLIT(m.Responsibility, ',')
              WHERE LTRIM(RTRIM(value)) = CAST(@JobGroupID AS NVARCHAR(10))
          )
      );
END;
GO
