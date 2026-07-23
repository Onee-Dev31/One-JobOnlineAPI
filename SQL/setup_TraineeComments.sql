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
        CreatedDate      DATETIME2 NOT NULL DEFAULT GETDATE(),
        ModifiedDate     DATETIME2 NULL
    )
    PRINT 'Created TraineeComments'
END
GO

IF COL_LENGTH('TraineeComments', 'ModifiedDate') IS NULL
    ALTER TABLE TraineeComments ADD ModifiedDate DATETIME2 NULL
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
    SET XACT_ABORT ON;

    DECLARE @CreatedByAdminID INT;
    DECLARE @CommentID INT;
	DECLARE @Action NVARCHAR(100);

    -- หา AdminID จาก Username
    SELECT @CreatedByAdminID = AdminID
    FROM dbo.AdminUsers
    WHERE Username = @CreatedByUsername;

    IF @CreatedByAdminID IS NULL
    BEGIN
        THROW 51001, N'ไม่พบข้อมูลผู้ใช้งาน', 1;
    END;

	-- หา Comment เดิมของ Admin คนนี้ ใน Assignment นี้
	SELECT @CommentID = CommentID
	FROM dbo.TraineeComments
	WHERE AssignmentID = @AssignmentID
	  AND CreatedByAdminID = @CreatedByAdminID;

	IF @CommentID IS NOT NULL
	BEGIN
		-- มีแล้ว แก้ข้อความเดิม
		UPDATE dbo.TraineeComments
		SET
			CommentText = @CommentText,
			ModifiedDate = GETDATE()
		WHERE CommentID = @CommentID;

		SET @Action = N'แก้ไขความคิดเห็นเรียบร้อยแล้ว';
	END
	ELSE
	BEGIN
		-- ยังไม่มี เพิ่มใหม่
		INSERT INTO dbo.TraineeComments
		(
			AssignmentID,
			CommentText,
			CreatedByAdminID
		)
		VALUES
		(
			@AssignmentID,
			@CommentText,
			@CreatedByAdminID
		);

		SET @CommentID = SCOPE_IDENTITY();
		SET @Action = N'บันทึกความคิดเห็นเรียบร้อยแล้ว';
	END;
    
    -- คืน Comment ไม่ว่าจะ Insert หรือ Update
    SELECT
        c.CommentID,
        c.AssignmentID,
        c.CommentText,
        c.CreatedByAdminID,
        a.NameThai AS CreatedByName,
        c.CreatedDate,
        c.ModifiedDate,
		@Action AS Action
    FROM dbo.TraineeComments c
    LEFT JOIN dbo.AdminUsers a
        ON a.AdminID = c.CreatedByAdminID
    WHERE c.CommentID = @CommentID;
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
        c.CreatedDate,
        c.ModifiedDate
    FROM TraineeComments c
    LEFT JOIN AdminUsers a ON a.AdminID = c.CreatedByAdminID
    WHERE c.AssignmentID = @AssignmentID
    ORDER BY c.CreatedDate DESC;
END
GO

-- Requires @AssignmentID (not just @CommentID) so a caller can't edit a comment via a mismatched
-- assignment route. Empty result set means no matching row was found (wrong ID or wrong assignment).
CREATE OR ALTER PROCEDURE sp_UpdateTraineeComment
    @CommentID    INT,
    @AssignmentID INT,
    @CommentText  NVARCHAR(1000)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE TraineeComments
    SET CommentText = @CommentText,
        ModifiedDate = GETDATE()
    WHERE CommentID = @CommentID AND AssignmentID = @AssignmentID;

    SELECT
        c.CommentID,
        c.AssignmentID,
        c.CommentText,
        c.CreatedByAdminID,
        a.NameThai AS CreatedByName,
        c.CreatedDate,
        c.ModifiedDate
    FROM TraineeComments c
    LEFT JOIN AdminUsers a ON a.AdminID = c.CreatedByAdminID
    WHERE c.CommentID = @CommentID AND c.AssignmentID = @AssignmentID;
END
GO
