-- Table: UiSettings
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UiSettings')
BEGIN
    CREATE TABLE UiSettings (
        Id           INT IDENTITY(1,1) PRIMARY KEY,
        SettingKey   NVARCHAR(100) NOT NULL UNIQUE,
        SettingValue NVARCHAR(50)  NOT NULL,
        Description  NVARCHAR(255) NULL,
        UpdatedAt    DATETIME2     NOT NULL DEFAULT GETDATE()
    )
    PRINT 'Created UiSettings'
END

-- Seed default values
MERGE UiSettings AS target
USING (VALUES
    ('font_size_base',      '16', 'Base font size (px)'),
    ('font_size_heading_1', '32', 'Heading 1 font size (px)'),
    ('font_size_heading_2', '24', 'Heading 2 font size (px)'),
    ('font_size_body',      '16', 'Body font size (px)'),
    ('font_size_small',     '14', 'Small text font size (px)')
) AS source (SettingKey, SettingValue, Description)
ON target.SettingKey = source.SettingKey
WHEN NOT MATCHED THEN
    INSERT (SettingKey, SettingValue, Description)
    VALUES (source.SettingKey, source.SettingValue, source.Description);

PRINT 'Seeded UiSettings'

GO
CREATE OR ALTER PROCEDURE sp_GetUiSettings
AS
BEGIN
    SELECT SettingKey, SettingValue FROM UiSettings
END

GO
CREATE OR ALTER PROCEDURE sp_UpsertUiSettings
    @SettingKey   NVARCHAR(100),
    @SettingValue NVARCHAR(50)
AS
BEGIN
    MERGE UiSettings AS target
    USING (SELECT @SettingKey AS SettingKey, @SettingValue AS SettingValue) AS source
    ON target.SettingKey = source.SettingKey
    WHEN MATCHED THEN
        UPDATE SET SettingValue = source.SettingValue, UpdatedAt = GETDATE()
    WHEN NOT MATCHED THEN
        INSERT (SettingKey, SettingValue, UpdatedAt)
        VALUES (source.SettingKey, source.SettingValue, GETDATE());
END
