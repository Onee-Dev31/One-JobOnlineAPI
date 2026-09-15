SET QUOTED_IDENTIFIER ON;
GO

-- Table: Universities
-- ตารางรายชื่อมหาวิทยาลัยในประเทศไทย (สำหรับใช้เป็น master data / dropdown เช่นในฟอร์มสมัครฝึกงาน)
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Universities')
BEGIN
    CREATE TABLE Universities (
        UniversityID       INT IDENTITY(1,1) PRIMARY KEY,
        UniversityNameThai NVARCHAR(200) NOT NULL,
        UniversityNameEng  NVARCHAR(200) NULL,
        UniversityType     NVARCHAR(20) NOT NULL,  -- public / private
        IsActive           BIT NOT NULL DEFAULT 1,
        CreatedDate        DATETIME NOT NULL DEFAULT GETDATE(),
        UpdatedDate        DATETIME NULL,

        CONSTRAINT UQ_Universities_NameThai UNIQUE (UniversityNameThai),
        CONSTRAINT CHK_Universities_Type CHECK (UniversityType IN ('public', 'private'))
    )

    CREATE INDEX IX_Universities_Type ON Universities(UniversityType)

    PRINT 'Created Universities'
END
GO

-- Seed data (เฉพาะตอนตารางยังว่าง กันการ insert ซ้ำเวลารันไฟล์นี้ซ้ำ)
IF NOT EXISTS (SELECT 1 FROM Universities)
BEGIN
    INSERT INTO Universities (UniversityNameThai, UniversityNameEng, UniversityType) VALUES
    -- มหาวิทยาลัยในกำกับของรัฐ (Autonomous Public Universities)
    (N'จุฬาลงกรณ์มหาวิทยาลัย', N'Chulalongkorn University', 'public'),
    (N'มหาวิทยาลัยเกษตรศาสตร์', N'Kasetsart University', 'public'),
    (N'มหาวิทยาลัยเชียงใหม่', N'Chiang Mai University', 'public'),
    (N'มหาวิทยาลัยขอนแก่น', N'Khon Kaen University', 'public'),
    (N'มหาวิทยาลัยทักษิณ', N'Thaksin University', 'public'),
    (N'มหาวิทยาลัยเทคโนโลยีพระจอมเกล้าธนบุรี', N'King Mongkut''s University of Technology Thonburi', 'public'),
    (N'มหาวิทยาลัยเทคโนโลยีพระจอมเกล้าพระนครเหนือ', N'King Mongkut''s University of Technology North Bangkok', 'public'),
    (N'สถาบันเทคโนโลยีพระจอมเกล้าเจ้าคุณทหารลาดกระบัง', N'King Mongkut''s Institute of Technology Ladkrabang', 'public'),
    (N'มหาวิทยาลัยเทคโนโลยีสุรนารี', N'Suranaree University of Technology', 'public'),
    (N'มหาวิทยาลัยธรรมศาสตร์', N'Thammasat University', 'public'),
    (N'มหาวิทยาลัยนวมินทราธิราช', N'Navamindradhiraj University', 'public'),
    (N'มหาวิทยาลัยบูรพา', N'Burapha University', 'public'),
    (N'มหาวิทยาลัยพะเยา', N'University of Phayao', 'public'),
    (N'มหาวิทยาลัยมหาจุฬาลงกรณราชวิทยาลัย', N'Mahachulalongkornrajavidyalaya University', 'public'),
    (N'มหาวิทยาลัยมหามกุฏราชวิทยาลัย', N'Mahamakut Buddhist University', 'public'),
    (N'มหาวิทยาลัยมหิดล', N'Mahidol University', 'public'),
    (N'มหาวิทยาลัยแม่โจ้', N'Maejo University', 'public'),
    (N'มหาวิทยาลัยแม่ฟ้าหลวง', N'Mae Fah Luang University', 'public'),
    (N'มหาวิทยาลัยวลัยลักษณ์', N'Walailak University', 'public'),
    (N'มหาวิทยาลัยศรีนครินทรวิโรฒ', N'Srinakharinwirot University', 'public'),
    (N'มหาวิทยาลัยศิลปากร', N'Silpakorn University', 'public'),
    (N'มหาวิทยาลัยสงขลานครินทร์', N'Prince of Songkla University', 'public'),
    (N'มหาวิทยาลัยสวนดุสิต', N'Suan Dusit University', 'public'),
    (N'สถาบันบัณฑิตพัฒนบริหารศาสตร์', N'National Institute of Development Administration (NIDA)', 'public'),

    -- มหาวิทยาลัยของรัฐ (Other Public Universities)
    (N'มหาวิทยาลัยกาฬสินธุ์', N'Kalasin University', 'public'),
    (N'มหาวิทยาลัยนครพนม', N'Nakhon Phanom University', 'public'),
    (N'มหาวิทยาลัยนราธิวาสราชนครินทร์', N'Princess of Naradhiwas University', 'public'),
    (N'มหาวิทยาลัยนเรศวร', N'Naresuan University', 'public'),
    (N'มหาวิทยาลัยมหาสารคาม', N'Mahasarakham University', 'public'),
    (N'มหาวิทยาลัยรามคำแหง', N'Ramkhamhaeng University', 'public'),
    (N'มหาวิทยาลัยสุโขทัยธรรมาธิราช', N'Sukhothai Thammathirat Open University', 'public'),
    (N'มหาวิทยาลัยอุบลราชธานี', N'Ubon Ratchathani University', 'public'),

    -- มหาวิทยาลัยราชภัฏ (Rajabhat Universities)
    (N'มหาวิทยาลัยราชภัฏกาญจนบุรี', N'Kanchanaburi Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏกำแพงเพชร', N'Kamphaeng Phet Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏจันทรเกษม', N'Chandrakasem Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏชัยภูมิ', N'Chaiyaphum Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏเชียงราย', N'Chiang Rai Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏเชียงใหม่', N'Chiang Mai Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏเทพสตรี', N'Thepsatri Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏธนบุรี', N'Dhonburi Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏนครปฐม', N'Nakhon Pathom Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏนครราชสีมา', N'Nakhon Ratchasima Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏนครศรีธรรมราช', N'Nakhon Si Thammarat Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏนครสวรรค์', N'Nakhon Sawan Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏบ้านสมเด็จเจ้าพระยา', N'Bansomdejchaopraya Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏบุรีรัมย์', N'Buri Ram Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏพระนคร', N'Phranakhon Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏพระนครศรีอยุธยา', N'Phranakhon Si Ayutthaya Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏพิบูลสงคราม', N'Pibulsongkram Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏเพชรบุรี', N'Phetchaburi Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏเพชรบูรณ์', N'Phetchabun Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏภูเก็ต', N'Phuket Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏมหาสารคาม', N'Maha Sarakham Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏยะลา', N'Yala Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏร้อยเอ็ด', N'Roi Et Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏราชนครินทร์', N'Rajanagarindra Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏรำไพพรรณี', N'Rambhai Barni Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏลำปาง', N'Lampang Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏเลย', N'Loei Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏวไลยอลงกรณ์ ในพระบรมราชูปถัมภ์', N'Valaya Alongkorn Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏศรีสะเกษ', N'Sisaket Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏสกลนคร', N'Sakon Nakhon Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏสงขลา', N'Songkhla Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏสวนสุนันทา', N'Suan Sunandha Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏสุราษฎร์ธานี', N'Suratthani Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏสุรินทร์', N'Surin Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏหมู่บ้านจอมบึง', N'Muban Chom Bung Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏอุดรธานี', N'Udon Thani Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏอุตรดิตถ์', N'Uttaradit Rajabhat University', 'public'),
    (N'มหาวิทยาลัยราชภัฏอุบลราชธานี', N'Ubon Ratchathani Rajabhat University', 'public'),

    -- มหาวิทยาลัยเทคโนโลยีราชมงคล (Rajamangala Universities of Technology)
    (N'มหาวิทยาลัยเทคโนโลยีราชมงคลกรุงเทพ', N'Rajamangala University of Technology Krungthep', 'public'),
    (N'มหาวิทยาลัยเทคโนโลยีราชมงคลตะวันออก', N'Rajamangala University of Technology Tawan-ok', 'public'),
    (N'มหาวิทยาลัยเทคโนโลยีราชมงคลธัญบุรี', N'Rajamangala University of Technology Thanyaburi', 'public'),
    (N'มหาวิทยาลัยเทคโนโลยีราชมงคลพระนคร', N'Rajamangala University of Technology Phra Nakhon', 'public'),
    (N'มหาวิทยาลัยเทคโนโลยีราชมงคลรัตนโกสินทร์', N'Rajamangala University of Technology Rattanakosin', 'public'),
    (N'มหาวิทยาลัยเทคโนโลยีราชมงคลล้านนา', N'Rajamangala University of Technology Lanna', 'public'),
    (N'มหาวิทยาลัยเทคโนโลยีราชมงคลศรีวิชัย', N'Rajamangala University of Technology Srivijaya', 'public'),
    (N'มหาวิทยาลัยเทคโนโลยีราชมงคลสุวรรณภูมิ', N'Rajamangala University of Technology Suvarnabhumi', 'public'),
    (N'มหาวิทยาลัยเทคโนโลยีราชมงคลอีสาน', N'Rajamangala University of Technology Isan', 'public'),

    -- มหาวิทยาลัยเอกชน (Private Universities)
    (N'มหาวิทยาลัยกรุงเทพ', N'Bangkok University', 'private'),
    (N'มหาวิทยาลัยกรุงเทพธนบุรี', N'Bangkokthonburi University', 'private'),
    (N'มหาวิทยาลัยกรุงเทพสุวรรณภูมิ', N'Bangkok Suvarnabhumi University', 'private'),
    (N'มหาวิทยาลัยการจัดการและเทคโนโลยีอีสเทิร์น', N'The Eastern University of Management and Technology', 'private'),
    (N'มหาวิทยาลัยเกริก', N'Krirk University', 'private'),
    (N'มหาวิทยาลัยเกษมบัณฑิต', N'Kasem Bundit University', 'private'),
    (N'มหาวิทยาลัยคริสเตียน', N'Christian University', 'private'),
    (N'มหาวิทยาลัยเฉลิมกาญจนา', N'Chalermkarnchana University', 'private'),
    (N'มหาวิทยาลัยตาปี', N'Tapee University', 'private'),
    (N'มหาวิทยาลัยเจ้าพระยา', N'Chaopraya University', 'private'),
    (N'มหาวิทยาลัยเซนต์จอห์น', N'Saint John''s University', 'private'),
    (N'มหาวิทยาลัยเทคโนโลยีมหานคร', N'Mahanakorn University of Technology', 'private'),
    (N'มหาวิทยาลัยธนบุรี', N'Thonburi University', 'private'),
    (N'มหาวิทยาลัยธุรกิจบัณฑิตย์', N'Dhurakij Pundit University', 'private'),
    (N'มหาวิทยาลัยนอร์ทกรุงเทพ', N'North Bangkok University', 'private'),
    (N'มหาวิทยาลัยนอร์ท-เชียงใหม่', N'North Chiang Mai University', 'private'),
    (N'มหาวิทยาลัยนานาชาติแสตมฟอร์ด', N'Stamford International University', 'private'),
    (N'มหาวิทยาลัยนานาชาติเอเชีย-แปซิฟิก', N'Asia-Pacific International University', 'private'),
    (N'มหาวิทยาลัยเนชั่น', N'Nation University', 'private'),
    (N'มหาวิทยาลัยปทุมธานี', N'Pathumthani University', 'private'),
    (N'มหาวิทยาลัยพายัพ', N'Payap University', 'private'),
    (N'มหาวิทยาลัยพิษณุโลก', N'Phitsanulok University', 'private'),
    (N'มหาวิทยาลัยฟาฏอนี', N'Fatoni University', 'private'),
    (N'มหาวิทยาลัยฟาร์อีสเทอร์น', N'The Far Eastern University', 'private'),
    (N'มหาวิทยาลัยภาคกลาง', N'The University of Central Thailand', 'private'),
    (N'มหาวิทยาลัยภาคตะวันออกเฉียงเหนือ', N'North Eastern University', 'private'),
    (N'มหาวิทยาลัยชินวัตร', N'Shinawatra University', 'private'),
    (N'มหาวิทยาลัยรังสิต', N'Rangsit University', 'private'),
    (N'มหาวิทยาลัยรัตนบัณฑิต', N'Rattana Bundit University', 'private'),
    (N'มหาวิทยาลัยราชธานี', N'Ratchathani University', 'private'),
    (N'มหาวิทยาลัยราชพฤกษ์', N'Rajapruk University', 'private'),
    (N'มหาวิทยาลัยวงษ์ชวลิตกุล', N'Vongchavalitkul University', 'private'),
    (N'มหาวิทยาลัยเวสเทิร์น', N'Western University', 'private'),
    (N'มหาวิทยาลัยศรีปทุม', N'Sripatum University', 'private'),
    (N'มหาวิทยาลัยสยาม', N'Siam University', 'private'),
    (N'มหาวิทยาลัยหอการค้าไทย', N'University of the Thai Chamber of Commerce', 'private'),
    (N'มหาวิทยาลัยหัวเฉียวเฉลิมพระเกียรติ', N'Huachiew Chalermprakiet University', 'private'),
    (N'มหาวิทยาลัยหาดใหญ่', N'Hatyai University', 'private'),
    (N'มหาวิทยาลัยอัสสัมชัญ', N'Assumption University', 'private'),
    (N'มหาวิทยาลัยอีสเทิร์นเอเชีย', N'Eastern Asia University', 'private'),
    (N'มหาวิทยาลัยเอเชียอาคเนย์', N'South-East Asia University', 'private'),
    (N'มหาวิทยาลัยนานาชาติเซนต์เทเรซา', N'St Theresa International University', 'private'),
    (N'มหาวิทยาลัยเซาธ์อีสท์บางกอก', N'Southeast Bangkok University', 'private'),
    (N'มหาวิทยาลัยเอเชียน', N'Asian University', 'private'),
    (N'มหาวิทยาลัยเว็บสเตอร์ (ประเทศไทย)', N'Webster University Thailand', 'private'),
    (N'สถาบันการจัดการปัญญาภิวัฒน์', N'Panyapiwat Institute of Management (PIM)', 'private');

    PRINT 'Seeded Universities';
END
GO

-- sp_GetUniversities
CREATE OR ALTER PROCEDURE sp_GetUniversities
AS
BEGIN
    SET NOCOUNT ON;
    SELECT UniversityID, UniversityNameThai, UniversityNameEng, UniversityType
    FROM Universities
    WHERE IsActive = 1
    ORDER BY UniversityNameThai;
END
GO
