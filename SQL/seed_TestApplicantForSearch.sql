-- Dev/test helper only — NOT part of the app runtime.
-- Creates one test applicant ("ทดสอบ ค้นหา") with up to 2 job applications so you can
-- try out sp_SearchApplicantsByName / GET /api/ApplicantNew/searchByName right away:
--   - one Status = 'Employment confirm'   -> frontend should show status only, no Reject button
--   - one Status = 'Waiting HR Nagotiate' -> frontend should show the Reject button
-- Picks the first 2 JobIDs it finds in Jobs (whatever you already have), so it should
-- run as-is on any environment that has at least 1 job posting.

DECLARE @TestApplicantID INT;
DECLARE @JobID1 INT, @JobID2 INT;

SELECT TOP 1 @JobID1 = JobID FROM Jobs ORDER BY JobID;
SELECT TOP 1 @JobID2 = JobID FROM Jobs WHERE JobID <> @JobID1 ORDER BY JobID;

IF @JobID1 IS NULL
BEGIN
    RAISERROR(N'ตาราง Jobs ไม่มีข้อมูลเลย ต้องมีอย่างน้อย 1 ตำแหน่งก่อนถึงจะสร้างใบสมัครทดสอบได้', 16, 1);
    RETURN;
END

INSERT INTO T_APPLICANTS (
    Title, FirstNameThai, LastNameThai, FirstNameEng, LastNameEng, Nickname, TitleENG, NicknameE,
    Gender, BirthDate, Height, Weight, CitizenID, CitizenIDIssuedBy, CitizenIDExpiresON,
    CurrentAddress, CurrentProvinceID, CurrentDistrictID, CurrentSubDistrictID, CurrentPostalCode,
    MobilePhone, Email, HomePhone, ReasonPosition, ReasonOther,
    InternshipStartDate, InternshipEndDate, InternshipType, DesiredField1, DesiredField2, DesiredField3,
    PlaceOfBirth, Nationality, Race, Religion,
    FatherName, FatherOccupation, FatherStatus, MotherName, MotherOccupation, MotherStatus,
    SiblingsAll, SiblingOrder,
    EmergencyName, EmergencyRelation, EmergencyAddress, EmergencyPhone,
    School, Faculty, Major, Minor, YearOfStudy, AdvisorName, AdvisorPhone, Activities,
    InfoSources, InfoSourceStaffName, InfoSourceDepartment, InfoSourceOther,
    UserID
)
VALUES (
    N'นาย', N'ทดสอบ', N'ค้นหา', 'Test', 'Search', N'เทส', 'Mr', 'Test',
    'M', '2000-01-01', 170, 60, '1111111111111', N'-', '2030-01-01',
    N'-', NULL, NULL, NULL, '10000',
    '0800000000', 'test.search@example.com', '-', N'-', N'-',
    NULL, NULL, NULL, N'-', N'-', N'-',
    N'กรุงเทพมหานคร', N'ไทย', N'ไทย', N'พุทธ',
    N'-', N'-', N'-', N'-', N'-', N'-',
    1, 1,
    N'-', N'-', N'-', '-',
    N'-', N'-', N'-', N'-', N'-', N'-', '-', N'-',
    N'-', N'-', N'-', N'-',
    NULL
);

SET @TestApplicantID = CAST(SCOPE_IDENTITY() AS INT);

INSERT INTO JobApplications (ApplicantID, JobID, Status, SubmissionDate)
VALUES (@TestApplicantID, @JobID1, 'Employment confirm', GETDATE());

IF @JobID2 IS NOT NULL
BEGIN
    INSERT INTO JobApplications (ApplicantID, JobID, Status, SubmissionDate)
    VALUES (@TestApplicantID, @JobID2, 'Waiting HR Nagotiate', GETDATE());
END
ELSE
BEGIN
    PRINT N'มีแค่ 1 ตำแหน่งใน Jobs เลยสร้างใบสมัครทดสอบให้แค่ใบเดียว (Employment confirm)';
END

PRINT N'สร้างผู้สมัครทดสอบแล้ว ApplicantID = ' + CAST(@TestApplicantID AS NVARCHAR(20));
PRINT N'ทดสอบด้วย: EXEC sp_SearchApplicantsByName @Name = N''ทดสอบ''';

-- ลบข้อมูลทดสอบทิ้งหลังเทสเสร็จ (uncomment 2 บรรทัดนี้ แล้วใส่ ApplicantID ที่ print ออกมา แล้วรันแยกต่างหาก):
-- DELETE FROM JobApplications WHERE ApplicantID = <ApplicantID ที่ได้จาก PRINT ด้านบน>;
-- DELETE FROM T_APPLICANTS WHERE ApplicantID = <ApplicantID ที่ได้จาก PRINT ด้านบน>;
