/* =========================================================
   TeamHub - SEED DATA
   Standalone seed script for ALL tables.
   Safe to run against an existing (empty or partially-filled) TeamHub DB:
     - Master/reference rows are inserted only if the unique name doesn't exist.
     - Transactional rows (addresses, skills, releases, ...) are inserted only
       when their target table is still empty, so re-running won't duplicate.
   Run AFTER databasesesign.sql and authentication-schema.sql.
   Apply with:  sqlcmd -S ".\SQLEXPRESS" -d TeamHub -E -i seed-data.sql
   ========================================================= */
USE [TeamHub];
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* =========================================================
   1) MASTER / REFERENCE TABLES  (idempotent by unique key)
   ========================================================= */

-- Departments
INSERT INTO dbo.Departments (DepartmentName, IsActive, CreatedBy, CreatedDate)
SELECT v.DepartmentName, 1, 'seed', SYSUTCDATETIME()
FROM (VALUES
    ('Engineering'), ('Human Resources'), ('Finance'), ('Sales'),
    ('Marketing'), ('Operations'), ('IT Support'), ('Quality Assurance')
) v(DepartmentName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Departments d WHERE d.DepartmentName = v.DepartmentName);
GO

-- CotivityDepartments
INSERT INTO dbo.CotivityDepartments (CotivityDepartmentName, IsActive, CreatedBy, CreatedDate)
SELECT v.CotivityDepartmentName, 1, 'seed', SYSUTCDATETIME()
FROM (VALUES
    ('Product'), ('Delivery'), ('Platform'), ('Solutions')
) v(CotivityDepartmentName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.CotivityDepartments c WHERE c.CotivityDepartmentName = v.CotivityDepartmentName);
GO

-- Designations (with Rank)
INSERT INTO dbo.Designations (DesignationName, [Rank], IsActive, CreatedBy, CreatedDate)
SELECT v.DesignationName, v.[Rank], 1, 'seed', SYSUTCDATETIME()
FROM (VALUES
    ('Chief Executive Officer', 1),  ('Chief Technology Officer', 2),
    ('VP Engineering',           3),  ('Engineering Manager',     4),
    ('Senior Developer',         5),  ('Developer',               6),
    ('QA Lead',                  7),  ('QA Engineer',             8),
    ('HR Manager',               9),  ('HR Executive',           10),
    ('Accountant',              11),  ('Sales Manager',          12),
    ('Support Engineer',        13),  ('Intern',                 14)
) v(DesignationName, [Rank])
WHERE NOT EXISTS (SELECT 1 FROM dbo.Designations d WHERE d.DesignationName = v.DesignationName);
GO

-- CotivityDesignations (with CotivityRank)
INSERT INTO dbo.CotivityDesignations (CotivityDesignationName, CotivityRank, IsActive, CreatedBy, CreatedDate)
SELECT v.CotivityDesignationName, v.CotivityRank, 1, 'seed', SYSUTCDATETIME()
FROM (VALUES
    ('Principal Architect', 1), ('Delivery Head', 2),
    ('Solution Lead',       3), ('Consultant',    4)
) v(CotivityDesignationName, CotivityRank)
WHERE NOT EXISTS (SELECT 1 FROM dbo.CotivityDesignations c WHERE c.CotivityDesignationName = v.CotivityDesignationName);
GO

-- JobTypes
INSERT INTO dbo.JobTypes (JobTypeName, IsActive, CreatedBy, CreatedDate)
SELECT v.JobTypeName, 1, 'seed', SYSUTCDATETIME()
FROM (VALUES ('Full-Time'), ('Part-Time'), ('Contract'), ('Internship'), ('Remote')) v(JobTypeName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.JobTypes j WHERE j.JobTypeName = v.JobTypeName);
GO

-- BloodGroups
INSERT INTO dbo.BloodGroups (BloodGroupName, IsActive, CreatedBy, CreatedDate)
SELECT v.BloodGroupName, 1, 'seed', SYSUTCDATETIME()
FROM (VALUES ('A+'), ('A-'), ('B+'), ('B-'), ('O+'), ('O-'), ('AB+'), ('AB-')) v(BloodGroupName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.BloodGroups b WHERE b.BloodGroupName = v.BloodGroupName);
GO

-- Gender
INSERT INTO dbo.Gender (GenderName, IsActive, CreatedBy, CreatedDate)
SELECT v.GenderName, 1, 'seed', SYSUTCDATETIME()
FROM (VALUES ('Male'), ('Female'), ('Other')) v(GenderName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Gender g WHERE g.GenderName = v.GenderName);
GO

-- AddressType
INSERT INTO dbo.AddressType (AddressTypeName, IsActive, CreatedBy, CreatedDate)
SELECT v.AddressTypeName, 1, 'seed', SYSUTCDATETIME()
FROM (VALUES ('Permanent'), ('Current'), ('Mailing')) v(AddressTypeName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.AddressType a WHERE a.AddressTypeName = v.AddressTypeName);
GO

-- Roles  (Admin / NormalUser are required by the auth flow)
INSERT INTO dbo.Roles (RoleName, IsActive, CreatedBy, CreatedDate)
SELECT v.RoleName, 1, 'seed', SYSUTCDATETIME()
FROM (VALUES ('Admin'), ('NormalUser'), ('Manager'), ('HR')) v(RoleName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Roles r WHERE r.RoleName = v.RoleName);
GO

-- Teams
INSERT INTO dbo.Teams (TeamName, IsActive, CreatedBy, CreatedDate)
SELECT v.TeamName, 1, 'seed', SYSUTCDATETIME()
FROM (VALUES ('Core Platform'), ('Payments'), ('Web'), ('Mobile'), ('QA'), ('DevOps'), ('People Ops')) v(TeamName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Teams t WHERE t.TeamName = v.TeamName);
GO

-- Skills
INSERT INTO dbo.Skills (SkillName, IsActive, CreatedBy, CreatedDate)
SELECT v.SkillName, 1, 'seed', SYSUTCDATETIME()
FROM (VALUES
    ('C#'), ('.NET'), ('SQL'), ('Angular'), ('TypeScript'), ('JavaScript'),
    ('Azure'), ('Docker'), ('Kubernetes'), ('Python'), ('Leadership'), ('Communication')
) v(SkillName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Skills s WHERE s.SkillName = v.SkillName);
GO

-- Recruitments
INSERT INTO dbo.Recruitments (RecruitmentName, IsActive, CreatedBy, CreatedDate)
SELECT v.RecruitmentName, 1, 'seed', SYSUTCDATETIME()
FROM (VALUES ('Referral'), ('LinkedIn'), ('Job Portal'), ('Campus'), ('Direct')) v(RecruitmentName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Recruitments r WHERE r.RecruitmentName = v.RecruitmentName);
GO

-- BusinessUnits
INSERT INTO dbo.BusinessUnits (BusinessUnitName, IsActive, CreatedBy, CreatedDate)
SELECT v.BusinessUnitName, 1, 'seed', SYSUTCDATETIME()
FROM (VALUES ('Enterprise'), ('Retail'), ('Government'), ('Startup')) v(BusinessUnitName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.BusinessUnits b WHERE b.BusinessUnitName = v.BusinessUnitName);
GO

-- DressSizes (DressName + Size unique)
INSERT INTO dbo.DressSizes (DressName, [Size], IsActive, CreatedBy, CreatedDate)
SELECT v.DressName, v.[Size], 1, 'seed', SYSUTCDATETIME()
FROM (VALUES
    ('Shirt', 'S'), ('Shirt', 'M'), ('Shirt', 'L'), ('Shirt', 'XL'),
    ('Trouser', '30'), ('Trouser', '32'), ('Trouser', '34'),
    ('Jersey', 'M'), ('Jersey', 'L'), ('Jersey', 'XL')
) v(DressName, [Size])
WHERE NOT EXISTS (SELECT 1 FROM dbo.DressSizes ds WHERE ds.DressName = v.DressName AND ds.[Size] = v.[Size]);
GO

/* =========================================================
   2) COTIVITY EMPLOYEES  (self-hierarchy set after insert)
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM dbo.CotivityEmployees)
BEGIN
    INSERT INTO dbo.CotivityEmployees
        (CotivityEmployeeName, CotivityDepartmentId, CotivityDepartmentDesignationId,
         ReportsToCotivityEmployeeId, IsActive, CreatedBy, CreatedDate)
    SELECT
        s.CotivityEmployeeName,
        cd.CotivityDepartmentId,
        cdes.CotivityDesignationId,
        NULL, 1, 'seed', SYSUTCDATETIME()
    FROM (VALUES
        ('Alice Turner', 'Product',   'Principal Architect'),
        ('Bob Cox',      'Delivery',  'Delivery Head'),
        ('Cara Diaz',    'Platform',  'Solution Lead'),
        ('Dan Frost',    'Solutions', 'Consultant')
    ) s(CotivityEmployeeName, DepartmentName, DesignationName)
    INNER JOIN dbo.CotivityDepartments   cd  ON cd.CotivityDepartmentName   = s.DepartmentName
    INNER JOIN dbo.CotivityDesignations  cdes ON cdes.CotivityDesignationName = s.DesignationName;

    -- hierarchy
    UPDATE ce SET ReportsToCotivityEmployeeId = m.CotivityEmployeeId
    FROM dbo.CotivityEmployees ce
    INNER JOIN (VALUES
        ('Cara Diaz', 'Alice Turner'),
        ('Dan Frost', 'Bob Cox')
    ) s(employeeName, managerName) ON ce.CotivityEmployeeName = s.employeeName
    INNER JOIN dbo.CotivityEmployees m ON m.CotivityEmployeeName = s.managerName;
END
GO

/* =========================================================
   3) EMPLOYEES  (core hierarchy set after insert)
      Emails / ENumber are unique keys used for idempotency.
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM dbo.Employees)
BEGIN
    INSERT INTO dbo.Employees
        (ENumber, FirstName, MiddleName, LastName, JoinedDate, JoinedDateBS,
         Email, AlternateEmail, DesignationId, DepartmentId, JobTypeId, BloodGroupId, GenderId,
         ReportsToEmployeeId, LeadEmployeeId, StationNumber, RecruitmentId, ExtensionNumber,
         CitizenshipNumber, DateOfBirth, DateOfBirthBS, PhoneNumber, MobileNumber,
         EmergencyContactNumber, EmergencyContactPerson, FatherName, MotherName, SpouseName,
         PANNumber, LockerNumber, RoleId, TeamId, CotivityEmployeeId, IsActive, CreatedBy, CreatedDate)
    SELECT
        s.ENumber, s.FirstName, s.MiddleName, s.LastName, s.JoinedDate, s.JoinedDateBS,
        s.Email, NULL, des.DesignationId, dep.DepartmentId, jt.JobTypeId, bg.BloodGroupId, g.GenderId,
        NULL, NULL, s.StationNumber, rc.RecruitmentId, s.ExtensionNumber,
        s.CitizenshipNumber, s.DateOfBirth, s.DateOfBirthBS, s.PhoneNumber, s.MobileNumber,
        s.EmergencyContactNumber, s.EmergencyContactPerson, s.FatherName, s.MotherName, s.SpouseName,
        s.PANNumber, s.LockerNumber, r.RoleId, t.TeamId, ce.CotivityEmployeeId, 1, NULL, SYSUTCDATETIME()
    FROM (VALUES
        -- ENumber, First, Middle, Last, JoinedDate, BS, Email, Designation, Department, JobType, Blood, Gender, Station, Ext, Citizenship, DOB, DOB-BS, Phone, Mobile, Emerg#, EmergPerson, Father, Mother, Spouse, PAN, Locker, Role, Team, Recruitment, CotivityName
        (1001,'David',   NULL,   'Chen',     '2015-01-10','2071-09-26','david.chen@teamhub.com','Chief Executive Officer','Engineering','Full-Time','O+','Male','ST-01',201,'NPC-100001','1980-03-01','2036-11-17','01-4000001','9800000001','9800001001','Mary Chen','John Chen','Elena Chen','NULL','PAN100001A','LK-01','Admin','Core Platform','Direct','Alice Turner'),
        (1002,'Sarah',   NULL,   'Wilson',   '2016-05-02','2072-01-19','sarah.wilson@teamhub.com','Chief Technology Officer','Engineering','Full-Time','A+','Female','ST-02',202,'NPC-100002','1982-07-14','2039-04-01','01-4000002','9800000002','9800001002','Tom Wilson','Robert Wilson','Anna Wilson','NULL','PAN100002A','LK-02','Admin','Web','Referral','Bob Cox'),
        (1003,'Michael', 'A',    'Brown',    '2017-08-15','2074-05-01','michael.brown@teamhub.com','VP Engineering','Engineering','Full-Time','B+','Male','ST-03',203,'NPC-100003','1983-11-22','2040-08-06','01-4000003','9800000003','9800001003','Grace Brown','Paul Brown','Nina Brown','NULL','PAN100003A','LK-03','Manager','Core Platform','LinkedIn','Cara Diaz'),
        (1004,'Emily',   NULL,   'Davis',    '2018-03-01','2074-11-17','emily.davis@teamhub.com','Engineering Manager','Engineering','Full-Time','AB+','Female','ST-04',204,'NPC-100004','1986-05-30','2043-02-15','01-4000004','9800000004','9800001004','Kevin Davis','Mark Davis',NULL,'NULL','PAN100004A','LK-04','Manager','Payments','Referral',NULL),
        (1005,'James',   NULL,   'Lee',      '2019-06-11','2076-03-01','james.lee@teamhub.com','Senior Developer','Engineering','Full-Time','O-','Male','ST-05',205,'NPC-100005','1990-09-09','2047-05-26','01-4000005','9800000005','9800001005','Amy Lee','Daniel Lee',NULL,'NULL','PAN100005A','LK-05','NormalUser','Web','Job Portal',NULL),
        (1006,'Priya',   NULL,   'Sharma',   '2020-02-20','2076-11-07','priya.sharma@teamhub.com','Developer','Engineering','Full-Time','A-','Female','ST-06',206,'NPC-100006','1995-12-05','2052-08-21','01-4000006','9800000006','9800001006','Raj Sharma','Vikram Sharma',NULL,'NULL','PAN100006A','LK-06','NormalUser','Web','Campus',NULL),
        (1007,'Daniel',  NULL,   'Garcia',   '2018-09-03','2075-05-19','daniel.garcia@teamhub.com','QA Lead','Quality Assurance','Full-Time','B-','Male','ST-07',207,'NPC-100007','1987-01-18','2043-10-02','01-4000007','9800000007','9800001007','Sofia Garcia','Luis Garcia','Ivy Garcia','NULL','PAN100007A','LK-07','Manager','QA','Direct',NULL),
        (1008,'Lisa',    NULL,   'Anderson', '2021-04-12','2078-01-01','lisa.anderson@teamhub.com','QA Engineer','Quality Assurance','Full-Time','O+','Female','ST-08',208,'NPC-100008','1993-06-27','2050-03-13','01-4000008','9800000008','9800001008','Carl Anderson','Peter Anderson',NULL,'NULL','PAN100008A','LK-08','NormalUser','QA','LinkedIn',NULL),
        (1009,'Robert',  NULL,   'Martinez', '2017-01-25','2073-10-12','robert.martinez@teamhub.com','HR Manager','Human Resources','Full-Time','A+','Male','ST-09',209,'NPC-100009','1984-08-08','2041-05-05','01-4000009','9800000009','9800001009','Rosa Martinez','Carlos Martinez','Mia Martinez','NULL','PAN100009A','LK-09','HR','People Ops','Referral',NULL),
        (1010,'Linda',   NULL,   'Wong',     '2019-11-05','2076-07-20','linda.wong@teamhub.com','Accountant','Finance','Full-Time','AB-','Female','ST-10',210,'NPC-100010','1989-02-14','2045-11-03','01-4000010','9800000010','9800001010','Ken Wong','George Wong','Sam Wong','NULL','PAN100010A','LK-10','NormalUser','People Ops','Job Portal',NULL)
    ) s(ENumber, FirstName, MiddleName, LastName, JoinedDate, JoinedDateBS, Email,
       DesignationName, DepartmentName, JobTypeName, BloodGroupName, GenderName,
       StationNumber, ExtensionNumber, CitizenshipNumber, DateOfBirth, DateOfBirthBS,
       PhoneNumber, MobileNumber, EmergencyContactNumber, EmergencyContactPerson,
       FatherName, MotherName, SpouseName, PANNumber, LockerNumber,
       RoleName, TeamName, RecruitmentName, CotivityEmployeeName)
    INNER JOIN dbo.Designations  des ON des.DesignationName = s.DesignationName
    INNER JOIN dbo.Departments   dep ON dep.DepartmentName  = s.DepartmentName
    INNER JOIN dbo.JobTypes      jt  ON jt.JobTypeName      = s.JobTypeName
    INNER JOIN dbo.BloodGroups   bg  ON bg.BloodGroupName   = s.BloodGroupName
    INNER JOIN dbo.Gender        g   ON g.GenderName        = s.GenderName
    INNER JOIN dbo.Roles         r   ON r.RoleName          = s.RoleName
    INNER JOIN dbo.Teams         t   ON t.TeamName          = s.TeamName
    LEFT  JOIN dbo.Recruitments  rc  ON rc.RecruitmentName  = s.RecruitmentName
    LEFT  JOIN dbo.CotivityEmployees ce ON ce.CotivityEmployeeName = s.CotivityEmployeeName;

    -- reporting hierarchy (ReportsTo + Lead) now that all employees exist
    UPDATE e SET ReportsToEmployeeId = mgr.EmployeeId
    FROM dbo.Employees e
    INNER JOIN (VALUES
        ('sarah.wilson@teamhub.com',   'david.chen@teamhub.com'),
        ('michael.brown@teamhub.com',  'sarah.wilson@teamhub.com'),
        ('emily.davis@teamhub.com',    'michael.brown@teamhub.com'),
        ('james.lee@teamhub.com',      'emily.davis@teamhub.com'),
        ('priya.sharma@teamhub.com',   'emily.davis@teamhub.com'),
        ('daniel.garcia@teamhub.com',  'sarah.wilson@teamhub.com'),
        ('lisa.anderson@teamhub.com',  'daniel.garcia@teamhub.com'),
        ('robert.martinez@teamhub.com','david.chen@teamhub.com'),
        ('linda.wong@teamhub.com',     'david.chen@teamhub.com')
    ) s(Email, ManagerEmail) ON e.Email = s.Email
    INNER JOIN dbo.Employees mgr ON mgr.Email = s.ManagerEmail;

    UPDATE e SET LeadEmployeeId = lead.EmployeeId
    FROM dbo.Employees e
    INNER JOIN (VALUES
        ('james.lee@teamhub.com',   'emily.davis@teamhub.com'),
        ('priya.sharma@teamhub.com','emily.davis@teamhub.com'),
        ('lisa.anderson@teamhub.com','daniel.garcia@teamhub.com')
    ) s(Email, LeadEmail) ON e.Email = s.Email
    INNER JOIN dbo.Employees lead ON lead.Email = s.LeadEmail;
END
GO

/* =========================================================
   4) EMPLOYEE ADDRESSES
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM dbo.EmployeeAddresses)
BEGIN
    INSERT INTO dbo.EmployeeAddresses
        (EmployeeId, AddressTypeId, AddressLine1, AddressLine2, City, [State],
         Latitude, Longitude, IsPrimary, CreatedBy, CreatedDate)
    SELECT e.EmployeeId, at.AddressTypeId, s.AddressLine1, s.AddressLine2, s.City, s.[State],
           s.Latitude, s.Longitude, s.IsPrimary, 'seed', SYSUTCDATETIME()
    FROM (VALUES
        ('david.chen@teamhub.com','Permanent','1 Lakeview Road',        'Baluwatar',  'Kathmandu',  'Bagmati', 27.71720000, 85.32400000, 1),
        ('david.chen@teamhub.com','Current',  '2 Hill Avenue',          'Kamalokhi',  'Kathmandu',  'Bagmati', 27.70150000, 85.31800000, 0),
        ('sarah.wilson@teamhub.com','Permanent','5 Ring Road',          '',           'Lalitpur',   'Bagmati', 27.67150000, 85.32500000, 1),
        ('michael.brown@teamhub.com','Permanent','8 New Road',          '',           'Bhaktapur',  'Bagmati', 27.67150000, 85.42900000, 1),
        ('priya.sharma@teamhub.com','Permanent','12 Pulchowk',          '',           'Lalitpur',   'Bagmati', 27.66050000, 85.31400000, 1),
        ('robert.martinez@teamhub.com','Permanent','3 Durbar Marg',     '',           'Kathmandu',  'Bagmati', 27.71000000, 85.33700000, 1)
    ) s(Email, AddressTypeName, AddressLine1, AddressLine2, City, [State], Latitude, Longitude, IsPrimary)
    INNER JOIN dbo.Employees   e  ON e.Email = s.Email
    INNER JOIN dbo.AddressType at ON at.AddressTypeName = s.AddressTypeName;
END
GO

/* =========================================================
   5) EMPLOYEE SKILLS  (SkillLevel 1-5)
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM dbo.EmployeeSkills)
BEGIN
    INSERT INTO dbo.EmployeeSkills
        (EmployeeId, SkillId, SkillLevel, IsPrimary, CreatedBy, CreatedDate)
    SELECT e.EmployeeId, sk.SkillId, s.SkillLevel, s.IsPrimary, 'seed', SYSUTCDATETIME()
    FROM (VALUES
        ('david.chen@teamhub.com',   'Leadership',   5, 1),
        ('david.chen@teamhub.com',   'Communication',4, 0),
        ('sarah.wilson@teamhub.com', '.NET',         5, 1),
        ('sarah.wilson@teamhub.com', 'Azure',        4, 0),
        ('james.lee@teamhub.com',    'C#',           5, 1),
        ('james.lee@teamhub.com',    'SQL',          4, 0),
        ('priya.sharma@teamhub.com', 'Angular',      4, 1),
        ('priya.sharma@teamhub.com', 'TypeScript',   4, 0),
        ('daniel.garcia@teamhub.com','Leadership',   4, 1),
        ('lisa.anderson@teamhub.com','Communication',3, 1)
    ) s(Email, SkillName, SkillLevel, IsPrimary)
    INNER JOIN dbo.Employees e  ON e.Email = s.Email
    INNER JOIN dbo.Skills    sk ON sk.SkillName = s.SkillName;
END
GO

/* =========================================================
   6) EMPLOYEE IMAGES  (placeholder paths under wwwroot)
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM dbo.EmployeeImages)
BEGIN
    INSERT INTO dbo.EmployeeImages (EmployeeId, ImagePath, CreatedBy, CreatedDate)
    SELECT e.EmployeeId, '/wwwroot/InfiniteImages/' + CAST(e.ENumber AS VARCHAR(10)) + '.png', 'seed', SYSUTCDATETIME()
    FROM dbo.Employees e
    WHERE e.Email IN ('david.chen@teamhub.com','sarah.wilson@teamhub.com','priya.sharma@teamhub.com');
END
GO

-- CotivityEmployeeImages
IF NOT EXISTS (SELECT 1 FROM dbo.CotivityEmployeeImages)
BEGIN
    INSERT INTO dbo.CotivityEmployeeImages (CotivityEmployeeId, CotivityImagePath, CreatedBy, CreatedDate)
    SELECT ce.CotivityEmployeeId, '/wwwroot/CotivityImages/' + ce.CotivityEmployeeName + '.png', 'seed', SYSUTCDATETIME()
    FROM dbo.CotivityEmployees ce
    WHERE ce.CotivityEmployeeName IN ('Alice Turner','Bob Cox');
END
GO

/* =========================================================
   7) EMPLOYEE DRESSES
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM dbo.EmployeeDresses)
BEGIN
    INSERT INTO dbo.EmployeeDresses (EmployeeId, DressId, CreatedBy, CreatedDate)
    SELECT e.EmployeeId, ds.DressId, 'seed', SYSUTCDATETIME()
    FROM (VALUES
        ('david.chen@teamhub.com',  'Shirt', 'L'),
        ('sarah.wilson@teamhub.com','Shirt', 'M'),
        ('priya.sharma@teamhub.com','Jersey','M')
    ) s(Email, DressName, DressSize)
    INNER JOIN dbo.Employees  e  ON e.Email = s.Email
    INNER JOIN dbo.DressSizes ds ON ds.DressName = s.DressName AND ds.[Size] = s.DressSize;
END
GO

/* =========================================================
   8) EMPLOYEE RELEASES (resignations)
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM dbo.EmployeeReleases)
BEGIN
    INSERT INTO dbo.EmployeeReleases
        (EmployeeId, ReleaseType, ResignationDate, NoticeDate, LastWorkingDate,
         NoticePeriodDays, ServedNoticeDays, Reason, EarlyReleaseReason, Remarks,
         KnowledgeTransferCompleted, AssetsReturned, ExitInterviewCompleted,
         SettlementAmount, RelievingLetterIssued, ExperienceLetterIssued,
         Status, ApprovedBy, ApprovedDate, CreatedBy, CreatedDate)
    SELECT e.EmployeeId, s.ReleaseType, s.ResignationDate, s.NoticeDate, s.LastWorkingDate,
         s.NoticePeriodDays, s.ServedNoticeDays, s.Reason, s.EarlyReleaseReason, s.Remarks,
         s.KnowledgeTransferCompleted, s.AssetsReturned, s.ExitInterviewCompleted,
         s.SettlementAmount, s.RelievingLetterIssued, s.ExperienceLetterIssued,
         s.Status, s.ApprovedBy, s.ApprovedDate, 'seed', SYSUTCDATETIME()
    FROM (VALUES
        ('james.lee@teamhub.com','Resignation','2025-06-01','2025-06-01','2025-07-31',
            60, 60, 'Seeking growth opportunities outside the organization', NULL,
            'Completed knowledge handover', 1, 1, 1, 150000.00, 1, 1, 'Released', 'David Chen', '2025-07-31 10:00:00'),
        ('lisa.anderson@teamhub.com','Resignation','2025-08-15','2025-08-15','2025-09-01',
            30, 17, 'Personal reasons requiring relocation to another city',
            'Family relocation moved up; could not serve full notice', 'Partial handover done',
            1, 0, 0, 50000.00, 0, 0, 'Submitted', NULL, NULL)
    ) s(Email, ReleaseType, ResignationDate, NoticeDate, LastWorkingDate,
        NoticePeriodDays, ServedNoticeDays, Reason, EarlyReleaseReason, Remarks,
        KnowledgeTransferCompleted, AssetsReturned, ExitInterviewCompleted,
        SettlementAmount, RelievingLetterIssued, ExperienceLetterIssued, Status, ApprovedBy, ApprovedDate)
    INNER JOIN dbo.Employees e ON e.Email = s.Email;
END
GO

/* =========================================================
   9) EMPLOYEE TERMINATIONS  (table is dbo.EmployeeTerminates)
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM dbo.EmployeeTerminates)
BEGIN
    INSERT INTO dbo.EmployeeTerminates
        (EmployeeId, TerminationType, EffectiveDate, InvestigationRequired, InvestigationSummary,
         WarningCount, LastWarningDate, Reason, Remarks, AssetRecovered, SystemAccessRevoked,
         ExitInterviewCompleted, SettlementAmount, EligibleForRehire, TerminationLetterIssued,
         TerminationLetterDate, Status, ApprovedBy, ApprovedDate, CreatedBy, CreatedDate)
    SELECT e.EmployeeId, s.TerminationType, s.EffectiveDate, s.InvestigationRequired, s.InvestigationSummary,
         s.WarningCount, s.LastWarningDate, s.Reason, s.Remarks, s.AssetRecovered, s.SystemAccessRevoked,
         s.ExitInterviewCompleted, s.SettlementAmount, s.EligibleForRehire, s.TerminationLetterIssued,
         s.TerminationLetterDate, s.Status, s.ApprovedBy, s.ApprovedDate, 'seed', SYSUTCDATETIME()
    FROM (VALUES
        ('linda.wong@teamhub.com','Policy Violation','2025-09-30', 1,
            'Verified expense-report discrepancies during internal review',
            2, '2025-08-01', 'Repeated violation of expense submission policy',
            'Settled final dues after recovery', 1, 1, 1, 80000.00, 0, 1, '2025-09-30',
            'Approved', 'Robert Martinez', '2025-09-30 09:00:00')
    ) s(Email, TerminationType, EffectiveDate, InvestigationRequired, InvestigationSummary,
        WarningCount, LastWarningDate, Reason, Remarks, AssetRecovered, SystemAccessRevoked,
        ExitInterviewCompleted, SettlementAmount, EligibleForRehire, TerminationLetterIssued,
        TerminationLetterDate, Status, ApprovedBy, ApprovedDate)
    INNER JOIN dbo.Employees e ON e.Email = s.Email;
END
GO

/* =========================================================
   10) AUTH USERS  (no passwords - Microsoft Entra ID is the sign-in mechanism;
        the Development bypass signs these accounts in passwordlessly.)
   ========================================================= */
IF NOT EXISTS (SELECT 1 FROM dbo.Users)
BEGIN
    INSERT INTO dbo.Users (EmployeeId, Username, Email, IsActive, CreatedDate)
    SELECT e.EmployeeId, s.Username, e.Email, 1, SYSUTCDATETIME()
    FROM (VALUES
        ('david.chen@teamhub.com', N'Admin'),
        ('priya.sharma@teamhub.com', N'NormalUser')
    ) s(Email, Username)
    INNER JOIN dbo.Employees e ON e.Email = s.Email
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Users u WHERE u.EmployeeId = e.EmployeeId OR u.Username = s.Username);
END
GO

/* Ensure the seeded sign-in accounts carry their roles in the authoritative
   UserRoles junction (the legacy Employees.RoleId column has been removed). */
IF OBJECT_ID(N'dbo.UserRoles', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.UserRoles (UserId, RoleId, CreatedBy, CreatedDate)
    SELECT u.UserId, r.RoleId, 'seed', SYSUTCDATETIME()
    FROM (VALUES (N'Admin', N'Admin'), (N'NormalUser', N'NormalUser')) AS seed(Username, RoleName)
    INNER JOIN dbo.Users u ON u.Username = seed.Username
    INNER JOIN dbo.Roles r ON r.RoleName = seed.RoleName AND r.IsActive = 1
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.UserRoles ur WHERE ur.UserId = u.UserId AND ur.RoleId = r.RoleId
    );
END
GO

/* =========================================================
   11) ROLE PERMISSION GRANTS
   Seeds the non-admin demo roles so the resignation self-service and the
   termination / offboarding paths work out of the box instead of having to be
   ticked by hand in the Role Permission Matrix. Idempotent: matched on natural
   keys (RoleName + PermissionCode) and only missing grants are inserted.

   Only codes that a backend policy actually enforces are listed here
   (RbacPolicies.PolicyPermissions). The catalog's inert codes - people.view,
   people.terminate, people.hierarchy.view, resignations.manage, master.view,
   master.manage - are deliberately NOT granted, so the matrix cannot show a
   tick that changes nothing. Admin needs no rows: rbac-schema.sql mirrors the
   whole catalog onto it.

   Naming note: both "Add" buttons have their own code - resignations.add
   (self-service; the create action checks it in code at
   EmployeeReleasesController.cs:110) and terminations.add
   (EmployeeTerminationsController.cs:85, mirrored by terminationAddGuard).
   Editing or transitioning an existing termination stays on terminations.manage,
   so a role can be allowed to file a record without being allowed to rewrite the
   queue.
========================================================= */
IF OBJECT_ID(N'dbo.RolePermissions', N'U') IS NOT NULL AND OBJECT_ID(N'dbo.Permissions', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.RolePermissions (RoleId, PermissionId, CreatedBy, CreatedDate)
    SELECT r.RoleId, p.PermissionId, 'seed', SYSUTCDATETIME()
    FROM (VALUES
        -- NormalUser: a plain employee. "People" needs no code (authenticatedGuard only),
        -- so this single grant is what makes the "Apply Resignation" entry appear.
        (N'NormalUser', N'resignations.add'),

        -- Manager: runs the resignation queue for their team (no termination writes).
        (N'Manager', N'resignations.view'),
        (N'Manager', N'resignations.add'),
        (N'Manager', N'resignations.action.submit'),
        (N'Manager', N'resignations.action.sendforapproval'),
        (N'Manager', N'resignations.action.approve'),
        (N'Manager', N'resignations.action.reject'),
        (N'Manager', N'resignations.action.cancel'),
        (N'Manager', N'master.team'),

        -- HR: people master data + offboarding (the "Add Termination" capability) and the
        -- operational resignation steps; approval/rejection stay with the Manager.
        (N'HR', N'people.create'),
        (N'HR', N'people.update'),
        (N'HR', N'people.release'),
        (N'HR', N'master.recruitment'),
        (N'HR', N'resignations.view'),
        (N'HR', N'resignations.add'),
        (N'HR', N'resignations.action.submit'),
        (N'HR', N'resignations.action.sendforapproval'),
        (N'HR', N'resignations.action.cancel'),
        (N'HR', N'resignations.action.exitchecklist'),
        (N'HR', N'resignations.action.release'),
        (N'HR', N'terminations.view'),
        (N'HR', N'terminations.add'),
        (N'HR', N'terminations.manage')
    ) g(RoleName, PermissionCode)
    INNER JOIN dbo.Roles r ON r.RoleName = g.RoleName AND r.IsActive = 1
    INNER JOIN dbo.Permissions p ON p.PermissionCode = g.PermissionCode AND p.IsActive = 1
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.RolePermissions rp
        WHERE rp.RoleId = r.RoleId AND rp.PermissionId = p.PermissionId
    );
END
GO

PRINT 'TeamHub seed data applied.';
GO
