-- เพิ่ม column Responsibility และ ComCode ใน T_EMAIL_HR_RECIPIENTS_MORE
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('T_EMAIL_HR_RECIPIENTS_MORE') AND name = 'Responsibility'
)
    ALTER TABLE [dbo].[T_EMAIL_HR_RECIPIENTS_MORE]
    ADD Responsibility NVARCHAR(200) NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('T_EMAIL_HR_RECIPIENTS_MORE') AND name = 'ComCode'
)
    ALTER TABLE [dbo].[T_EMAIL_HR_RECIPIENTS_MORE]
    ADD ComCode NVARCHAR(50) NULL;

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('T_EMAIL_HR_RECIPIENTS_MORE') AND name = 'UserAD'
)
    ALTER TABLE [dbo].[T_EMAIL_HR_RECIPIENTS_MORE]
    ADD UserAD NVARCHAR(100) NULL;
GO
