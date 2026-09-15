SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
ALTER PROCEDURE [dbo].[sp_UpdateApplicantStatusV3]
    @ApplicantID INT,
    @Status NVARCHAR(50) = NULL,
    @JobID INT = NULL,
    @Remark NVARCHAR(MAX) = NULL,
    @RankOfSelect INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    -- Step 1: ดึง Status ถ้าไม่ส่งมา
    IF @Status IS NULL OR LTRIM(RTRIM(@Status)) = ''
    BEGIN
        SELECT @Status = Status
        FROM JobApplications
        WHERE ApplicantID = @ApplicantID AND JobID = @JobID;
    END

    -- Step 2: นับจำนวน job ของผู้สมัคร
    DECLARE @JobCount INT;
    SELECT @JobCount = COUNT(*)
    FROM JobApplications
    WHERE ApplicantID = @ApplicantID;

    -- Step 3: กรณีสมัครหลายตำแหน่ง และ Status = Confirmed เดิม => Success
    IF @JobCount > 1 AND @Status = 'Nagotiate Success'
    BEGIN
        -- อัปเดต job ที่เลือกให้เป็น Success
        UPDATE JobApplications
        SET
            Status = 'Nagotiate Success',
            RankOfSelect = @RankOfSelect,
            Remark = CASE
                        WHEN @Remark IS NOT NULL AND LTRIM(RTRIM(@Remark)) <> ''
                        THEN @Remark
                        ELSE Remark
                     END
        WHERE ApplicantID = @ApplicantID AND JobID = @JobID;

        -- ปิดไว้ก่อน: concept auto-reject ตำแหน่งอื่นยังไม่สรุปกับทีม (2026-07-21)
        /*
        DECLARE @JobTitle NVARCHAR(500);
        SELECT @JobTitle = JobTitle
        FROM Jobs
        WHERE JobID = @JobID;

        -- อัปเดต job อื่นให้เป็น Reject
        UPDATE JobApplications
        SET
            Status = 'Reject',
            Remark = CONCAT(N'ผู้สมัครได้ตกลงเริ่มงานในตำแหน่ง ', @JobTitle, N' แล้ว')
        WHERE ApplicantID = @ApplicantID AND JobID <> @JobID;
        */
    END
    ELSE
    BEGIN
        IF @Status = 'Waiting HR Nagotiate'
        BEGIN
            DECLARE @NextRank INT;
            DECLARE @CurrentRank INT;
            SELECT @CurrentRank = RankOfSelect
            FROM [dbo].[JobApplications]
            WHERE ApplicantID = @ApplicantID AND JobID = @JobID;

            IF @CurrentRank IS NULL  -- หรือเพิ่ม AND Status != 'Waiting HR Nagotiate' ถ้าต้องการ strict
            BEGIN
                SELECT @NextRank = ISNULL(MAX(RankOfSelect), 0) + 1
                FROM [dbo].[JobApplications]
                WHERE JobID = @JobID;

                UPDATE [dbo].[JobApplications]
                SET Status = @Status,
                    RankOfSelect = @NextRank
                WHERE ApplicantID = @ApplicantID AND JobID = @JobID;
        END
    ELSE
    BEGIN
        -- ถ้าอยาก update Status/Remark แต่ไม่เปลี่ยน rank
        UPDATE [dbo].[JobApplications]
        SET Status = @Status
        WHERE ApplicantID = @ApplicantID AND JobID = @JobID;
    END
        END
        ELSE
        BEGIN
            UPDATE JobApplications
            SET
                Status = @Status,
                Remark = CASE
                   WHEN @Remark IS NOT NULL AND LTRIM(RTRIM(@Remark)) <> ''
                   THEN @Remark
                   ELSE Remark
                END
            WHERE ApplicantID = @ApplicantID AND JobID = @JobID;

            -- [เอาไป update ใน TraineeAssignments]
            IF @Status = 'Employment confirm'
            BEGIN
                DECLARE @ApplicationID INT;
                DECLARE @DepartmentCode NVARCHAR(50);
                DECLARE @CompanyCode NVARCHAR(50);

                SELECT @ApplicationID = ApplicationID
                FROM dbo.JobApplications
                WHERE ApplicantID = @ApplicantID
                  AND JobID = @JobID;

                SELECT @DepartmentCode = Department
                FROM dbo.Jobs
                WHERE JobID = @JobID;

                SELECT @CompanyCode = CompanyCode
                FROM dbo.fn_GetCompanyCodeFromCoscent(@DepartmentCode);

                UPDATE dbo.TraineeAssignments
                SET
                    CompanyCode = @CompanyCode,
                    DepartmentCode = @DepartmentCode
                WHERE ApplicationID = @ApplicationID;
            END
        END

        ---- Step 4: อัปเดตปกติในกรณีสมัครตำแหน่งเดียว หรือไม่ใช่ Success  -- ของเดิมก่อน Update 14/08/68
        --UPDATE JobApplications
        --SET
        --    Status = @Status,
        --    RankOfSelect = @RankOfSelect,
        --    Remark = CASE
        --                WHEN @Remark IS NOT NULL AND LTRIM(RTRIM(@Remark)) <> ''
        --                THEN @Remark
        --                ELSE Remark
        --             END
        --WHERE ApplicantID = @ApplicantID AND JobID = @JobID;
        --DECLARE @NextRank INT;
        --SELECT @NextRank = MAX(RankOfSelect)
        --FROM JobApplications
        --WHERE JobID = 1
        --  AND RankOfSelect IS NOT NULL;

        ---- ถ้าไม่มี (NULL) ให้เริ่มที่ 1
        --SET @NextRank = ISNULL(@NextRank, 0) + 1;
    END
END

GO
