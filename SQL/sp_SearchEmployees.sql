-- Search all employees from HRMS (not limited to job-opener permission, unlike getDateOpenFor)
-- Used by the admin-creation page to pick an employee by company/department/free-text search.
CREATE OR ALTER PROCEDURE sp_SearchEmployees
    @CompanyCode NVARCHAR(50)  = NULL,
    @Department  NVARCHAR(200) = NULL,
    @Search      NVARCHAR(200) = NULL,
    @Page        INT = 1,
    @PageSize    INT = 20
AS
BEGIN
    SET NOCOUNT ON;

    IF @Page IS NULL OR @Page < 1
        SET @Page = 1;

    IF @PageSize IS NULL OR @PageSize < 1
        SET @PageSize = 20;

    DECLARE @Offset INT = (@Page - 1) * @PageSize;
    DECLARE @SearchPattern NVARCHAR(210) = '%' + LTRIM(RTRIM(@Search)) + '%';

    SELECT
        t.CODEMPID      AS EmpNo,
        t.AD_USER       AS AdUser,
        t.NAMETHAI      AS NameThai,
        t.EMAIL         AS Email,
        COALESCE(NULLIF(t.USR_MOBILE, ''), NULLIF(t.MOBILE, '-')) AS Mobile,
        t.TELOFF        AS Telephone,
        t.COSTCENT      AS DepartmentCode,
        t.NAMECOSTCENT  AS DepartmentName,
        t.POST          AS Position,
        t.COMPANY_CODE  AS CompanyCode,
        t.COMPANY_NAME  AS CompanyName,
        CASE WHEN au.AdminID IS NOT NULL THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END AS IsAdmin,
        COUNT(*) OVER() AS TotalCount
    FROM [HRMS_LINKED_SERVER].HRMS.dbo.T_EMPLOYEE t
    LEFT JOIN AdminUsers au ON au.EmpNo = t.CODEMPID
    WHERE (@CompanyCode IS NULL OR t.COMPANY_CODE = @CompanyCode)
      AND (@Department IS NULL OR t.COSTCENT = @Department)
      AND (
            @Search IS NULL OR LTRIM(RTRIM(@Search)) = ''
            OR t.CODEMPID LIKE @SearchPattern
            OR LOWER(t.AD_USER) LIKE LOWER(@SearchPattern)
            OR t.NAMETHAI LIKE @SearchPattern
            OR LOWER(t.EMAIL) LIKE LOWER(@SearchPattern)
          )
    ORDER BY t.NAMETHAI
    OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
END
GO
