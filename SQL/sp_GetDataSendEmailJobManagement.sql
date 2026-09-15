SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[sp_GetDataSendEmailJobManagement]
    @CreatorRole INT,
    @CreatorDepartment NVARCHAR(50),
    @OpenForEmpID NVARCHAR(10) = NULL,
    @CreatorUsername NVARCHAR(100) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    IF @CreatorRole IS NULL
    BEGIN
        RAISERROR ('ต้องระบุ CreatorRole', 16, 1);
        RETURN;
    END

    IF @CreatorRole = 2
    BEGIN
        SELECT
            e.CODEMPID,
            e.NAMETHAI,
            e.NAMECOSTCENT,
            CASE
                WHEN e.EMAIL IS NOT NULL AND LTRIM(RTRIM(e.EMAIL)) <> '' THEN e.EMAIL
                ELSE a.EMAIL
            END AS EMAIL
        FROM JobOnlineDB.dbo.AdminUsers a
        LEFT JOIN [HRMS_LINKED_SERVER].HRMS.Dbo.T_EMPLOYEE e ON LOWER(a.Username) = LOWER(e.AD_USER)
        WHERE
            (
                a.Department = @CreatorDepartment
                OR (@OpenForEmpID IS NOT NULL AND e.CODEMPID = @OpenForEmpID)
            )
            AND (
                (e.EMAIL IS NOT NULL AND LTRIM(RTRIM(e.EMAIL)) <> '')
                OR (a.EMAIL IS NOT NULL AND LTRIM(RTRIM(a.EMAIL)) <> '')
            )
             AND (@CreatorUsername IS NULL OR LOWER(e.AD_USER) <> LOWER(@CreatorUsername));
    END
    ELSE
    BEGIN
        SELECT
            e.CODEMPID,
            e.NAMETHAI,
            e.NAMECOSTCENT,
            CASE
                WHEN e.EMAIL IS NOT NULL AND LTRIM(RTRIM(e.EMAIL)) <> '' THEN e.EMAIL
                ELSE a.EMAIL
            END AS EMAIL
        FROM JobOnlineDB.dbo.AdminUsers a
        LEFT JOIN [HRMS_LINKED_SERVER].HRMS.Dbo.T_EMPLOYEE e ON LOWER(a.Username) = LOWER(e.AD_USER)
        WHERE
            (
                a.Role = 2
                OR (@OpenForEmpID IS NOT NULL AND e.CODEMPID = @OpenForEmpID)
            )
            AND (
                (e.EMAIL IS NOT NULL AND LTRIM(RTRIM(e.EMAIL)) <> '')
                OR (a.EMAIL IS NOT NULL AND LTRIM(RTRIM(a.EMAIL)) <> '')
            )
             AND (@CreatorUsername IS NULL OR LOWER(e.AD_USER) <> LOWER(@CreatorUsername));
    END
END
GO
