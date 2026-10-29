/* =========================================================
   DEPARTMENTS
========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='Departments')
BEGIN
    CREATE TABLE Departments
    (
        DepartmentId INT IDENTITY(1,1) PRIMARY KEY,
        DepartmentName VARCHAR(100) NOT NULL UNIQUE,
        CreatedBy VARCHAR(100) NULL,
        CreatedDate DATETIME2  NULL,
        LastModifiedBy VARCHAR(100) NULL,
        LastModifiedDate DATETIME2 NULL,
        IsActive BIT NOT NULL DEFAULT(1)
    );
END
GO

/* =========================================================
   COTIVITY DEPARTMENTS
========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='CotivityDepartments')
BEGIN
    CREATE TABLE CotivityDepartments
    (
        CotivityDepartmentId INT IDENTITY(1,1) PRIMARY KEY,
        CotivityDepartmentName VARCHAR(100) NOT NULL UNIQUE,
        CreatedBy VARCHAR(100) NULL,
        CreatedDate DATETIME2  NULL,
        LastModifiedBy VARCHAR(100) NULL,
        LastModifiedDate DATETIME2 NULL,
        IsActive BIT NOT NULL DEFAULT(1)
    );
END
GO

/* =========================================================
   DESIGNATIONS
========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='Designations')
BEGIN
    CREATE TABLE Designations
    (
        DesignationId INT IDENTITY(1,1) PRIMARY KEY,
        DesignationName VARCHAR(100) NOT NULL UNIQUE,
        [Rank] INT NOT NULL,
        CreatedBy VARCHAR(100),
        CreatedDate DATETIME2  NULL,
        LastModifiedBy VARCHAR(100),
        LastModifiedDate DATETIME2,
        IsActive BIT NOT NULL DEFAULT(1)
    );
END
GO

/* =========================================================
   COTIVITY DESIGNATIONS
========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='CotivityDesignations')
BEGIN
    CREATE TABLE CotivityDesignations
    (
        CotivityDesignationId INT IDENTITY(1,1) PRIMARY KEY,
        CotivityDesignationName VARCHAR(100) NOT NULL UNIQUE,
        CotivityRank INT NOT NULL,
        CreatedBy VARCHAR(100),
        CreatedDate DATETIME2  NULL,
        LastModifiedBy VARCHAR(100),
        LastModifiedDate DATETIME2,
        IsActive BIT NOT NULL DEFAULT(1)
    );
END
GO

/* =========================================================
   BLOOD GROUPS
========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='BloodGroups')
BEGIN
    CREATE TABLE BloodGroups
    (
        BloodGroupId INT IDENTITY(1,1) PRIMARY KEY,
        BloodGroupName VARCHAR(5) NOT NULL UNIQUE,
        CreatedBy VARCHAR(100),
        CreatedDate DATETIME2  NULL,
        LastModifiedBy VARCHAR(100),
        LastModifiedDate DATETIME2,
        IsActive BIT NOT NULL DEFAULT(1)
    );
END
GO

/* =========================================================
   GENDER
========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='Gender')
BEGIN
    CREATE TABLE Gender
    (
        GenderId INT IDENTITY(1,1) PRIMARY KEY,
        GenderName VARCHAR(50) NOT NULL UNIQUE,
        CreatedBy VARCHAR(100),
        CreatedDate DATETIME2  NULL,
        LastModifiedBy VARCHAR(100),
        LastModifiedDate DATETIME2,
        IsActive BIT NOT NULL DEFAULT(1)
    );
END
GO

/* =========================================================
   ADDRESS TYPE
========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='AddressType')
BEGIN
    CREATE TABLE AddressType
    (
        AddressTypeId INT IDENTITY(1,1) PRIMARY KEY,
        AddressTypeName VARCHAR(100) NOT NULL UNIQUE,
        CreatedBy VARCHAR(100),
        CreatedDate DATETIME2  NULL,
        LastModifiedBy VARCHAR(100),
        LastModifiedDate DATETIME2,
        IsActive BIT NOT NULL DEFAULT(1)
    );
END
GO

/* =========================================================
   ROLES
========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='Roles')
BEGIN
    CREATE TABLE Roles
    (
        RoleId INT IDENTITY(1,1) PRIMARY KEY,
        RoleName VARCHAR(100) NOT NULL UNIQUE,
        CreatedBy VARCHAR(100),
        CreatedDate DATETIME2  NULL,
        LastModifiedBy VARCHAR(100),
        LastModifiedDate DATETIME2,
        IsActive BIT NOT NULL DEFAULT(1)
    );
END
GO

/* =========================================================
   TEAMS
========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='Teams')
BEGIN
    CREATE TABLE Teams
    (
        TeamId INT IDENTITY(1,1) PRIMARY KEY,
        TeamName VARCHAR(100) NOT NULL UNIQUE,
        CreatedBy VARCHAR(100),
        CreatedDate DATETIME2  NULL,
        LastModifiedBy VARCHAR(100),
        LastModifiedDate DATETIME2,
        IsActive BIT NOT NULL DEFAULT(1)
    );
END
GO

/* =========================================================
   SKILLS
========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='Skills')
BEGIN
    CREATE TABLE Skills
    (
        SkillId INT IDENTITY(1,1) PRIMARY KEY,
        SkillName VARCHAR(100) NOT NULL UNIQUE,
        CreatedBy VARCHAR(100),
        CreatedDate DATETIME2  NULL,
        LastModifiedBy VARCHAR(100),
        LastModifiedDate DATETIME2,
        IsActive BIT NOT NULL DEFAULT(1)
    );
END
GO

/* =========================================================
   RECRUITMENTS
========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='Recruitments')
BEGIN
    CREATE TABLE Recruitments
    (
        RecruitmentId INT IDENTITY(1,1) PRIMARY KEY,
        RecruitmentName VARCHAR(100) NOT NULL UNIQUE,
        CreatedBy VARCHAR(100),
        CreatedDate DATETIME2  NULL,
        LastModifiedBy VARCHAR(100),
        LastModifiedDate DATETIME2,
        IsActive BIT NOT NULL DEFAULT(1)
    );
END
GO

/* =========================================================
   JOB TYPES
========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='JobTypes')
BEGIN
    CREATE TABLE JobTypes
    (
        JobTypeId INT IDENTITY(1,1) PRIMARY KEY,
        JobTypeName VARCHAR(100) NOT NULL UNIQUE,
        CreatedBy VARCHAR(100),
        CreatedDate DATETIME2  NULL,
        LastModifiedBy VARCHAR(100),
        LastModifiedDate DATETIME2,
        IsActive BIT NOT NULL DEFAULT(1)
    );
END
GO

/* =========================================================
   COTIVITY EMPLOYEES
========================================================= */
IF OBJECT_ID('dbo.CotivityEmployees', 'U') IS NOT NULL
BEGIN
    DROP TABLE dbo.CotivityEmployees;
END
GO

CREATE TABLE dbo.CotivityEmployees
(
    CotivityEmployeeId INT IDENTITY(1,1) PRIMARY KEY,
    CotivityEmployeeName VARCHAR(150) NOT NULL,

    CotivityDepartmentId INT NOT NULL,
    CotivityDepartmentDesignationId INT NOT NULL,

    ReportsToCotivityEmployeeId INT NULL,

    CreatedBy VARCHAR(100) NULL,
    CreatedDate DATETIME2 NULL,
    LastModifiedBy VARCHAR(100) NULL,
    LastModifiedDate DATETIME2 NULL,

    IsActive BIT NOT NULL DEFAULT(1),

    CONSTRAINT CK_CotivityEmployees_NoSelfReporting
        CHECK (
            ReportsToCotivityEmployeeId IS NULL
            OR ReportsToCotivityEmployeeId <> CotivityEmployeeId
        ),

    CONSTRAINT UQ_CotivityEmployees UNIQUE
    (
        CotivityEmployeeName,
        CotivityDepartmentId,
        CotivityDepartmentDesignationId,
        ReportsToCotivityEmployeeId
    ),

    CONSTRAINT FK_CotivityEmployees_Department
        FOREIGN KEY (CotivityDepartmentId)
        REFERENCES CotivityDepartments(CotivityDepartmentId),

    CONSTRAINT FK_CotivityEmployees_Designation
        FOREIGN KEY (CotivityDepartmentDesignationId)
        REFERENCES CotivityDesignations(CotivityDesignationId),

    CONSTRAINT FK_CotivityEmployees_Manager
        FOREIGN KEY (ReportsToCotivityEmployeeId)
        REFERENCES CotivityEmployees(CotivityEmployeeId)
);
GO
/* =========================================================
   EMPLOYEES
========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Employees')
BEGIN
    CREATE TABLE dbo.Employees
    (
        EmployeeId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,

        ENumber INT NOT NULL UNIQUE,

        FirstName VARCHAR(100) NOT NULL,
        MiddleName VARCHAR(100) NULL,
        LastName VARCHAR(100) NOT NULL,

        JoinedDate DATE NOT NULL,
        JoinedDateBS VARCHAR(10) NULL,

        Email VARCHAR(150) NOT NULL UNIQUE,
        AlternateEmail VARCHAR(150) NULL,

        DesignationId INT NOT NULL,
        DepartmentId INT NOT NULL,
        JobTypeId INT NOT NULL,
        BloodGroupId INT NOT NULL,
        GenderId INT NOT NULL,

        ReportsToEmployeeId INT NULL,
        LeadEmployeeId INT NULL,

        StationNumber VARCHAR(20) NULL,
        RecruitmentId INT NULL,
        ExtensionNumber INT NOT NULL,

        CitizenshipNumber VARCHAR(50) NOT NULL,

        DateOfBirth DATE NULL,
        DateOfBirthBS VARCHAR(10) NULL,

        PhoneNumber VARCHAR(20) NULL,
        MobileNumber VARCHAR(20) NULL,

        EmergencyContactNumber VARCHAR(20) NULL,
        EmergencyContactPerson VARCHAR(150) NOT NULL,

        FatherName VARCHAR(150) NOT NULL,
        MotherName VARCHAR(150) NULL,
        SpouseName VARCHAR(150) NULL,

        PANNumber VARCHAR(50) NOT NULL,
        LockerNumber VARCHAR(20) NULL,

        --RoleId INT NOT NULL,
        TeamId INT NOT NULL,

        BusinessUnitId INT NULL,

        CotivityEmployeeId INT NULL,

        IsActive BIT NOT NULL DEFAULT(1),

        CreatedBy VARCHAR(100) NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),

        LastModifiedBy VARCHAR(100) NULL,
        LastModifiedDate DATETIME2 NULL,

        CONSTRAINT CK_Employees_Email
            CHECK (Email LIKE '%@%.%'),

        CONSTRAINT FK_Employees_Designation
            FOREIGN KEY (DesignationId)
            REFERENCES dbo.Designations(DesignationId),

        CONSTRAINT FK_Employees_Department
            FOREIGN KEY (DepartmentId)
            REFERENCES dbo.Departments(DepartmentId),

        CONSTRAINT FK_Employees_JobType
            FOREIGN KEY (JobTypeId)
            REFERENCES dbo.JobTypes(JobTypeId),

        CONSTRAINT FK_Employees_BloodGroup
            FOREIGN KEY (BloodGroupId)
            REFERENCES dbo.BloodGroups(BloodGroupId),

        CONSTRAINT FK_Employees_Gender
            FOREIGN KEY (GenderId)
            REFERENCES dbo.Gender(GenderId),

        CONSTRAINT FK_Employees_Recruitment
            FOREIGN KEY (RecruitmentId)
            REFERENCES dbo.Recruitments(RecruitmentId),

       -- CONSTRAINT FK_Employees_Role
       --    FOREIGN KEY (RoleId)
       --     REFERENCES dbo.Roles(RoleId),

        CONSTRAINT FK_Employees_Team
            FOREIGN KEY (TeamId)
            REFERENCES dbo.Teams(TeamId),

        CONSTRAINT FK_Employees_CotivityEmployee
            FOREIGN KEY (CotivityEmployeeId)
            REFERENCES dbo.CotivityEmployees(CotivityEmployeeId),

        CONSTRAINT FK_Employees_Manager
            FOREIGN KEY (ReportsToEmployeeId)
            REFERENCES dbo.Employees(EmployeeId),

        CONSTRAINT FK_Employees_Lead
            FOREIGN KEY (LeadEmployeeId)
            REFERENCES dbo.Employees(EmployeeId)
    );
END
GO
IF COL_LENGTH('dbo.Employees', 'JoinedDateBS') IS NULL
    ALTER TABLE dbo.Employees ADD JoinedDateBS VARCHAR(10) NULL;
IF COL_LENGTH('dbo.Employees', 'IsDeleted') IS NULL
    ALTER TABLE dbo.Employees ADD IsDeleted BIT NOT NULL CONSTRAINT DF_Employees_IsDeleted DEFAULT (0);
IF COL_LENGTH('dbo.Employees', 'DeletedDate') IS NULL
    ALTER TABLE dbo.Employees ADD DeletedDate DATETIME2 NULL;
IF COL_LENGTH('dbo.Employees', 'DeletedBy') IS NULL
    ALTER TABLE dbo.Employees ADD DeletedBy VARCHAR(100) NULL;
IF COL_LENGTH('dbo.Employees', 'DateOfBirthBS') IS NULL
    ALTER TABLE dbo.Employees ADD DateOfBirthBS VARCHAR(10) NULL;
IF COL_LENGTH('dbo.Employees', 'PhoneNumber') IS NULL
    ALTER TABLE dbo.Employees ADD PhoneNumber VARCHAR(20) NULL;
IF COL_LENGTH('dbo.Employees', 'MobileNumber') IS NULL
    ALTER TABLE dbo.Employees ADD MobileNumber VARCHAR(20) NULL;
IF COL_LENGTH('dbo.Employees', 'EmergencyContactNumber') IS NULL
    ALTER TABLE dbo.Employees ADD EmergencyContactNumber VARCHAR(20) NULL;
IF COL_LENGTH('dbo.Employees', 'EmergencyContactPerson') IS NULL
    ALTER TABLE dbo.Employees ADD EmergencyContactPerson VARCHAR(150) NULL;
IF COL_LENGTH('dbo.Employees', 'FatherName') IS NULL
    ALTER TABLE dbo.Employees ADD FatherName VARCHAR(150) NULL;
IF COL_LENGTH('dbo.Employees', 'MotherName') IS NULL
    ALTER TABLE dbo.Employees ADD MotherName VARCHAR(150) NULL;
IF COL_LENGTH('dbo.Employees', 'SpouseName') IS NULL
    ALTER TABLE dbo.Employees ADD SpouseName VARCHAR(150) NULL;
IF COL_LENGTH('dbo.Employees', 'BusinessUnitId') IS NULL
    ALTER TABLE dbo.Employees ADD BusinessUnitId INT NULL;
IF COL_LENGTH('dbo.Employees', 'ReleaseDate') IS NOT NULL
    ALTER TABLE dbo.Employees DROP COLUMN ReleaseDate;
IF COL_LENGTH('dbo.Employees', 'ReasonForRelease') IS NOT NULL
    ALTER TABLE dbo.Employees DROP COLUMN ReasonForRelease;
GO
IF NOT EXISTS (
    SELECT 1
    FROM sys.tables
    WHERE name = 'EmployeeReleases'
)
BEGIN
    CREATE TABLE dbo.EmployeeReleases
    (
        ReleasedId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmployeeId INT NOT NULL,

        ReleaseType VARCHAR(100) NOT NULL,
        ResignationDate DATE NULL,
        NoticeDate DATE NULL,
        LastWorkingDate DATE NULL,

        NoticePeriodDays INT NULL,
        ServedNoticeDays INT NULL,

        Reason NVARCHAR(500) NULL,
        EarlyReleaseReason NVARCHAR(500) NULL,
        Remarks NVARCHAR(MAX) NULL,

        KnowledgeTransferCompleted BIT NULL,
        AssetsReturned BIT NULL,
        ExitInterviewCompleted BIT NULL,

        SettlementAmount DECIMAL(18,2) NULL,

        RelievingLetterIssued BIT NULL,
        ExperienceLetterIssued BIT NULL,

        Status VARCHAR(50) NOT NULL,

        ApprovedBy Varchar(100) NULL,
        ApprovedDate DATETIME2 NULL,

        CreatedBy Varchar(100) Not NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),

        LastModifiedBy Varchar(100) NULL,
        LastModifiedDate DATETIME2 NULL,

        CONSTRAINT FK_EmployeeReleases_Employee
            FOREIGN KEY (EmployeeId)
            REFERENCES dbo.Employees(EmployeeId)
    );
END
GO

-- Idempotent: add EarlyReleaseReason to existing EmployeeReleases tables
IF NOT EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.EmployeeReleases')
        AND name = 'EarlyReleaseReason'
)
BEGIN
    ALTER TABLE dbo.EmployeeReleases
        ADD EarlyReleaseReason NVARCHAR(500) NULL;
END
GO
IF NOT EXISTS (
    SELECT 1
    FROM sys.tables
    WHERE name = 'EmployeeTerminates'
)
BEGIN
    CREATE TABLE dbo.EmployeeTerminates
    (
        TerminationId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        EmployeeId INT NOT NULL,

        TerminationType VARCHAR(100) NOT NULL,
        EffectiveDate DATE NULL,

        InvestigationRequired BIT NULL,
        InvestigationSummary NVARCHAR(MAX) NULL,

        WarningCount INT NULL,
        LastWarningDate DATE NULL,

        Reason NVARCHAR(500) NULL,
        Remarks NVARCHAR(MAX) NULL,

        AssetRecovered BIT NULL,
        SystemAccessRevoked BIT NULL,
        ExitInterviewCompleted BIT NULL,

        SettlementAmount DECIMAL(18,2) NULL,

        EligibleForRehire BIT NULL,

        TerminationLetterIssued BIT NULL,
        TerminationLetterDate DATE NULL,

        Status VARCHAR(50) NOT NULL,

        ApprovedBy Varchar(100) NULL,
        ApprovedDate DATETIME2 NULL,

        CreatedBy Varchar(100) NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETDATE(),

        LastModifiedBy Varchar(100) NULL,
        LastModifiedDate DATETIME2 NULL,

        CONSTRAINT FK_EmployeeTerminates_Employee
            FOREIGN KEY (EmployeeId)
            REFERENCES dbo.Employees(EmployeeId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='EmployeeAddresses')
BEGIN
    CREATE TABLE EmployeeAddresses
    (
        AddressId INT IDENTITY(1,1) PRIMARY KEY,
        EmployeeId INT NOT NULL,
        AddressTypeId INT NOT NULL,
        AddressLine1 VARCHAR(160) NOT NULL,
        AddressLine2 VARCHAR(160) NULL,
        City VARCHAR(100) NOT NULL,
        [State] VARCHAR(100) NOT NULL,
        Latitude DECIMAL(10,8) NOT NULL,
        Longitude DECIMAL(11,8) NOT NULL,
        IsPrimary BIT NOT NULL DEFAULT(0),
        CreatedBy VARCHAR(100) NULL,
        CreatedDate DATETIME2 NULL,
        LastModifiedBy VARCHAR(100) NULL,
        LastModifiedDate DATETIME2 NULL,
        CONSTRAINT FK_EmployeeAddresses_Employee FOREIGN KEY(EmployeeId) REFERENCES Employees(EmployeeId),
        CONSTRAINT FK_EmployeeAddresses_AddressType FOREIGN KEY(AddressTypeId) REFERENCES AddressType(AddressTypeId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='EmployeeSkills')
BEGIN
    CREATE TABLE EmployeeSkills
    (
        EmployeeSkillId INT IDENTITY(1,1) PRIMARY KEY,
        EmployeeId INT NOT NULL,
        SkillId INT NOT NULL,
        SkillLevel INT NOT NULL CHECK(SkillLevel BETWEEN 1 AND 5),
        IsPrimary BIT NOT NULL DEFAULT(0),
        CreatedBy VARCHAR(100) NULL,
        CreatedDate DATETIME2 NULL,
        LastModifiedBy VARCHAR(100) NULL,
        LastModifiedDate DATETIME2 NULL,
        CONSTRAINT FK_EmployeeSkills_Employee FOREIGN KEY(EmployeeId) REFERENCES Employees(EmployeeId),
        CONSTRAINT FK_EmployeeSkills_Skill FOREIGN KEY(SkillId) REFERENCES Skills(SkillId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='EmployeeImages')
BEGIN
CREATE TABLE EmployeeImages
(
    ImageId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    ImagePath VARCHAR(500) NOT NULL,

    CreatedBy VARCHAR(100) NULL,
    CreatedDate DATETIME NULL,

    LastModifiedBy VARCHAR(100) NULL,
    LastModifiedDate DATETIME NULL,

    CONSTRAINT FK_EmployeeImages_Employees
        FOREIGN KEY (EmployeeId)
        REFERENCES Employees(EmployeeId)
);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='CotivityEmployeeImages')
BEGIN
CREATE TABLE CotivityEmployeeImages
(
    CotivityImageId INT IDENTITY(1,1) PRIMARY KEY,
    CotivityEmployeeId INT NOT NULL,
    CotivityImagePath VARCHAR(500) NOT NULL,

    CreatedBy VARCHAR(100) NULL,
    CreatedDate DATETIME NULL,

    LastModifiedBy VARCHAR(100) NULL,
    LastModifiedDate DATETIME NULL,

    CONSTRAINT FK_CotivityEmployeeImages_CotivityEmployees
        FOREIGN KEY (CotivityEmployeeId)
        REFERENCES CotivityEmployees(CotivityEmployeeId)
);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='DressSizes')
BEGIN
CREATE TABLE DressSizes
(
    DressId INT IDENTITY(1,1) PRIMARY KEY,
    DressName VARCHAR(100) NOT NULL,
    [Size] VARCHAR(10) NOT NULL,
    IsActive BIT NOT NULL DEFAULT(1),
    CreatedBy VARCHAR(100) NULL,
    CreatedDate DATETIME NULL,
    LastModifiedBy VARCHAR(100) NULL,
    LastModifiedDate DATETIME NULL,

    CONSTRAINT UQ_DressSizes_DressName_Size
        UNIQUE (DressName, Size)
);
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='BusinessUnits')
BEGIN

CREATE TABLE BusinessUnits
(
    BusinessUnitId INT IDENTITY(1,1) PRIMARY KEY,
    BusinessUnitName VARCHAR(150) NOT NULL,
    IsActive BIT NOT NULL DEFAULT(1),
    CreatedBy VARCHAR(100) NULL,
    CreatedDate DATETIME NULL,
    LastModifiedBy VARCHAR(100) NULL,
    LastModifiedDate DATETIME NULL,
    CONSTRAINT UQ_BusinessUnits_BusinessUnitName UNIQUE (BusinessUnitName)
);

END
GO

IF NOT EXISTS (
        SELECT 1
        FROM sys.indexes indexInfo
        WHERE indexInfo.object_id = OBJECT_ID('dbo.BusinessUnits')
            AND indexInfo.is_unique = 1
            AND indexInfo.has_filter = 0
            AND EXISTS (
                    SELECT 1
                    FROM sys.index_columns indexColumn
                    INNER JOIN sys.columns columnInfo
                            ON columnInfo.object_id = indexColumn.object_id
                         AND columnInfo.column_id = indexColumn.column_id
                    WHERE indexColumn.object_id = indexInfo.object_id
                        AND indexColumn.index_id = indexInfo.index_id
                        AND indexColumn.key_ordinal = 1
                        AND columnInfo.name = 'BusinessUnitName'
            )
            AND NOT EXISTS (
                    SELECT 1
                    FROM sys.index_columns otherKey
                    WHERE otherKey.object_id = indexInfo.object_id
                        AND otherKey.index_id = indexInfo.index_id
                        AND otherKey.key_ordinal > 1
            )
)
BEGIN
    IF EXISTS (
        SELECT BusinessUnitName
        FROM dbo.BusinessUnits
        GROUP BY BusinessUnitName
        HAVING COUNT(*) > 1
    )
    BEGIN
        THROW 51001, 'BusinessUnits contains duplicate names; resolve them before enabling name uniqueness.', 1;
    END;

    CREATE UNIQUE INDEX UQ_BusinessUnits_BusinessUnitName
        ON dbo.BusinessUnits (BusinessUnitName);
END
GO

-- Employees.BusinessUnitId: the FK is declared here, not inside the Employees
-- CREATE TABLE, because BusinessUnits is created later in this script.
IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = 'FK_Employees_BusinessUnit'
)
BEGIN
    ALTER TABLE dbo.Employees
        ADD CONSTRAINT FK_Employees_BusinessUnit
            FOREIGN KEY (BusinessUnitId)
            REFERENCES dbo.BusinessUnits(BusinessUnitId);
END
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = 'IX_Employees_BusinessUnitId'
        AND object_id = OBJECT_ID('dbo.Employees')
)
BEGIN
    CREATE INDEX IX_Employees_BusinessUnitId
        ON dbo.Employees (BusinessUnitId)
        INCLUDE (IsActive, IsDeleted);
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='EmployeeDresses')
BEGIN
CREATE TABLE EmployeeDresses
(
    EmployeeDressId INT IDENTITY(1,1) PRIMARY KEY,
    EmployeeId INT NOT NULL,
    DressId INT NOT NULL,
    CreatedBy VARCHAR(100) NULL,
    CreatedDate DATETIME NULL,
    LastModifiedBy VARCHAR(100) NULL,
    LastModifiedDate DATETIME NULL,

    CONSTRAINT FK_EmployeeDresses_Employees
        FOREIGN KEY (EmployeeId)
        REFERENCES Employees(EmployeeId),

    CONSTRAINT FK_EmployeeDresses_DressSizes
        FOREIGN KEY (DressId)
        REFERENCES DressSizes(DressId)
);
END
GO

-- /* =========================================================
--    INDEXES
-- ========================================================= */

-- IF NOT EXISTS (
--     SELECT 1 FROM sys.indexes
--     WHERE name='IX_Employees_DepartmentId'
-- )
-- CREATE INDEX IX_Employees_DepartmentId
-- ON Employees(DepartmentId);
-- GO

-- IF NOT EXISTS (
--     SELECT 1 FROM sys.indexes
--     WHERE name='IX_Employees_DesignationId'
-- )
-- CREATE INDEX IX_Employees_DesignationId
-- ON Employees(DesignationId);
-- GO

-- IF NOT EXISTS (
--     SELECT 1 FROM sys.indexes
--     WHERE name='IX_Employees_JobTypeId'
-- )
-- CREATE INDEX IX_Employees_JobTypeId
-- ON Employees(JobTypeId);
-- GO

-- IF NOT EXISTS (
--     SELECT 1 FROM sys.indexes
--     WHERE name='IX_Employees_BloodGroupId'
-- )
-- CREATE INDEX IX_Employees_BloodGroupId
-- ON Employees(BloodGroupId);
-- GO

-- IF NOT EXISTS (
--     SELECT 1 FROM sys.indexes
--     WHERE name='IX_Employees_GenderId'
-- )
-- CREATE INDEX IX_Employees_GenderId
-- ON Employees(GenderId);
-- GO

-- IF NOT EXISTS (
--     SELECT 1 FROM sys.indexes
--     WHERE name='IX_Employees_RoleId'
-- )
-- CREATE INDEX IX_Employees_RoleId
-- ON Employees(RoleId);
-- GO

-- IF NOT EXISTS (
--     SELECT 1 FROM sys.indexes
--     WHERE name='IX_Employees_TeamId'
-- )
-- CREATE INDEX IX_Employees_TeamId
-- ON Employees(TeamId);
-- GO

-- IF NOT EXISTS (
--     SELECT 1 FROM sys.indexes
--     WHERE name='IX_Employees_RecruitmentId'
-- )
-- CREATE INDEX IX_Employees_RecruitmentId
-- ON Employees(RecruitmentId);
-- GO

-- IF NOT EXISTS (
--     SELECT 1 FROM sys.indexes
--     WHERE name='IX_Employees_ReportsToEmployeeId'
-- )
-- CREATE INDEX IX_Employees_ReportsToEmployeeId
-- ON Employees(ReportsToEmployeeId);
-- GO

-- IF NOT EXISTS (
--     SELECT 1 FROM sys.indexes
--     WHERE name='IX_Employees_LeadEmployeeId'
-- )
-- CREATE INDEX IX_Employees_LeadEmployeeId
-- ON Employees(LeadEmployeeId);
-- GO

-- IF NOT EXISTS (
--     SELECT 1 FROM sys.indexes
--     WHERE name='IX_Employees_CotivityEmployeeId'
-- )
-- CREATE INDEX IX_Employees_CotivityEmployeeId
-- ON Employees(CotivityEmployeeId);
-- GO

-- IF NOT EXISTS (
--     SELECT 1 FROM sys.indexes
--     WHERE name='IX_EmployeesAddress_EmployeeId'
-- )
-- CREATE INDEX IX_EmployeesAddress_EmployeeId
-- ON EmployeesAddress(EmployeeId);
-- GO

-- IF NOT EXISTS (
--     SELECT 1 FROM sys.indexes
--     WHERE name='IX_EmployeesAddress_AddressTypeId'
-- )
-- CREATE INDEX IX_EmployeesAddress_AddressTypeId
-- ON EmployeesAddress(AddressTypeId);
-- GO

-- IF NOT EXISTS (
--     SELECT 1 FROM sys.indexes
--     WHERE name='IX_EmployeeSkills_EmployeeId'
-- )
-- CREATE INDEX IX_EmployeeSkills_EmployeeId
-- ON EmployeeSkills(EmployeeId);
-- GO

-- IF NOT EXISTS (
--     SELECT 1 FROM sys.indexes
--     WHERE name='IX_EmployeeSkills_SkillId'
-- )
-- CREATE INDEX IX_EmployeeSkills_SkillId
-- ON EmployeeSkills(SkillId);
-- GO

-- IF NOT EXISTS (
--     SELECT 1 FROM sys.indexes
--     WHERE name='IX_CotivityEmployees_DepartmentId'
-- )
-- CREATE INDEX IX_CotivityEmployees_DepartmentId
-- ON CotivityEmployees(CotivityDepartmentId);
-- GO

-- IF NOT EXISTS (
--     SELECT 1 FROM sys.indexes
--     WHERE name='IX_CotivityEmployees_DesignationId'
-- )
-- CREATE INDEX IX_CotivityEmployees_DesignationId
-- ON CotivityEmployees(CotivityDepartmentDesignationId);
-- GO

-- IF NOT EXISTS (
--     SELECT 1 FROM sys.indexes
--     WHERE name='IX_CotivityEmployees_ReportsTo'
-- )
-- CREATE INDEX IX_CotivityEmployees_ReportsTo
-- ON CotivityEmployees(ReportsToCotivityEmployeeId);
-- GO
