-- Reconciles Universities against the official MHESI open-data registry
-- (data.mhesi.go.th, academic year 2566 dataset: univ_uni_11_01_2566.xlsx).
SET QUOTED_IDENTIFIER ON;
GO

-- Remove institutions that have ceased operations
DELETE FROM Universities WHERE UniversityNameThai IN (
    N'มหาวิทยาลัยเว็บสเตอร์ (ประเทศไทย)',  -- Webster University Thailand, closed Dec 2021
    N'มหาวิทยาลัยเอเชียน'                   -- Asian University, ordered to close 2024
);
GO

-- Remove institutions officially classified by MHESI as "non-Ministry affiliated"
-- (same excluded bucket as military/police academies), not public MHESI universities
DELETE FROM Universities WHERE UniversityNameThai IN (
    N'มหาวิทยาลัยการกีฬาแห่งชาติ',
    N'สถาบันบัณฑิตพัฒนศิลป์',
    N'สถาบันพระบรมราชชนก',
    N'วิทยาลัยวิทยาศาสตร์การแพทย์เจ้าฟ้าจุฬาภรณ์'
);
GO

-- Correct Thai names to match the official registry's legal names
UPDATE Universities SET UniversityNameThai = N'มหาวิทยาลัยเฉลิมกาญจนา ศรีสะเกษ' WHERE UniversityNameThai = N'มหาวิทยาลัยเฉลิมกาญจนา';
UPDATE Universities SET UniversityNameThai = N'วิทยาลัยเทคโนโลยีพนมวันท์' WHERE UniversityNameThai = N'วิทยาลัยพนมวันท์';
UPDATE Universities SET UniversityNameThai = N'วิทยาลัยพุทธศาสนานานาชาติ' WHERE UniversityNameThai = N'วิทยาลัยพุทธศาสตร์นานาชาติ';
UPDATE Universities SET UniversityNameThai = N'สถาบันการพยาบาลศรีสวรินทิราสภากาชาดไทย' WHERE UniversityNameThai = N'สถาบันการพยาบาลศรีสวรินทิรา สภากาชาดไทย';
GO

-- Add institutions confirmed present in the official registry but missing from our seed
INSERT INTO Universities (UniversityNameThai, UniversityNameEng, UniversityType)
SELECT v.NameThai, v.NameEng, v.Type
FROM (VALUES
    -- Public
    (N'สถาบันเทคโนโลยีจิตรลดา', N'Chitralada Technology Institute', 'public'),
    (N'สถาบันวิทยาลัยชุมชน', N'Institute of Community Colleges (ICCs)', 'public'),

    -- Private
    (N'วิทยาลัยนอร์ทเทิร์น', N'Northern College', 'private'),
    (N'วิทยาลัยแสงธรรม', N'Saengtham College', 'private'),
    (N'วิทยาลัยอินเตอร์เทคลำปาง', N'Lampang Inter-Tech College', 'private'),
    (N'วิทยาลัยพิชญบัณฑิต', N'Pitchayabundit College', 'private'),
    (N'วิทยาลัยนานาชาติราฟเฟิลส์', N'Raffles International College', 'private'),
    (N'สถาบันวิทยาการประกอบการแห่งอโยธยา', N'Institute of Entrepreneurial Science Ayothaya (IESA)', 'private'),
    (N'สถาบันการเรียนรู้เพื่อปวงชน', N'Learning Institute For Everyone (LIFE)', 'private'),
    (N'สถาบันวิทยาการจัดการแห่งแปซิฟิค', N'Pacific Institute of Management Science (PIMS)', 'private'),
    (N'สถาบันเทคโนโลยีแห่งสุวรรณภูมิ', N'Suvarnabhumi Institute of Technology (SVIT)', 'private')
) AS v(NameThai, NameEng, Type)
WHERE NOT EXISTS (SELECT 1 FROM Universities u WHERE u.UniversityNameThai = v.NameThai);
GO
