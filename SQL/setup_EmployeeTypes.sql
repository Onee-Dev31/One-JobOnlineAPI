SET QUOTED_IDENTIFIER ON;
GO

-- Table: EmployeeTypes
-- Master data สำหรับ "ประเภทพนักงาน" ของตำแหน่งงาน (Jobs.EmployeeTypeID FK มาที่นี่แทนคอลัมน์ string EmployeeType เดิม)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EmployeeTypes')
BEGIN
    CREATE TABLE EmployeeTypes (
        EmployeeTypeID INT IDENTITY(1,1) PRIMARY KEY,
        TypeName       NVARCHAR(100) NOT NULL,
        SortOrder      INT NOT NULL DEFAULT 0,
        IsActive       BIT NOT NULL DEFAULT 1,
        CreatedBy      NVARCHAR(100) NULL,
        CreatedDate    DATETIME NULL DEFAULT GETDATE(),
        UpdatedBy      NVARCHAR(100) NULL,
        UpdatedDate    DATETIME NULL,

        CONSTRAINT UQ_EmployeeTypes_TypeName UNIQUE (TypeName)
    )

    PRINT 'Created EmployeeTypes'
END
GO

-- Seed data (เฉพาะตอนตารางยังว่าง กันการ insert ซ้ำเวลารันไฟล์นี้ซ้ำ)
-- "นักศึกษาฝึกงาน" ต้องอยู่ในนี้ด้วย เพราะหลายจุดในระบบ trainee management เช็คว่า Job เป็นตำแหน่งฝึกงาน
-- หรือไม่ ผ่านค่านี้โดยตรง (ดู setup_JobSlots.sql, setup_TraineeManagementV2.sql เป็นต้น)
IF NOT EXISTS (SELECT 1 FROM EmployeeTypes)
BEGIN
    INSERT INTO EmployeeTypes (TypeName, SortOrder, IsActive, CreatedDate) VALUES
    (N'พนักงานประจำ', 1, 1, GETDATE()),
    (N'พนักงานสัญญาจ้าง', 2, 1, GETDATE()),
    (N'ฟรีแลนซ์', 3, 1, GETDATE()),
    (N'พาร์ทไทม์', 4, 1, GETDATE()),
    (N'นักศึกษาฝึกงาน', 5, 1, GETDATE())
END
GO

-- Public, active-only dropdown proc (ใช้จาก /api/Jobs/employee-types) — คืนแค่ {id, name}
CREATE OR ALTER PROCEDURE sp_GetEmployeeTypes
    @EmployeeTypeID INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        EmployeeTypeID AS id,
        TypeName AS name
    FROM dbo.EmployeeTypes
    WHERE
        IsActive = 1
        AND (@EmployeeTypeID IS NULL OR EmployeeTypeID = @EmployeeTypeID)
    ORDER BY
        SortOrder ASC,
        TypeName ASC;
END
GO
