-- Management stored procedures for the EmployeeTypes table (list/create/update/deactivate).
-- sp_GetEmployeeTypes (in setup_EmployeeTypes.sql) stays as-is for the public, active-only dropdown.

CREATE OR ALTER PROCEDURE sp_GetEmployeeTypesAdmin
    @EmployeeTypeID  INT = NULL,
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT EmployeeTypeID, TypeName, SortOrder, IsActive,
           CreatedBy, CreatedDate, UpdatedBy, UpdatedDate
    FROM dbo.EmployeeTypes
    WHERE (@EmployeeTypeID IS NULL OR EmployeeTypeID = @EmployeeTypeID)
      AND (@IncludeInactive = 1 OR IsActive = 1)
    ORDER BY SortOrder ASC, TypeName ASC
END
GO

-- @SortOrder is the desired 1-based insertion position among active types (not a raw stored value).
-- Active types' SortOrder is kept as a gap-free 1..N sequence at all times; inactive types keep
-- whatever SortOrder they last had (left untouched, since they aren't shown in the dropdown anyway).
CREATE OR ALTER PROCEDURE sp_AddEmployeeType
    @TypeName   NVARCHAR(100),
    @SortOrder  INT = 0,
    @IsActive   BIT = 1,
    @CreatedBy  NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @TargetPosition INT = @SortOrder;

    IF @IsActive = 1
    BEGIN
        DECLARE @ActiveCount INT = (
            SELECT COUNT(*) FROM dbo.EmployeeTypes WITH (UPDLOCK, HOLDLOCK) WHERE IsActive = 1
        );

        IF @TargetPosition < 1 SET @TargetPosition = 1;
        IF @TargetPosition > @ActiveCount + 1 SET @TargetPosition = @ActiveCount + 1;

        UPDATE dbo.EmployeeTypes
        SET SortOrder = SortOrder + 1
        WHERE IsActive = 1 AND SortOrder >= @TargetPosition;
    END

    INSERT INTO dbo.EmployeeTypes (TypeName, SortOrder, IsActive, CreatedBy, CreatedDate)
    VALUES (@TypeName, @TargetPosition, @IsActive, @CreatedBy, GETDATE())

    DECLARE @NewID INT = CAST(SCOPE_IDENTITY() AS INT);

    COMMIT TRANSACTION;

    SELECT @NewID
END
GO

-- Covers both a plain edit and a "restore" (IsActive: false -> true). Whenever the row ends up
-- active, @SortOrder is treated as the desired 1-based position and the active list is
-- resequenced to stay a gap-free 1..N. Deactivating here closes the gap it leaves behind.
CREATE OR ALTER PROCEDURE sp_UpdateEmployeeType
    @EmployeeTypeID INT,
    @TypeName       NVARCHAR(100),
    @SortOrder      INT = 0,
    @IsActive       BIT = 1,
    @UpdatedBy      NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @OldIsActive BIT, @OldSortOrder INT;
    SELECT @OldIsActive = IsActive, @OldSortOrder = SortOrder
    FROM dbo.EmployeeTypes WITH (UPDLOCK, HOLDLOCK)
    WHERE EmployeeTypeID = @EmployeeTypeID;

    IF @OldIsActive IS NULL
    BEGIN
        ROLLBACK TRANSACTION;
        RETURN;
    END

    DECLARE @TargetPosition INT = @SortOrder;

    IF @IsActive = 1
    BEGIN
        IF @OldIsActive = 1
        BEGIN
            -- Moving within the active list: close the gap left at the old position first.
            UPDATE dbo.EmployeeTypes
            SET SortOrder = SortOrder - 1
            WHERE IsActive = 1 AND SortOrder > @OldSortOrder AND EmployeeTypeID <> @EmployeeTypeID;
        END

        DECLARE @ActiveCount INT = (
            SELECT COUNT(*) FROM dbo.EmployeeTypes WHERE IsActive = 1 AND EmployeeTypeID <> @EmployeeTypeID
        );
        IF @TargetPosition < 1 SET @TargetPosition = 1;
        IF @TargetPosition > @ActiveCount + 1 SET @TargetPosition = @ActiveCount + 1;

        -- Open a gap at the target position (works for both "move" and "reactivate").
        UPDATE dbo.EmployeeTypes
        SET SortOrder = SortOrder + 1
        WHERE IsActive = 1 AND SortOrder >= @TargetPosition AND EmployeeTypeID <> @EmployeeTypeID;
    END
    ELSE IF @OldIsActive = 1
    BEGIN
        -- Deactivating: close the gap left behind in the active list.
        UPDATE dbo.EmployeeTypes
        SET SortOrder = SortOrder - 1
        WHERE IsActive = 1 AND SortOrder > @OldSortOrder AND EmployeeTypeID <> @EmployeeTypeID;
    END

    UPDATE dbo.EmployeeTypes
    SET TypeName    = @TypeName,
        SortOrder   = @TargetPosition,
        IsActive    = @IsActive,
        UpdatedBy   = @UpdatedBy,
        UpdatedDate = GETDATE()
    WHERE EmployeeTypeID = @EmployeeTypeID

    COMMIT TRANSACTION;

    SELECT EmployeeTypeID, TypeName, SortOrder, IsActive,
           CreatedBy, CreatedDate, UpdatedBy, UpdatedDate
    FROM dbo.EmployeeTypes
    WHERE EmployeeTypeID = @EmployeeTypeID
END
GO

-- Soft delete: Jobs.EmployeeTypeID may reference this row, so we deactivate instead of removing it.
-- Closes the gap it leaves behind in the active 1..N sequence; the row's own SortOrder is left
-- as-is since inactive rows aren't part of that sequence.
CREATE OR ALTER PROCEDURE sp_DeleteEmployeeType
    @EmployeeTypeID INT,
    @UpdatedBy      NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @OldSortOrder INT, @WasActive BIT;
    SELECT @OldSortOrder = SortOrder, @WasActive = IsActive
    FROM dbo.EmployeeTypes WITH (UPDLOCK, HOLDLOCK)
    WHERE EmployeeTypeID = @EmployeeTypeID;

    UPDATE dbo.EmployeeTypes
    SET IsActive    = 0,
        UpdatedBy   = @UpdatedBy,
        UpdatedDate = GETDATE()
    WHERE EmployeeTypeID = @EmployeeTypeID

    IF @WasActive = 1
    BEGIN
        UPDATE dbo.EmployeeTypes
        SET SortOrder = SortOrder - 1
        WHERE IsActive = 1 AND SortOrder > @OldSortOrder;
    END

    COMMIT TRANSACTION;
END
GO
