-- Adds the "Secretary" Role: a self-referencing ReportsToAdminID column on AdminUsers,
-- the Role master-data row itself, and sp_CreateSecretaryAdminUser which — in one transaction —
-- finds-or-auto-creates the boss's AdminUsers account (as Hiring Manager, sourced from HRMS by
-- EmpNo) and then inserts the secretary pointing at that boss.

-- AdminUsers has a filtered unique index (UQ_AdminUsers_EmpNo, WHERE EmpNo IS NOT NULL), so every
-- proc that INSERT/UPDATE/DELETEs it must be compiled with QUOTED_IDENTIFIER ON (this setting is
-- captured at CREATE PROCEDURE time and baked into the proc regardless of the caller's session).
SET QUOTED_IDENTIFIER ON;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('AdminUsers') AND name = 'ReportsToAdminID')
BEGIN
    ALTER TABLE AdminUsers ADD ReportsToAdminID INT NULL;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_AdminUsers_ReportsToAdminID')
BEGIN
    ALTER TABLE AdminUsers ADD CONSTRAINT FK_AdminUsers_ReportsToAdminID FOREIGN KEY (ReportsToAdminID) REFERENCES AdminUsers(AdminID);
END
GO

IF NOT EXISTS (SELECT 1 FROM T_ROLE WHERE ROLE_NAME = N'Secretary')
BEGIN
    DECLARE @NewRoleID INT;
    INSERT INTO T_ROLE (ROLE_NAME, IsActive) VALUES (N'Secretary', 1);
    SET @NewRoleID = CAST(SCOPE_IDENTITY() AS INT);
    UPDATE T_ROLE SET RoleCode = CAST(@NewRoleID AS NVARCHAR(10)) WHERE ID = @NewRoleID;
END
GO

-- @ReportsToEmpNo is the boss's EmpNo (HRMS CODEMPID). If no AdminUsers row exists for that EmpNo
-- yet (regardless of that person's Role), one is auto-created here as Hiring Manager using data
-- pulled from HRMS. If a row already exists, it's reused as-is (its Role is never touched).
CREATE OR ALTER PROCEDURE sp_CreateSecretaryAdminUser
    @Username       NVARCHAR(50),
    @HRID           INT = NULL,
    @EMAIL          NVARCHAR(255) = NULL,
    @Department     NVARCHAR(100) = NULL,
    @EmpNo          NVARCHAR(50) = NULL,
    @NameThai       NVARCHAR(100) = NULL,
    @Mobile         NVARCHAR(20) = NULL,
    @Position       NVARCHAR(100) = NULL,
    @CompanyName    NVARCHAR(100) = NULL,
    @RoleID         INT,
    @ReportsToEmpNo NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @BossAdminID INT, @BossNameThai NVARCHAR(100), @BossWasNewlyCreated BIT = 0;

    SELECT @BossAdminID = AdminID, @BossNameThai = NameThai
    FROM AdminUsers WITH (UPDLOCK, HOLDLOCK)
    WHERE EmpNo = @ReportsToEmpNo;

    IF @BossAdminID IS NULL
    BEGIN
        DECLARE @HiringManagerRoleID INT = (SELECT ID FROM T_ROLE WHERE ROLE_NAME = N'Hiring Manager');

        DECLARE @BossAdUser NVARCHAR(100), @BossEmail NVARCHAR(255), @BossMobile NVARCHAR(50),
                @BossDept NVARCHAR(100), @BossPosition NVARCHAR(200), @BossCompanyName NVARCHAR(200);

        SELECT
            @BossAdUser      = t.AD_USER,
            @BossNameThai    = t.NAMETHAI,
            @BossEmail       = t.EMAIL,
            @BossMobile      = COALESCE(NULLIF(t.USR_MOBILE, ''), NULLIF(t.MOBILE, '-')),
            @BossDept        = t.COSTCENT,
            @BossPosition    = t.POST,
            @BossCompanyName = t.COMPANY_NAME
        FROM [HRMS_LINKED_SERVER].HRMS.dbo.T_EMPLOYEE_SSO t
        WHERE t.CODEMPID = @ReportsToEmpNo;

        IF @BossAdUser IS NULL
        BEGIN
            ROLLBACK TRANSACTION;
            THROW 51000, N'ไม่พบข้อมูลพนักงานตามรหัสพนักงานของหัวหน้าในระบบ HRMS กรุณาตรวจสอบรหัสพนักงาน', 1;
        END

        INSERT INTO AdminUsers (Username, EMAIL, Department, EmpNo, NameThai, Mobile, Position, CompanyName, RoleID)
        VALUES (@BossAdUser, @BossEmail, @BossDept, @ReportsToEmpNo, @BossNameThai, @BossMobile, @BossPosition, @BossCompanyName, @HiringManagerRoleID);

        SET @BossAdminID = CAST(SCOPE_IDENTITY() AS INT);
        SET @BossWasNewlyCreated = 1;
    END

    INSERT INTO AdminUsers (Username, HRID, EMAIL, Department, EmpNo, NameThai, Mobile, Position, CompanyName, RoleID, ReportsToAdminID)
    VALUES (@Username, @HRID, @EMAIL, @Department, @EmpNo, @NameThai, @Mobile, @Position, @CompanyName, @RoleID, @BossAdminID);

    DECLARE @NewAdminID INT = CAST(SCOPE_IDENTITY() AS INT);

    COMMIT TRANSACTION;

    SELECT @NewAdminID AS NewAdminID, @BossWasNewlyCreated AS BossWasNewlyCreated,
           @BossAdminID AS BossAdminID, @BossNameThai AS BossNameThai;
END
GO
