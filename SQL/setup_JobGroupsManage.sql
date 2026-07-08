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

CREATE OR ALTER PROCEDURE sp_AddJobGroup
    @GroupName  NVARCHAR(100),
    @SortOrder  INT = 0,
    @IsActive   BIT = 1,
    @CreatedBy  NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.JobGroups (GroupName, SortOrder, IsActive, CreatedBy, CreatedDate)
    VALUES (@GroupName, @SortOrder, @IsActive, @CreatedBy, GETDATE())

    SELECT CAST(SCOPE_IDENTITY() AS INT)
END
GO

CREATE OR ALTER PROCEDURE sp_UpdateJobGroup
    @JobGroupID INT,
    @GroupName  NVARCHAR(100),
    @SortOrder  INT = 0,
    @IsActive   BIT = 1,
    @UpdatedBy  NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.JobGroups
    SET GroupName  = @GroupName,
        SortOrder  = @SortOrder,
        IsActive   = @IsActive,
        UpdatedBy  = @UpdatedBy,
        UpdatedDate = GETDATE()
    WHERE JobGroupID = @JobGroupID

    SELECT JobGroupID, GroupName, SortOrder, IsActive,
           CreatedBy, CreatedDate, UpdatedBy, UpdatedDate
    FROM dbo.JobGroups
    WHERE JobGroupID = @JobGroupID
END
GO

-- Soft delete: Jobs.JobGroupID may reference this row, so we deactivate instead of removing it.
CREATE OR ALTER PROCEDURE sp_DeleteJobGroup
    @JobGroupID INT,
    @UpdatedBy  NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.JobGroups
    SET IsActive    = 0,
        UpdatedBy   = @UpdatedBy,
        UpdatedDate = GETDATE()
    WHERE JobGroupID = @JobGroupID
END
GO
