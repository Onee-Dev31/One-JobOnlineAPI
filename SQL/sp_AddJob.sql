-- First time this proc is tracked in the repo (previously only lived in the DB).
-- Body captured from the live definition via sp_helptext, then updated to replace
-- @Tier/@EmployeeType (free-text) with @LevelID/@EmployeeTypeID (FK to JobLevels/EmployeeTypes).

CREATE OR ALTER PROCEDURE [dbo].[sp_AddJob]

    @JobTitle NVARCHAR(200),
    @JobDescription NVARCHAR(MAX),
    @Requirements NVARCHAR(MAX),
    @Location NVARCHAR(200),
    @ExperienceYears NVARCHAR(100),
    @NumberOfPositions INT,
    @Department NVARCHAR(100),
    @JobStatus NVARCHAR(50),
    @ApprovalStatus NVARCHAR(50),
    @OpenFor NVARCHAR(50),
    @ClosingDate DATETIME,
    @CreatedBy INT,
    @CreatedByRole NVARCHAR(50),

    @JobGroupID INT = NULL,
    @Office NVARCHAR(100) = NULL,
    @LevelID INT = NULL,
    @EmployeeTypeID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.Jobs
    (
        JobTitle,
        JobDescription,
        Requirements,
        Location,
        ExperienceYears,
        NumberOfPositions,
        Department,
        JobStatus,
        ApprovalStatus,
        PostedDate,
        ClosingDate,
        CreatedBy,
        CreatedByRole,
        CreatedDate,
        Remark,
        OpenFor,
        JobGroupID,
        Office,
        LevelID,
        EmployeeTypeID
    )
    VALUES
    (
        @JobTitle,
        @JobDescription,
        @Requirements,
        @Location,
        @ExperienceYears,
        @NumberOfPositions,
        @Department,
        @JobStatus,
        @ApprovalStatus,
        GETDATE(),
        @ClosingDate,
        @CreatedBy,
        @CreatedByRole,
        GETDATE(),
        NULL,
        @OpenFor,
        @JobGroupID,
        @Office,
        @LevelID,
        @EmployeeTypeID
    );

    SELECT CAST(SCOPE_IDENTITY() AS INT) AS JobID;
END
GO
