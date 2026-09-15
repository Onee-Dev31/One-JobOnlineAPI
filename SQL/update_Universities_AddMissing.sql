-- Adds public/private higher-ed institutions that MHESI (info.mhesi.go.th) counts as accredited
-- but were missing from the initial Wikipedia-sourced seed in setup_Universities.sql
-- (specialized public institutes, and private "college"/"institute" named institutions that
-- are legitimate degree-granting higher-ed institutions, not vocational schools).
SET QUOTED_IDENTIFIER ON;
GO

INSERT INTO Universities (UniversityNameThai, UniversityNameEng, UniversityType)
SELECT v.NameThai, v.NameEng, v.Type
FROM (VALUES
    -- Public: specialized autonomous/state institutes missing from the first seed
    (N'สถาบันบัณฑิตศึกษาจุฬาภรณ์', N'Chulabhorn Graduate Institute', 'public'),
    (N'วิทยาลัยวิทยาศาสตร์การแพทย์เจ้าฟ้าจุฬาภรณ์', N'HRH Princess Chulabhorn College of Medical Science', 'public'),
    (N'สถาบันดนตรีกัลยาณิวัฒนา', N'Princess Galyani Vadhana Institute of Music', 'public'),
    (N'สถาบันการพยาบาลศรีสวรินทิรา สภากาชาดไทย', N'Srisavarindhira Thai Red Cross Institute of Nursing', 'public'),
    (N'สถาบันเทคโนโลยีปทุมวัน', N'Pathumwan Institute of Technology', 'public'),
    (N'มหาวิทยาลัยการกีฬาแห่งชาติ', N'Thailand National Sports University', 'public'),
    (N'สถาบันบัณฑิตพัฒนศิลป์', N'Bunditpatanasilpa Institute', 'public'),
    (N'สถาบันพระบรมราชชนก', N'Praboromarajchanok Institute', 'public'),

    -- Private: degree-granting colleges/institutes missing from the first seed
    (N'วิทยาลัยดุสิตธานี', N'Dusit Thani College', 'private'),
    (N'วิทยาลัยเซนต์หลุยส์', N'Saint Louis College', 'private'),
    (N'วิทยาลัยเทคโนโลยีสยาม', N'Siam Technology College', 'private'),
    (N'วิทยาลัยเทคโนโลยีภาคใต้', N'Southern College of Technology', 'private'),
    (N'วิทยาลัยพุทธศาสตร์นานาชาติ', N'International Buddhist College', 'private'),
    (N'วิทยาลัยเชียงราย', N'Chiangrai College', 'private'),
    (N'วิทยาลัยนครราชสีมา', N'Nakhonratchasima College', 'private'),
    (N'วิทยาลัยบัณฑิตเอเชีย', N'College of Asian Scholars', 'private'),
    (N'วิทยาลัยพนมวันท์', N'Phanomwan College', 'private'),
    (N'วิทยาลัยสันตพล', N'Santapol College', 'private'),
    (N'วิทยาลัยทองสุข', N'Thongsook College', 'private'),
    (N'สถาบันอาศรมศิลป์', N'Arsom Silp Institute of the Arts', 'private'),
    (N'สถาบันรัชต์ภาคย์', N'Rajapark Institute', 'private'),
    (N'สถาบันเทคโนโลยีไทย-ญี่ปุ่น', N'Thai-Nichi Institute of Technology', 'private'),
    (N'สถาบันวิทยสิริเมธี', N'Vidyasirimedhi Institute of Science and Technology (VISTEC)', 'private'),
    (N'สถาบันกันตนา', N'Kantana Institute', 'private')
) AS v(NameThai, NameEng, Type)
WHERE NOT EXISTS (SELECT 1 FROM Universities u WHERE u.UniversityNameThai = v.NameThai);
GO
