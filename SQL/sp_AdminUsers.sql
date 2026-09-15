-- AdminUsers has a filtered unique index (UQ_AdminUsers_EmpNo, WHERE EmpNo IS NOT NULL), so every
-- proc that INSERT/UPDATE/DELETEs it must be compiled with QUOTED_IDENTIFIER ON (this setting is
-- captured at CREATE PROCEDURE time and baked into the proc regardless of the caller's session).
SET QUOTED_IDENTIFIER ON;
GO

-- sp_GetAllAdminUsers
CREATE OR ALTER PROCEDURE sp_GetAllAdminUsers
AS
BEGIN
    SET NOCOUNT ON;
    SELECT au.AdminID, au.Username, au.HRID, au.EMAIL, au.Department, au.EmpNo, au.NameThai,
           au.Mobile, au.Position, au.CompanyName, au.RoleID, au.IsActive, au.CreatedDate, au.UpdatedDate, au.Role,
           au.ReportsToAdminID, boss.NameThai AS ReportsToName, au.CanViewAllCompanies
    FROM AdminUsers au
    LEFT JOIN AdminUsers boss ON boss.AdminID = au.ReportsToAdminID
    ORDER BY au.AdminID;
END
GO

-- sp_GetAdminUserById
CREATE OR ALTER PROCEDURE sp_GetAdminUserById
    @AdminID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT au.AdminID, au.Username, au.HRID, au.EMAIL, au.Department, au.EmpNo, au.NameThai,
           au.Mobile, au.Position, au.CompanyName, au.RoleID, au.IsActive, au.CreatedDate, au.UpdatedDate, au.Role,
           au.ReportsToAdminID, boss.NameThai AS ReportsToName, au.CanViewAllCompanies
    FROM AdminUsers au
    LEFT JOIN AdminUsers boss ON boss.AdminID = au.ReportsToAdminID
    WHERE au.AdminID = @AdminID;
END
GO

-- sp_CreateAdminUser
CREATE OR ALTER PROCEDURE sp_CreateAdminUser
    @Username   NVARCHAR(50),
    @HRID       INT = NULL,
    @EMAIL      NVARCHAR(255) = NULL,
    @Department NVARCHAR(100) = NULL,
    @EmpNo      NVARCHAR(50) = NULL,
    @NameThai   NVARCHAR(100) = NULL,
    @Mobile     NVARCHAR(20) = NULL,
    @Position   NVARCHAR(100) = NULL,
    @CompanyName NVARCHAR(100) = NULL,
    @RoleID     INT = NULL,
    @CanViewAllCompanies BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO AdminUsers (Username, HRID, EMAIL, Department, EmpNo, NameThai, Mobile, Position, CompanyName, RoleID, CanViewAllCompanies)
    VALUES (@Username, @HRID, @EMAIL, @Department, @EmpNo, @NameThai, @Mobile, @Position, @CompanyName, @RoleID, COALESCE(@CanViewAllCompanies, 0));
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewAdminID;
END
GO

-- sp_UpdateAdminUser
-- @ReportsToEmpNo: boss's EmpNo (HRMS CODEMPID). NULL (field omitted) leaves the existing
-- ReportsToAdminID relationship untouched; a non-NULL value resolves/auto-creates the boss's
-- AdminUsers row via sp_ResolveBossAdminUser (see setup_SecretaryRole.sql) and repoints ReportsToAdminID at it.
CREATE OR ALTER PROCEDURE sp_UpdateAdminUser
    @AdminID        INT,
    @EMAIL          NVARCHAR(255) = NULL,
    @Department     NVARCHAR(100) = NULL,
    @NameThai       NVARCHAR(100) = NULL,
    @Mobile         NVARCHAR(20) = NULL,
    @Position       NVARCHAR(100) = NULL,
    @CompanyName    NVARCHAR(100) = NULL,
    @RoleID         INT = NULL,
    @IsActive       BIT = NULL,
    @ReportsToEmpNo NVARCHAR(50) = NULL,
    @CanViewAllCompanies BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @BossAdminID INT;

    IF @ReportsToEmpNo IS NOT NULL
    BEGIN
        DECLARE @BossNameThai NVARCHAR(100), @BossWasNewlyCreated BIT;
        EXEC sp_ResolveBossAdminUser
            @ReportsToEmpNo = @ReportsToEmpNo,
            @BossAdminID = @BossAdminID OUTPUT,
            @BossNameThai = @BossNameThai OUTPUT,
            @BossWasNewlyCreated = @BossWasNewlyCreated OUTPUT;
    END

    UPDATE AdminUsers SET
        EMAIL            = @EMAIL,
        Department       = @Department,
        NameThai         = @NameThai,
        Mobile           = @Mobile,
        Position         = @Position,
        CompanyName      = @CompanyName,
        RoleID           = @RoleID,
        IsActive         = @IsActive,
        ReportsToAdminID = CASE WHEN @ReportsToEmpNo IS NOT NULL THEN @BossAdminID ELSE ReportsToAdminID END,
        CanViewAllCompanies = COALESCE(@CanViewAllCompanies, CanViewAllCompanies),
        UpdatedDate      = GETDATE()
    WHERE AdminID = @AdminID;

    DECLARE @Rows INT = @@ROWCOUNT;

    COMMIT TRANSACTION;

    SELECT @Rows AS AffectedRows;
END
GO

-- sp_DeleteAdminUser
CREATE OR ALTER PROCEDURE sp_DeleteAdminUser
    @AdminID INT
AS
BEGIN
    SET NOCOUNT ON;
    DELETE FROM AdminUsers WHERE AdminID = @AdminID;
    SELECT @@ROWCOUNT AS AffectedRows;
END
GO

-- sp_SetAdminUserActive
CREATE OR ALTER PROCEDURE sp_SetAdminUserActive
    @AdminID  INT,
    @IsActive BIT
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE AdminUsers SET IsActive = @IsActive, UpdatedDate = GETDATE()
    WHERE AdminID = @AdminID;
    SELECT @@ROWCOUNT AS AffectedRows;
END
GO

-- sp_GetDependentSecretaryNames: names of secretaries reporting to @AdminID, for the
-- "cannot delete this admin" conflict message shown when a delete hits FK_AdminUsers_ReportsToAdminID.
CREATE OR ALTER PROCEDURE sp_GetDependentSecretaryNames
    @AdminID INT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT COALESCE(NameThai, Username) AS DisplayName
    FROM AdminUsers
    WHERE ReportsToAdminID = @AdminID;
END
GO

-- sp_GetAdminUsersWithRoleV2: used by LoginAdminNewController (POST /api/LoginAdminNew/LoginAD)
-- to fetch the admin's role + HRMS profile after AD auth succeeds. Joins on RoleID (not the legacy
-- Role column) because sp_CreateAdminUser/sp_UpdateAdminUser only ever write RoleID, so Role can be
-- NULL or stale and was causing valid users to fail login or get the wrong role's permissions.
CREATE OR ALTER PROCEDURE sp_GetAdminUsersWithRoleV2
    @Username NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        a.AdminID,
        a.Username,
        a.Department,
        a.Role,
        ISNULL(b.ROLE_NAME, '') AS ROLE_NAME,
        ISNULL(e.CODEMPID, '') AS Empno,
        ISNULL(LTRIM(RTRIM(CONCAT(e.NAMFIRSTT, ' ', e.NAMLASTT))), '') AS NAMETHAI,
        ISNULL(e.EMAIL, '') AS EMAIL,
        ISNULL(e.MOBILE, '') AS MOBILE,
        ISNULL(e.POST, '') AS POST,
        ISNULL(e.COMPANY_NAME, '') AS COMPANY_NAME,
        ISNULL(e.COMPANY_CODE, '') AS COMPANY_CODE,
        ISNULL(e.TELOFF, '') AS TELOFF,
        ISNULL(e.NAMECOSTCENT, '') AS NAMECOSTCENT,
        ISNULL(e.JOBGRADE, '') AS JOBGRADE,
        ISNULL(a.CanViewAllCompanies, 0) AS CanViewAllCompanies
    FROM
        AdminUsers a
    INNER JOIN
        T_ROLE b ON a.RoleID = b.ID
    INNER JOIN
        [HRMS_LINKED_SERVER].HRMS.Dbo.T_EMPLOYEE_SSO e ON TRIM(LOWER(a.Username)) = TRIM(LOWER(e.AD_USER))
    WHERE
        (@Username IS NULL OR TRIM(LOWER(a.Username)) = TRIM(LOWER(@Username)));
END
GO
