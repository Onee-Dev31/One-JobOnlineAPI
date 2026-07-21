SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Lets HR/Admin leave free-text comments about a specific trainee's TraineeAssignments row
-- (e.g. mentor/supervisor notes during the internship). Multiple comments accumulate per
-- assignment over time, unlike JobApplications.Remark which is a single field overwritten on
-- every status change.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TraineeComments')
BEGIN
    CREATE TABLE TraineeComments (
        CommentID        INT IDENTITY(1,1) PRIMARY KEY,
        AssignmentID     INT NOT NULL FOREIGN KEY REFERENCES TraineeAssignments(AssignmentID),
        CommentText      NVARCHAR(1000) NOT NULL,
        CreatedByAdminID INT NULL FOREIGN KEY REFERENCES AdminUsers(AdminID),
        CreatedDate      DATETIME2 NOT NULL DEFAULT GETDATE()
    )
    PRINT 'Created TraineeComments'
END
GO

-- @CreatedByUsername resolves to AdminUsers.AdminID here (rather than the caller passing an
-- AdminID directly) because the admin JWT only carries the username (sub claim) -- it has no
-- admin_id claim.
CREATE OR ALTER PROCEDURE sp_AddTraineeComment
    @AssignmentID       INT,
    @CommentText        NVARCHAR(1000),
    @CreatedByUsername  NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CreatedByAdminID INT;
    SELECT @CreatedByAdminID = AdminID FROM AdminUsers WHERE Username = @CreatedByUsername;

    INSERT INTO TraineeComments (AssignmentID, CommentText, CreatedByAdminID)
    VALUES (@AssignmentID, @CommentText, @CreatedByAdminID);

    DECLARE @NewCommentID INT = SCOPE_IDENTITY();

    SELECT
        c.CommentID,
        c.AssignmentID,
        c.CommentText,
        c.CreatedByAdminID,
        a.NameThai AS CreatedByName,
        c.CreatedDate
    FROM TraineeComments c
    LEFT JOIN AdminUsers a ON a.AdminID = c.CreatedByAdminID
    WHERE c.CommentID = @NewCommentID;
END
GO

CREATE OR ALTER PROCEDURE sp_GetTraineeComments
    @AssignmentID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.CommentID,
        c.AssignmentID,
        c.CommentText,
        c.CreatedByAdminID,
        a.NameThai AS CreatedByName,
        c.CreatedDate
    FROM TraineeComments c
    LEFT JOIN AdminUsers a ON a.AdminID = c.CreatedByAdminID
    WHERE c.AssignmentID = @AssignmentID
    ORDER BY c.CreatedDate DESC;
END
GO
