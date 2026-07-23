-- Management stored procedures for the existing JobGroups table (list/create/update/deactivate).
-- sp_GetJobGroups (in sp_GetJobGroups.sql) stays as-is for the public, active-only dropdown.

CREATE OR ALTER PROCEDURE sp_GetJobGroupsAdmin
    @JobGroupID     INT = NULL,
    @IncludeInactive BIT = 0
AS
BEGIN
    SET NOCOUNT ON;

    SELECT JobGroupID, GroupName, SortOrder, IsActive,
           CreatedBy, CreatedDate, UpdatedBy, UpdatedDate
    FROM dbo.JobGroups
    WHERE (@JobGroupID IS NULL OR JobGroupID = @JobGroupID)
      AND (@IncludeInactive = 1 OR IsActive = 1)
    ORDER BY SortOrder ASC, GroupName ASC
END
GO

-- @SortOrder is the desired 1-based insertion position among active groups (not a raw stored value).
-- Active groups' SortOrder is kept as a gap-free 1..N sequence at all times; inactive groups keep
-- whatever SortOrder they last had (left untouched, since they aren't shown in the dropdown anyway).
CREATE OR ALTER PROCEDURE sp_AddJobGroup
    @GroupName  NVARCHAR(100),
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
            SELECT COUNT(*) FROM dbo.JobGroups WITH (UPDLOCK, HOLDLOCK) WHERE IsActive = 1
        );

        IF @TargetPosition < 1 SET @TargetPosition = 1;
        IF @TargetPosition > @ActiveCount + 1 SET @TargetPosition = @ActiveCount + 1;

        UPDATE dbo.JobGroups
        SET SortOrder = SortOrder + 1
        WHERE IsActive = 1 AND SortOrder >= @TargetPosition;
    END

    INSERT INTO dbo.JobGroups (GroupName, SortOrder, IsActive, CreatedBy, CreatedDate)
    VALUES (@GroupName, @TargetPosition, @IsActive, @CreatedBy, GETDATE())

    DECLARE @NewID INT = CAST(SCOPE_IDENTITY() AS INT);

    COMMIT TRANSACTION;

    SELECT @NewID
END
GO

-- Covers both a plain edit and a "restore" (IsActive: false -> true). Whenever the row ends up
-- active, @SortOrder is treated as the desired 1-based position and the active list is
-- resequenced to stay a gap-free 1..N. Deactivating here closes the gap it leaves behind.
CREATE OR ALTER PROCEDURE sp_UpdateJobGroup
    @JobGroupID INT,
    @GroupName  NVARCHAR(100),
    @SortOrder  INT = 0,
    @IsActive   BIT = 1,
    @UpdatedBy  NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @OldIsActive BIT, @OldSortOrder INT;
    SELECT @OldIsActive = IsActive, @OldSortOrder = SortOrder
    FROM dbo.JobGroups WITH (UPDLOCK, HOLDLOCK)
    WHERE JobGroupID = @JobGroupID;

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
            UPDATE dbo.JobGroups
            SET SortOrder = SortOrder - 1
            WHERE IsActive = 1 AND SortOrder > @OldSortOrder AND JobGroupID <> @JobGroupID;
        END

        DECLARE @ActiveCount INT = (
            SELECT COUNT(*) FROM dbo.JobGroups WHERE IsActive = 1 AND JobGroupID <> @JobGroupID
        );
        IF @TargetPosition < 1 SET @TargetPosition = 1;
        IF @TargetPosition > @ActiveCount + 1 SET @TargetPosition = @ActiveCount + 1;

        -- Open a gap at the target position (works for both "move" and "reactivate").
        UPDATE dbo.JobGroups
        SET SortOrder = SortOrder + 1
        WHERE IsActive = 1 AND SortOrder >= @TargetPosition AND JobGroupID <> @JobGroupID;
    END
    ELSE IF @OldIsActive = 1
    BEGIN
        -- Deactivating: close the gap left behind in the active list.
        UPDATE dbo.JobGroups
        SET SortOrder = SortOrder - 1
        WHERE IsActive = 1 AND SortOrder > @OldSortOrder AND JobGroupID <> @JobGroupID;
    END

    UPDATE dbo.JobGroups
    SET GroupName   = @GroupName,
        SortOrder   = @TargetPosition,
        IsActive    = @IsActive,
        UpdatedBy   = @UpdatedBy,
        UpdatedDate = GETDATE()
    WHERE JobGroupID = @JobGroupID

    COMMIT TRANSACTION;

    SELECT JobGroupID, GroupName, SortOrder, IsActive,
           CreatedBy, CreatedDate, UpdatedBy, UpdatedDate
    FROM dbo.JobGroups
    WHERE JobGroupID = @JobGroupID
END
GO

-- Soft delete: Jobs.JobGroupID may reference this row, so we deactivate instead of removing it.
-- Closes the gap it leaves behind in the active 1..N sequence; the row's own SortOrder is left
-- as-is since inactive rows aren't part of that sequence.
CREATE OR ALTER PROCEDURE sp_DeleteJobGroup
    @JobGroupID INT,
    @UpdatedBy  NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @OldSortOrder INT, @WasActive BIT;
    SELECT @OldSortOrder = SortOrder, @WasActive = IsActive
    FROM dbo.JobGroups WITH (UPDLOCK, HOLDLOCK)
    WHERE JobGroupID = @JobGroupID;

    UPDATE dbo.JobGroups
    SET IsActive    = 0,
        UpdatedBy   = @UpdatedBy,
        UpdatedDate = GETDATE()
    WHERE JobGroupID = @JobGroupID

    IF @WasActive = 1
    BEGIN
        UPDATE dbo.JobGroups
        SET SortOrder = SortOrder - 1
        WHERE IsActive = 1 AND SortOrder > @OldSortOrder;
    END

    COMMIT TRANSACTION;
END
GO
