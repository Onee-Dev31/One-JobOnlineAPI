-- Management stored procedures for the existing Universities table (list/create/update/deactivate).
-- sp_GetUniversities (in setup_Universities.sql) stays as-is for the public, active-only dropdown.

CREATE OR ALTER PROCEDURE sp_GetUniversitiesAdmin
    @UniversityID    INT = NULL,
    @IncludeInactive BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    SELECT UniversityID, UniversityNameThai, UniversityNameEng, UniversityType,
           IsActive, CreatedDate, UpdatedDate
    FROM Universities
    WHERE (@UniversityID IS NULL OR UniversityID = @UniversityID)
      AND (@IncludeInactive = 1 OR IsActive = 1)
    ORDER BY UniversityNameThai;
END
GO

CREATE OR ALTER PROCEDURE sp_AddUniversity
    @UniversityNameThai NVARCHAR(200),
    @UniversityNameEng  NVARCHAR(200) = NULL,
    @UniversityType     NVARCHAR(20)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO Universities (UniversityNameThai, UniversityNameEng, UniversityType, IsActive, CreatedDate)
    VALUES (@UniversityNameThai, @UniversityNameEng, @UniversityType, 1, GETDATE());

    SELECT CAST(SCOPE_IDENTITY() AS INT);
END
GO

CREATE OR ALTER PROCEDURE sp_UpdateUniversity
    @UniversityID       INT,
    @UniversityNameThai NVARCHAR(200),
    @UniversityNameEng  NVARCHAR(200) = NULL,
    @UniversityType     NVARCHAR(20),
    @IsActive           BIT = 1
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Universities
    SET UniversityNameThai = @UniversityNameThai,
        UniversityNameEng  = @UniversityNameEng,
        UniversityType     = @UniversityType,
        IsActive           = @IsActive,
        UpdatedDate        = GETDATE()
    WHERE UniversityID = @UniversityID;

    SELECT UniversityID, UniversityNameThai, UniversityNameEng, UniversityType,
           IsActive, CreatedDate, UpdatedDate
    FROM Universities
    WHERE UniversityID = @UniversityID;
END
GO

-- Soft delete: no FK currently references UniversityID (applications store the university name as
-- free text), but this keeps history and allows restoring a row instead of losing it outright.
CREATE OR ALTER PROCEDURE sp_DeleteUniversity
    @UniversityID INT
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE Universities
    SET IsActive    = 0,
        UpdatedDate = GETDATE()
    WHERE UniversityID = @UniversityID;
END
GO
