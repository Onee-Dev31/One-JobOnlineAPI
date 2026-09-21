-- ============================================================
-- CRUD: T_EMAIL_HR_RECIPIENTS_MORE
-- @Action: 'INSERT' | 'UPDATE' | 'DELETE'
-- ============================================================
CREATE OR ALTER PROCEDURE [dbo].[sp_ManageEmailHRRecipients]
    @Action         NVARCHAR(10),
    @ID             INT            = NULL,
    @Email          NVARCHAR(200)  = NULL,
    @Name           NVARCHAR(200)  = NULL,
    @IsActive       BIT            = NULL,
    @Responsibility NVARCHAR(200)  = NULL,
    @ComCode        NVARCHAR(50)   = NULL,
    @UserAD         NVARCHAR(100)  = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @Action = 'GET'
    BEGIN
        SELECT ID, Email, Name, IsActive, Responsibility, ComCode, UserAD
        FROM [dbo].[T_EMAIL_HR_RECIPIENTS_MORE]
        ORDER BY ID;
    END

    ELSE IF @Action = 'INSERT'
    BEGIN
        IF @Email IS NULL
            THROW 50001, 'Email is required.', 1;

        INSERT INTO [dbo].[T_EMAIL_HR_RECIPIENTS_MORE] (Email, Name, IsActive, Responsibility, ComCode, UserAD)
        VALUES (@Email, @Name, ISNULL(@IsActive, 1), @Responsibility, @ComCode, @UserAD);

        SELECT CAST(SCOPE_IDENTITY() AS INT) AS ID;
    END

    ELSE IF @Action = 'UPDATE'
    BEGIN
        IF @ID IS NULL
            THROW 50002, 'ID is required for UPDATE.', 1;

        UPDATE [dbo].[T_EMAIL_HR_RECIPIENTS_MORE]
        SET
            Email          = COALESCE(@Email, Email),
            Name           = COALESCE(@Name, Name),
            IsActive       = COALESCE(@IsActive, IsActive),
            Responsibility = COALESCE(@Responsibility, Responsibility),
            ComCode        = COALESCE(@ComCode, ComCode),
            UserAD         = COALESCE(@UserAD, UserAD)
        WHERE ID = @ID;

        SELECT @@ROWCOUNT AS AffectedRows;
    END

    ELSE IF @Action = 'DELETE'
    BEGIN
        IF @ID IS NULL
            THROW 50003, 'ID is required for DELETE.', 1;

        DELETE FROM [dbo].[T_EMAIL_HR_RECIPIENTS_MORE]
        WHERE ID = @ID;

        SELECT @@ROWCOUNT AS AffectedRows;
    END

    ELSE
        THROW 50004, 'Invalid @Action. Use INSERT, UPDATE, or DELETE.', 1;
END;
GO
