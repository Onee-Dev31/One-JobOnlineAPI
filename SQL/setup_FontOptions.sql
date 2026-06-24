-- เพิ่ม font family keys ใน UiSettings
MERGE UiSettings AS target
USING (VALUES
    ('font_family_base',    'Sarabun',  'Primary font family'),
    ('font_family_heading', 'Sarabun',  'Heading font family')
) AS source (SettingKey, SettingValue, Description)
ON target.SettingKey = source.SettingKey
WHEN NOT MATCHED THEN
    INSERT (SettingKey, SettingValue, Description)
    VALUES (source.SettingKey, source.SettingValue, source.Description);

PRINT 'Seeded font_family keys in UiSettings'

-- Table: เก็บ font options ที่ให้เลือกได้
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'T_FONT_OPTIONS')
BEGIN
    CREATE TABLE T_FONT_OPTIONS (
        Id         INT IDENTITY(1,1) PRIMARY KEY,
        FontName   NVARCHAR(100) NOT NULL UNIQUE,
        FontLabel  NVARCHAR(100) NOT NULL,
        GoogleFont NVARCHAR(200) NULL,
        IsActive   BIT NOT NULL DEFAULT 1
    )
    PRINT 'Created T_FONT_OPTIONS'
END

-- Seed font options (รองรับภาษาไทย)
MERGE T_FONT_OPTIONS AS target
USING (VALUES
    ('Sarabun',      'Sarabun',       'https://fonts.googleapis.com/css2?family=Sarabun:wght@400;600;700&display=swap'),
    ('Noto Sans Thai','Noto Sans Thai','https://fonts.googleapis.com/css2?family=Noto+Sans+Thai:wght@400;600;700&display=swap'),
    ('Prompt',       'Prompt',        'https://fonts.googleapis.com/css2?family=Prompt:wght@400;600;700&display=swap'),
    ('Kanit',        'Kanit',         'https://fonts.googleapis.com/css2?family=Kanit:wght@400;600;700&display=swap'),
    ('IBM Plex Sans Thai','IBM Plex Sans Thai','https://fonts.googleapis.com/css2?family=IBM+Plex+Sans+Thai:wght@400;600;700&display=swap')
) AS source (FontName, FontLabel, GoogleFont)
ON target.FontName = source.FontName
WHEN NOT MATCHED THEN
    INSERT (FontName, FontLabel, GoogleFont)
    VALUES (source.FontName, source.FontLabel, source.GoogleFont);

PRINT 'Seeded T_FONT_OPTIONS'

GO
CREATE OR ALTER PROCEDURE sp_GetFontOptions
AS
BEGIN
    SELECT FontName, FontLabel, GoogleFont
    FROM T_FONT_OPTIONS
    WHERE IsActive = 1
    ORDER BY FontName
END
