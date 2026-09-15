-- Table: TraineeApplications
-- ใบสมัครนักศึกษาฝึกงาน (Trainee Application Form)
-- NOTE: verify that Provinces(ProvinceID) / Districts(DistrictID) / SubDistricts(SubDistrictID)
-- match the actual table/column names of the location tables already in this database.
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'TraineeApplications')
BEGIN
    CREATE TABLE TraineeApplications (
        TraineeApplicationID INT IDENTITY(1,1) PRIMARY KEY,

        -- ระยะเวลาฝึกงาน
        StartDate             DATE NULL,
        EndDate               DATE NULL,

        -- ความประสงค์ขอฝึกงาน
        DesiredField1         NVARCHAR(200) NULL,
        DesiredField2         NVARCHAR(200) NULL,
        DesiredField3         NVARCHAR(200) NULL,
        Reason                NVARCHAR(20) NULL,   -- course / experience / other
        ReasonOther           NVARCHAR(255) NULL,

        -- ประวัติส่วนตัว
        Name                  NVARCHAR(100) NOT NULL,
        Surname               NVARCHAR(100) NOT NULL,
        Nickname              NVARCHAR(50) NULL,
        DateOfBirth           DATE NULL,
        Age                   INT NULL,
        PlaceOfBirth          NVARCHAR(200) NULL,
        Nationality           NVARCHAR(50) NULL,
        Race                  NVARCHAR(50) NULL,
        Religion              NVARCHAR(50) NULL,
        Height                DECIMAL(5,2) NULL,   -- cm
        Weight                DECIMAL(5,2) NULL,   -- kg
        IDCardNo              NVARCHAR(20) NULL,
        IDIssuedBy            NVARCHAR(200) NULL,
        IDExpiredDate         DATE NULL,
        Address               NVARCHAR(500) NULL,
        ProvinceID            INT NULL FOREIGN KEY REFERENCES Provinces(ProvinceID),
        DistrictID            INT NULL FOREIGN KEY REFERENCES Districts(DistrictID),
        SubDistrictID         INT NULL FOREIGN KEY REFERENCES SubDistricts(SubDistrictID),
        PostalCode            NVARCHAR(10) NULL,
        Telephone             NVARCHAR(20) NULL,
        Mobile                NVARCHAR(20) NOT NULL,
        Email                 NVARCHAR(255) NOT NULL,

        -- ข้อมูลครอบครัว
        FatherName            NVARCHAR(200) NULL,
        FatherOccupation      NVARCHAR(200) NULL,
        FatherStatus          NVARCHAR(10) NULL,   -- alive / demised
        MotherName            NVARCHAR(200) NULL,
        MotherOccupation      NVARCHAR(200) NULL,
        MotherStatus          NVARCHAR(10) NULL,   -- alive / demised
        SiblingCount          INT NULL,
        SiblingOrder          INT NULL,

        -- บุคคลติดต่อกรณีฉุกเฉิน
        EmergencyName         NVARCHAR(200) NULL,
        EmergencyRelation     NVARCHAR(100) NULL,
        EmergencyAddress      NVARCHAR(500) NULL,
        EmergencyPhone        NVARCHAR(20) NULL,

        -- ประวัติการศึกษา
        School                NVARCHAR(255) NOT NULL,
        Faculty               NVARCHAR(255) NULL,
        Major                 NVARCHAR(255) NULL,
        Minor                 NVARCHAR(255) NULL,
        YearOfStudy           NVARCHAR(20) NULL,
        AdvisorName           NVARCHAR(200) NULL,
        AdvisorPhone          NVARCHAR(20) NULL,
        Activities            NVARCHAR(MAX) NULL,

        -- ทราบข้อมูลจากช่องทางใด (multi-select, เก็บเป็น JSON array เช่น ["website","staff"])
        InfoSources           NVARCHAR(200) NULL,
        InfoSourceStaffName   NVARCHAR(200) NULL,
        InfoSourceDepartment  NVARCHAR(200) NULL,
        InfoSourceOther       NVARCHAR(255) NULL,

        -- Workflow
        Status                NVARCHAR(20) NOT NULL DEFAULT 'pending', -- pending / approved / rejected
        CreatedAt             DATETIME2 NOT NULL DEFAULT GETDATE(),
        UpdatedAt             DATETIME2 NULL,

        CONSTRAINT CHK_TraineeApplications_Reason
            CHECK (Reason IS NULL OR Reason IN ('course', 'experience', 'other')),
        CONSTRAINT CHK_TraineeApplications_ReasonOther
            CHECK (Reason <> 'other' OR ReasonOther IS NOT NULL),
        CONSTRAINT CHK_TraineeApplications_FatherStatus
            CHECK (FatherStatus IS NULL OR FatherStatus IN ('alive', 'demised')),
        CONSTRAINT CHK_TraineeApplications_MotherStatus
            CHECK (MotherStatus IS NULL OR MotherStatus IN ('alive', 'demised')),
        CONSTRAINT CHK_TraineeApplications_Status
            CHECK (Status IN ('pending', 'approved', 'rejected')),
        CONSTRAINT CHK_TraineeApplications_InfoSources
            CHECK (InfoSources IS NULL OR ISJSON(InfoSources) = 1)
    )

    CREATE INDEX IX_TraineeApplications_Status ON TraineeApplications(Status)
    CREATE INDEX IX_TraineeApplications_Email ON TraineeApplications(Email)
    CREATE INDEX IX_TraineeApplications_ProvinceID ON TraineeApplications(ProvinceID)
    CREATE INDEX IX_TraineeApplications_DistrictID ON TraineeApplications(DistrictID)
    CREATE INDEX IX_TraineeApplications_SubDistrictID ON TraineeApplications(SubDistrictID)

    PRINT 'Created TraineeApplications'
END

GO
