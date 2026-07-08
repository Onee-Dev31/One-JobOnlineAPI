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
           au.ReportsToAdminID, boss.NameThai AS ReportsToName
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
           au.ReportsToAdminID, boss.NameThai AS ReportsToName
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
    @RoleID     INT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO AdminUsers (Username, HRID, EMAIL, Department, EmpNo, NameThai, Mobile, Position, CompanyName, RoleID)
    VALUES (@Username, @HRID, @EMAIL, @Department, @EmpNo, @NameThai, @Mobile, @Position, @CompanyName, @RoleID);
    SELECT CAST(SCOPE_IDENTITY() AS INT) AS NewAdminID;
END
GO

-- sp_UpdateAdminUser
CREATE OR ALTER PROCEDURE sp_UpdateAdminUser
    @AdminID     INT,
    @EMAIL       NVARCHAR(255) = NULL,
    @Department  NVARCHAR(100) = NULL,
    @NameThai    NVARCHAR(100) = NULL,
    @Mobile      NVARCHAR(20) = NULL,
    @Position    NVARCHAR(100) = NULL,
    @CompanyName NVARCHAR(100) = NULL,
    @RoleID      INT = NULL,
    @IsActive    BIT = NULL
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE AdminUsers SET
        EMAIL        = @EMAIL,
        Department   = @Department,
        NameThai     = @NameThai,
        Mobile       = @Mobile,
        Position     = @Position,
        CompanyName  = @CompanyName,
        RoleID       = @RoleID,
        IsActive     = @IsActive,
        UpdatedDate  = GETDATE()
    WHERE AdminID = @AdminID;
    SELECT @@ROWCOUNT AS AffectedRows;
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
