SET QUOTED_IDENTIFIER ON;
GO

-- Table: JobLevels
-- Master data สำหรับ "ระดับ" ของตำแหน่งงาน (Jobs.LevelID FK มาที่นี่แทนคอลัมน์ string Tier เดิม)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'JobLevels')
BEGIN
    CREATE TABLE JobLevels (
        JobLevelID  INT IDENTITY(1,1) PRIMARY KEY,
        LevelName   NVARCHAR(100) NOT NULL,
        SortOrder   INT NOT NULL DEFAULT 0,
        IsActive    BIT NOT NULL DEFAULT 1,
        CreatedBy   NVARCHAR(100) NULL,
        CreatedDate DATETIME NULL DEFAULT GETDATE(),
        UpdatedBy   NVARCHAR(100) NULL,
        UpdatedDate DATETIME NULL,

        CONSTRAINT UQ_JobLevels_LevelName UNIQUE (LevelName)
    )

    PRINT 'Created JobLevels'
END
GO

-- Seed data (เฉพาะตอนตารางยังว่าง กันการ insert ซ้ำเวลารันไฟล์นี้ซ้ำ)
IF NOT EXISTS (SELECT 1 FROM JobLevels)
BEGIN
    INSERT INTO JobLevels (LevelName, SortOrder, IsActive, CreatedDate) VALUES
    (N'Junior', 1, 1, GETDATE()),
    (N'Mid Level', 2, 1, GETDATE()),
    (N'Senior', 3, 1, GETDATE()),
    (N'Manager', 4, 1, GETDATE()),
    (N'Director', 5, 1, GETDATE()),
    (N'Executive Director', 6, 1, GETDATE()),
    (N'VP', 7, 1, GETDATE()),
    (N'SVP', 8, 1, GETDATE()),
    (N'EVP', 9, 1, GETDATE()),
    (N'C-Level', 10, 1, GETDATE())
END
GO

-- Public, active-only dropdown proc (ใช้จาก /api/Jobs/job-levels) — คืนแค่ {id, name}
CREATE OR ALTER PROCEDURE sp_GetJobLevels
    @JobLevelID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        JobLevelID AS id,
        LevelName AS name
    FROM dbo.JobLevels
    WHERE
        IsActive = 1
        AND (@JobLevelID IS NULL OR JobLevelID = @JobLevelID)
    ORDER BY
        SortOrder ASC,
        LevelName ASC;
END
GO
