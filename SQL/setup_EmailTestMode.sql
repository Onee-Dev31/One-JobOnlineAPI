-- T_EMAIL_CONFIG: เก็บ config ว่าอยู่ใน test mode หรือเปล่า
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'T_EMAIL_CONFIG')
BEGIN
    CREATE TABLE T_EMAIL_CONFIG (
        ID          INT PRIMARY KEY DEFAULT 1,
        IsTestMode  BIT NOT NULL DEFAULT 0,
        UpdatedBy   NVARCHAR(100),
        UpdatedAt   DATETIME DEFAULT GETDATE(),
        CONSTRAINT CHK_SINGLE_ROW CHECK (ID = 1)
    )
    INSERT INTO T_EMAIL_CONFIG (ID, IsTestMode) VALUES (1, 0)
    PRINT 'Created T_EMAIL_CONFIG'
END

-- T_EMAIL_TEST_RECIPIENTS: รายชื่อทีม test ที่รับเมล์แทน
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'T_EMAIL_TEST_RECIPIENTS')
BEGIN
    CREATE TABLE T_EMAIL_TEST_RECIPIENTS (
        ID          INT IDENTITY PRIMARY KEY,
        Email       NVARCHAR(200) NOT NULL,
        Name        NVARCHAR(200),
        IsActive    BIT NOT NULL DEFAULT 1
    )
    PRINT 'Created T_EMAIL_TEST_RECIPIENTS'
END

-- Insert test recipients
IF NOT EXISTS (SELECT 1 FROM T_EMAIL_TEST_RECIPIENTS WHERE Email = 'sengchip1@gmail.com')
    INSERT INTO T_EMAIL_TEST_RECIPIENTS (Email, Name) VALUES ('sengchip1@gmail.com', 'Test 1')

IF NOT EXISTS (SELECT 1 FROM T_EMAIL_TEST_RECIPIENTS WHERE Email = 'thanagorn26@gmail.com')
    INSERT INTO T_EMAIL_TEST_RECIPIENTS (Email, Name) VALUES ('thanagorn26@gmail.com', 'Test 2')

IF NOT EXISTS (SELECT 1 FROM T_EMAIL_TEST_RECIPIENTS WHERE Email = 'jpspure@gmail.com')
    INSERT INTO T_EMAIL_TEST_RECIPIENTS (Email, Name) VALUES ('jpspure@gmail.com', 'Test 3')

IF NOT EXISTS (SELECT 1 FROM T_EMAIL_TEST_RECIPIENTS WHERE Email = 'pnugnolike@hotmail.com')
    INSERT INTO T_EMAIL_TEST_RECIPIENTS (Email, Name) VALUES ('pnugnolike@hotmail.com', 'Test 4')

PRINT 'Inserted test recipients'

GO
-- SP: ดึง email config + test recipients
CREATE OR ALTER PROCEDURE sp_GetEmailConfig
AS
BEGIN
    SELECT IsTestMode FROM T_EMAIL_CONFIG WHERE ID = 1
    SELECT Email FROM T_EMAIL_TEST_RECIPIENTS WHERE IsActive = 1
END

GO
-- SP: toggle test mode
CREATE OR ALTER PROCEDURE sp_SetEmailTestMode
    @IsTestMode BIT,
    @UpdatedBy  NVARCHAR(100)
AS
BEGIN
    UPDATE T_EMAIL_CONFIG
    SET IsTestMode = @IsTestMode,
        UpdatedBy  = @UpdatedBy,
        UpdatedAt  = GETDATE()
    WHERE ID = 1
END
