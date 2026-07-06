SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_UpdateJob]
    @JobID INT,
    @JobTitle NVARCHAR(200),
    @JobDescription NVARCHAR(MAX),
    @Requirements NVARCHAR(MAX),
    @Location NVARCHAR(200),
    @ExperienceYears NVARCHAR(100),
    @NumberOfPositions INT,
    @Department NVARCHAR(100),
    @JobStatus NVARCHAR(50),
    --@ApprovalStatus NVARCHAR(50),
    @PostedDate DATETIME,
    @ClosingDate DATETIME = NULL,
    @ModifiedBy INT = NULL,
    @ModifiedDate DATETIME = NULL,
    @JobGroupID INT = NULL,
    @Office NVARCHAR(100) = NULL,
    @Tier NVARCHAR(50) = NULL,
    @EmployeeType NVARCHAR(50) = NULL
AS
BEGIN
    UPDATE Jobs
    SET
        JobTitle = @JobTitle,
        JobDescription = @JobDescription,
        Requirements = @Requirements,
        Location = @Location,
        ExperienceYears = @ExperienceYears,
        NumberOfPositions = @NumberOfPositions,
        Department = @Department,
        JobStatus = @JobStatus,
        --ApprovalStatus = @ApprovalStatus,
        PostedDate = @PostedDate,
        ClosingDate = @ClosingDate,
        ModifiedBy = @ModifiedBy,
        ModifiedDate = @ModifiedDate,
        JobGroupID = @JobGroupID,
        Office = @Office,
        Tier = @Tier,
        EmployeeType = @EmployeeType
    WHERE JobID = @JobID;
END
GO
