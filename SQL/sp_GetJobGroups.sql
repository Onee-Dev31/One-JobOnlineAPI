SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER   PROCEDURE [dbo].[sp_GetJobGroups]
(
    @JobGroupID INT = NULL
)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        JobGroupID AS jobGroupID,
        GroupName AS groupName,
        SortOrder AS sortOrder
    FROM dbo.JobGroups
    WHERE
        IsActive = 1
        AND (@JobGroupID IS NULL OR JobGroupID = @JobGroupID)
    ORDER BY
        SortOrder ASC,
        GroupName ASC;
END
GO
