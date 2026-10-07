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
    @OpenFor NVARCHAR(200) = NULL,
    @Remark NVARCHAR(200) = NULL,
    @PostedDate DATETIME,
    @ClosingDate DATETIME = NULL,
    @ModifiedBy INT = NULL,
    @ModifiedDate DATETIME = NULL,
    @JobGroupID INT = NULL,
    @Office NVARCHAR(100) = NULL,
    @LevelID INT = NULL,
    @EmployeeTypeID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- เก็บ JobTitle เดิมก่อน update
    DECLARE @OldJobTitle NVARCHAR(200);
    SELECT @OldJobTitle = JobTitle FROM dbo.Jobs WHERE JobID = @JobID;

    UPDATE dbo.Jobs
    SET
        JobTitle          = @JobTitle,
        JobDescription    = @JobDescription,
        Requirements      = @Requirements,
        Location          = @Location,
        ExperienceYears   = @ExperienceYears,
        NumberOfPositions = @NumberOfPositions,
        Department        = @Department,
        JobStatus         = @JobStatus,
        PostedDate        = @PostedDate,
        ClosingDate       = @ClosingDate,
        ModifiedBy        = @ModifiedBy,
        ModifiedDate      = @ModifiedDate,
        Remark            = @Remark,
        OpenFor           = @OpenFor,
        JobGroupID        = @JobGroupID,
        Office            = @Office,
        LevelID           = @LevelID,
        EmployeeTypeID    = @EmployeeTypeID
    WHERE JobID = @JobID;

    -- ถ้า JobTitle เปลี่ยน ให้ sync PreferredPosition และ PreferredPositionBackup
    IF @OldJobTitle <> @JobTitle
    BEGIN
        UPDATE a
        SET
            PreferredPosition       = CASE WHEN a.PreferredPosition       = @OldJobTitle THEN @JobTitle ELSE a.PreferredPosition       END,
            PreferredPositionBackup = CASE WHEN a.PreferredPositionBackup = @OldJobTitle THEN @JobTitle ELSE a.PreferredPositionBackup END
        FROM dbo.T_APPLICANTS a
        INNER JOIN dbo.JobApplications ja ON ja.ApplicantID = a.ApplicantID
        WHERE ja.JobID = @JobID
          AND (a.PreferredPosition = @OldJobTitle OR a.PreferredPositionBackup = @OldJobTitle);
    END
END
GO
