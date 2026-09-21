-- ============================================================
-- แก้ GetEmailHRByCompanyCode ให้ filter ตาม Responsibility
-- Responsibility NULL หรือ 'all' → รับทุก JobGroup
-- Responsibility '1,7' → รับเฉพาะ JobGroupID ที่อยู่ใน list
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

    /* =====================================================
       CompanyCode = GTV หรือ ACT → ดึงจาก T_HR_ROLE
    ===================================================== */
    IF @CompanyCode IN ('GTV', 'ACT')
    BEGIN
        SELECT
            e.Email,
            e.nickname
        FROM [EVA_LINKED_SERVER].[Evaluate-Dev-One].[dbo].[T_HR_ROLE] r
        LEFT JOIN [HRMS_LINKED_SERVER].HRMS.dbo.T_EMPLOYEE e
            ON e.CODEMPID = r.EMPLOYEE_NO
        WHERE r.COMPANY_CODE = @CompanyCode
          AND e.Email IS NOT NULL;
    END

    /* =====================================================
       CompanyCode อื่น ๆ → ดึงจาก T_EMAIL_HR_RECIPIENTS_MORE
       กรอง Responsibility:
         - NULL หรือ 'all' → รับทุก JobGroup
         - มีค่า → เช็คว่า JobGroupID อยู่ใน list ไหม
    ===================================================== */
    ELSE
    BEGIN
        SELECT
            m.Email,
            m.Name AS nickname
        FROM dbo.T_EMAIL_HR_RECIPIENTS_MORE m
        WHERE m.IsActive = 1
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
END;
GO
