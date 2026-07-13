-- sp_GetJobSlotAssignmentSeedData: อ่านข้อมูล Part1 (JobSlotAssignments) เพื่อ prefill ฟอร์ม Part2
-- คอลัมน์ alias ตรงกับ field ของ Models/TraineeApplication.cs โดยตรง เพื่อให้ frontend
-- เอา response ไปใส่ formData ได้เลยโดยไม่ต้อง map ซ้ำ
-- Result set ที่ 2 คือไฟล์แนบที่อัปโหลดไว้ตอน Part1 (JobSlotAssignmentFiles) เผื่อ Part2
-- อยากโชว์/ใช้ไฟล์เดิมต่อ ไม่ต้องให้ผู้สมัครอัปโหลดซ้ำ
-- Read-only: ไม่แก้ schema ของ JobSlotAssignments/JobSlotAssignmentFiles หรือ sp_AssignApplicantToSlot ใด ๆ

CREATE OR ALTER PROCEDURE sp_GetJobSlotAssignmentSeedData
    @AssignmentID INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        A.AssignmentID,
        A.ManualTitle                   AS PrefixT,
        A.ManualFirstNameThai           AS NameFirstT,
        A.ManualLastNameThai            AS NameLastT,
        A.ManualNickname                AS NicknameT,
        A.ManualMobilePhone             AS Mobile,
        A.ManualEmail                   AS Email,
        A.ManualYear                    AS YearOfStudy,
        A.ManualMajor                   AS Major,
        A.ManualFaculty                 AS Faculty,
        A.ManualUniversity              AS School,
        A.ManualInternStartDate         AS StartDate,
        A.ManualInternEndDate           AS EndDate,
        A.ManualPreferredPosition       AS DesiredField1,
        A.ManualPreferredPositionBackup AS DesiredField2,
        A.ManualInternshipType          AS InternshipType
    FROM JobSlotAssignments A
    WHERE A.AssignmentID = @AssignmentID

    SELECT
        F.FileID,
        F.AssignmentID,
        F.FilePath,
        F.FileName,
        F.FileSize,
        F.FileType,
        F.SectionFile,
        F.UploadedDate
    FROM JobSlotAssignmentFiles F
    WHERE F.AssignmentID = @AssignmentID
END
GO
