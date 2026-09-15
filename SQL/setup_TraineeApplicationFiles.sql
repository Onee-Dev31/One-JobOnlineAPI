-- usp_ApplicantFile_Insert: บันทึกไฟล์แนบใบสมัคร (idCard/houseReg/resume) เข้า T_APPLICANT_FILES
-- ใช้แทนการ INSERT ตรงจาก TraineeApplicationsController.SaveFilesAsync

CREATE OR ALTER PROCEDURE usp_ApplicantFile_Insert
    @ApplicantID   INT,
    @ApplicationID INT,
    @FilePath      NVARCHAR(500),
    @FileName      NVARCHAR(255),
    @FileSize      BIGINT = NULL,
    @FileType      NVARCHAR(100) = NULL,
    @SectionFile   NVARCHAR(50) = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO T_APPLICANT_FILES (ApplicantID, ApplicationID, FilePath, FileName, FileSize, FileType, SectionFile)
    VALUES (@ApplicantID, @ApplicationID, @FilePath, @FileName, @FileSize, @FileType, @SectionFile)
END
GO
