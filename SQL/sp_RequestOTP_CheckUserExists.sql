ALTER PROCEDURE [dbo].[sp_RequestOTP]
    @Identifier NVARCHAR(200), @OTPCode NVARCHAR(6), @Action NVARCHAR(50)
AS
BEGIN
    SET NOCOUNT ON;

    -- ถ้าเป็น LOGIN ให้ตรวจว่า email มีในระบบก่อน
    IF @Action = 'LOGIN' AND NOT EXISTS (
        SELECT 1 FROM Users WHERE Email = @Identifier
    )
    BEGIN
        SELECT 0 AS Status, N'ไม่พบบัญชีผู้ใช้งานในระบบ' AS Message; RETURN;
    END

    IF EXISTS (
        SELECT 1 FROM OTPRequests
        WHERE Identifier = @Identifier AND Action = @Action
          AND IsUsed = 0 AND ExpiresAt > GETDATE()
    )
    BEGIN
        SELECT 0 AS Status, N'มี OTP ที่ใช้งานอยู่สำหรับ user นี้' AS Message; RETURN;
    END

    INSERT INTO OTPRequests (Identifier, OTPCode, Action, ExpiresAt)
    VALUES (@Identifier, @OTPCode, @Action, DATEADD(MINUTE, 5, GETDATE()));

    SELECT 1 AS Status, N'ส่ง OTP เรียบร้อย' AS Message;
END
GO
