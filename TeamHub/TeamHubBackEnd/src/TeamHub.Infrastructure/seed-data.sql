/* =========================================================
   TeamHub - SEED DATA
   Standalone seed script for ALL tables.

   Section 0 is DESTRUCTIVE: with @ResetMasters = 1 it wipes every
   master/reference row together with the employee domain that points at
   it; section 1 then rebuilds the master set from scratch with clean
   identities (1..N in insert order). With @ResetMasters = 0 nothing is
   deleted and section 1 falls back to idempotent-by-name inserts.
   Permissions is never deleted: the Role -> Permission grants are
   snapshotted by name before the reset and re-applied at the end of
   section 1, so the Role Permission Matrix survives the new RoleIds.

   Employee-domain rows are REBUILT: section 3 wipes every employee and its
   dependent rows, then seeds 300 employees (toggle @ResetEmployees there).
   Sections 4-10 are inserted only when their target table is still empty, so
   re-running the script does not duplicate anything.
   Run AFTER databasesesign.sql and authentication-schema.sql.
   Apply with:  sqlcmd -S ".\SQLEXPRESS" -d TeamHub -E -C -b -i seed-data.sql
   ========================================================= */
USE [TeamHub];
GO

/* Filtered indexes (EmployeeHierarchy.sql) and computed objects require these
   options. sqlcmd connects with QUOTED_IDENTIFIER OFF by default, which makes
   every DELETE/INSERT against dbo.Employees fail with Msg 1934. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* =========================================================
   0) DESTRUCTIVE MASTER RESET  (toggle @ResetMasters)

      Empties every master/reference table and the employee domain that
      references it, so section 1 can rebuild the whole master set with
      clean identities (1..N in insert order) instead of inheriting the
      drifting ids left behind by earlier runs and manual UI edits.

      Permissions is never touched. The Role -> Permission grants are
      snapshotted by name first, because a rebuilt Roles table gets new
      RoleIds, and re-applied at the end of section 1.

      Purge order follows the FK graph (children first):
        RefreshTokens -> UserLoginHistory -> UserRoles -> RolePermissions
        -> Users -> Successions -> EmployeeReleases -> EmployeeTerminates
        -> EmployeeAddresses -> EmployeeSkills -> EmployeeImages
        -> EmployeeDresses -> Employees -> CotivityEmployeeImages
        -> CotivityEmployees -> the 14 master tables
   ========================================================= */
DECLARE @ResetMasters BIT = 1;   -- 1 = wipe + rebuild masters, 0 = keep existing

IF @ResetMasters = 1
BEGIN
    -- the RBAC matrix must survive the RoleId churn, so key it by name
    CREATE TABLE #RoleGrant
    (
        RoleName       VARCHAR(100)  NOT NULL,
        PermissionCode NVARCHAR(200) NOT NULL
    );

    INSERT INTO #RoleGrant (RoleName, PermissionCode)
    SELECT r.RoleName, p.PermissionCode
    FROM dbo.RolePermissions rp
    INNER JOIN dbo.Roles r       ON r.RoleId = rp.RoleId
    INNER JOIN dbo.Permissions p ON p.PermissionId = rp.PermissionId;

    BEGIN TRANSACTION;

    DELETE FROM dbo.RefreshTokens;
    DELETE FROM dbo.UserLoginHistory;
    DELETE FROM dbo.UserRoles;
    DELETE FROM dbo.RolePermissions;
    DELETE FROM dbo.Users;

    DELETE FROM dbo.Successions;
    DELETE FROM dbo.EmployeeReleases;
    DELETE FROM dbo.EmployeeTerminates;
    DELETE FROM dbo.EmployeeAddresses;
    DELETE FROM dbo.EmployeeSkills;
    DELETE FROM dbo.EmployeeImages;
    DELETE FROM dbo.EmployeeDresses;
    DELETE FROM dbo.Employees;

    DELETE FROM dbo.CotivityEmployeeImages;
    DELETE FROM dbo.CotivityEmployees;

    DELETE FROM dbo.Departments;
    DELETE FROM dbo.Designations;
    DELETE FROM dbo.CotivityDepartments;
    DELETE FROM dbo.CotivityDesignations;
    DELETE FROM dbo.JobTypes;
    DELETE FROM dbo.BloodGroups;
    DELETE FROM dbo.Gender;
    DELETE FROM dbo.AddressType;
    DELETE FROM dbo.Roles;
    DELETE FROM dbo.Teams;
    DELETE FROM dbo.Skills;
    DELETE FROM dbo.Recruitments;
    DELETE FROM dbo.BusinessUnits;
    DELETE FROM dbo.DressSizes;

    -- every master table is empty here, so restart its identity at 1
    DBCC CHECKIDENT (N'dbo.Departments',            RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Designations',           RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.CotivityDepartments',    RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.CotivityDesignations',   RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.JobTypes',               RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.BloodGroups',            RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Gender',                 RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.AddressType',            RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Roles',                  RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Teams',                  RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Skills',                 RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.Recruitments',           RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.BusinessUnits',          RESEED, 0) WITH NO_INFOMSGS;
    DBCC CHECKIDENT (N'dbo.DressSizes',             RESEED, 0) WITH NO_INFOMSGS;

    COMMIT TRANSACTION;
END
GO

/* =========================================================
   1) MASTER / REFERENCE TABLES
      Rebuilds the master set after section 0. When section 0 is
      disabled the inserts stay idempotent by unique name.
   ========================================================= */

-- Departments
INSERT INTO dbo.Departments (DepartmentName, IsActive, CreatedBy, CreatedDate)
SELECT v.DepartmentName, 1, 'seed', SYSUTCDATETIME()
FROM (VALUES
    ('Engineering'), ('Human Resources'), ('Finance'),  ('Operations'), ('IT Support')
) v(DepartmentName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.Departments d WHERE d.DepartmentName = v.DepartmentName);
GO

-- CotivityDepartments
INSERT INTO dbo.CotivityDepartments (CotivityDepartmentName, IsActive, CreatedBy, CreatedDate)
SELECT v.CotivityDepartmentName, 1, 'seed', SYSUTCDATETIME()
FROM (VALUES
    ('Engineering'), ('Human Resources'), ('Finance'),  ('Operations'), ('IT Support'),
    -- section 2 joins these four by name, so they must exist even after a wipe
    ('Product'), ('Delivery'), ('Platform'), ('Solutions')
) v(CotivityDepartmentName)
WHERE NOT EXISTS (SELECT 1 FROM dbo.CotivityDepartments c WHERE c.CotivityDepartmentName = v.CotivityDepartmentName);
GO

-- Designations (with Rank)
INSERT INTO dbo.Designations (DesignationName, [Rank], IsActive, CreatedBy, CreatedDate)
SELECT v.DesignationName, v.[Rank], 1, 'seed', SYSUTCDATETIME()
FROM (VALUES
    ('Assistant VICE President', 1),  ('Senior Hr Director', 2),
    ('Hr Director', 3),  (' Hr Senior Manager', 4),
    ('Hr Manager', 5), ('Senior  Director  Engineering  ', 2),
    ('Director Engineering ', 3),  (' Senior Manager Engineering ',4),
    ('Manager Engineering', 5),  ('Senior Accountant', 6),
    ('QA Lead Manager', 7),  ('QA Engineer', 8), ('Senior Principal Engineer', 8),
    ('Senior Engineer', 9),  ('Software Engineer II', 10),
    ('Senior Engineer Support', 9),  ('Software Engineer Support II', 10),
    ('Software Engineer Support I', 11), 
    ('Software Engineer I', 11),  ('Associate Engineer', 12),
    ('Support', 13),  ('Trainee',14)
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
FROM (VALUES ('Full-Time'), ('Part-Time'), ('Contract'), ('Trainee'), ('Remote')) v(JobTypeName)
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
FROM (VALUES ('Super Admin'), ('Admin'), ('Director'),('Manager'), ('HR'),('Developer'),('IT'), ('Trainee'),
             -- required by the auth flow and by section 11; Admin lands on RoleId 2
             ('NormalUser')) v(RoleName)
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

/* ---- role -> permission grants restored after the rebuild ----------
   Section 0 dropped every RolePermissions row because Roles was emptied.
   Replay the snapshot taken there against the new RoleIds, so custom
   grants (including the Admin mirror from rbac-schema.sql) are kept. */
IF OBJECT_ID(N'tempdb..#RoleGrant') IS NOT NULL
BEGIN
    INSERT INTO dbo.RolePermissions (RoleId, PermissionId, CreatedBy, CreatedDate)
    SELECT r.RoleId, p.PermissionId, 'seed', SYSUTCDATETIME()
    FROM #RoleGrant g
    INNER JOIN dbo.Roles r       ON r.RoleName = g.RoleName
    INNER JOIN dbo.Permissions p ON p.PermissionCode = g.PermissionCode
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.RolePermissions rp
        WHERE rp.RoleId = r.RoleId AND rp.PermissionId = p.PermissionId
    );

    DROP TABLE #RoleGrant;
END
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
   3) EMPLOYEES  - DESTRUCTIVE RESEED (300 records)

      Master/reference tables are never touched here: this block owns
      only employee-domain rows. It purges them in FK order, then
      re-seeds 300 employees from a fixed name list (150 male, then
      150 female).

      WARNING: every employee is erased, together with the addresses,
      skills, images, dresses, releases, terminations, logins and role
      assignments that point at one. Set @ResetEmployees = 0 to keep
      the current employee data and skip the whole block.

      Purge order follows the FK graph (children first):
        RefreshTokens -> UserLoginHistory -> UserRoles -> Users
        -> Successions -> EmployeeReleases -> EmployeeTerminates
        -> EmployeeAddresses -> EmployeeSkills -> EmployeeImages
        -> EmployeeDresses -> Employees

      Successions must go before Employees: it holds three FKs into the
      employee domain (DepartingEmployeeId, InterimSupervisorEmployeeId,
      InterimSupervisorExternalId) and every one of them is NO ACTION.

      Deterministic layout (ENumber = 5000 + Seq):
        5001          head of the company       (no manager)
        5002-5003     executive tier            -> 5001
        5004-5008     department heads          -> 5001
        5009-5070     managers                  -> head of own department
        5071-5300     staff                     -> manager of own department
        5291-5300     inactive (exit pipeline: 5 resignations, 3 terminations)
   ========================================================= */
DECLARE @ResetEmployees BIT = 1;   -- 1 = wipe + reseed employees, 0 = keep existing

IF @ResetEmployees = 1
BEGIN
    BEGIN TRANSACTION;

    DELETE FROM dbo.RefreshTokens;
    DELETE FROM dbo.UserLoginHistory;
    DELETE FROM dbo.UserRoles;
    DELETE FROM dbo.Users;

    DELETE FROM dbo.Successions;

    DELETE FROM dbo.EmployeeReleases;
    DELETE FROM dbo.EmployeeTerminates;
    DELETE FROM dbo.EmployeeAddresses;
    DELETE FROM dbo.EmployeeSkills;
    DELETE FROM dbo.EmployeeImages;
    DELETE FROM dbo.EmployeeDresses;

    DELETE FROM dbo.Employees;

    INSERT INTO dbo.Employees
        (ENumber, FirstName, MiddleName, LastName, JoinedDate, JoinedDateBS,
         Email, AlternateEmail, DesignationId, DepartmentId, JobTypeId, BloodGroupId, GenderId,
         ReportsToEmployeeId, LeadEmployeeId, StationNumber, RecruitmentId, ExtensionNumber,
         CitizenshipNumber, DateOfBirth, DateOfBirthBS, PhoneNumber, MobileNumber,
         EmergencyContactNumber, EmergencyContactPerson, FatherName, MotherName, SpouseName,
         PANNumber, LockerNumber, TeamId, BusinessUnitId, CotivityEmployeeId, IsActive, CreatedBy, CreatedDate)
    SELECT
        s.Seq + 5000,
        s.FirstName, s.MiddleName, s.LastName,
        jd.JoinedDate,
        FORMAT(DATEADD(YEAR, 56, jd.JoinedDate), 'yyyy-MM-dd'),
        LOWER(s.FirstName) + '.' + LOWER(s.LastName)
            + RIGHT('000' + CAST(s.Seq AS VARCHAR(3)), 3) + '@teamhub.com',
        CASE WHEN s.Seq % 10 = 0 THEN
            'alt.' + LOWER(s.FirstName) + '.' + LOWER(s.LastName)
            + RIGHT('000' + CAST(s.Seq AS VARCHAR(3)), 3) + '@teamhub.com'
        END,
        COALESCE(des.DesignationId, (SELECT MIN(DesignationId) FROM dbo.Designations)),
        dep.DepartmentId, jt.JobTypeId, bg.BloodGroupId, g.GenderId,
        NULL, NULL,
        'ST-' + RIGHT('000' + CAST(s.Seq AS VARCHAR(3)), 3),
        rec.RecruitmentId,
        400 + s.Seq,
        'NPC-' + RIGHT('0000000' + CAST(100000 + s.Seq AS VARCHAR(10)), 7),
        dob.DateOfBirth,
        FORMAT(DATEADD(YEAR, 56, dob.DateOfBirth), 'yyyy-MM-dd'),
        '01-4' + RIGHT('00000' + CAST(s.Seq AS VARCHAR(5)), 5),
        '9800' + RIGHT('000000' + CAST(s.Seq AS VARCHAR(3)), 6),
        '9810' + RIGHT('000000' + CAST(s.Seq AS VARCHAR(3)), 6),
        fam.MotherName,
        fam.FatherName,
        fam.MotherName,
        fam.SpouseName,
        'PAN' + RIGHT('0000000' + CAST(500000 + s.Seq AS VARCHAR(10)), 7) + 'N',
        'LK-' + RIGHT('000' + CAST(s.Seq AS VARCHAR(3)), 3),
        tm.TeamId,
        bu.BusinessUnitId,
        cot.CotivityEmployeeId,
        CASE WHEN s.Seq > 290 THEN 0 ELSE 1 END,
        'seed', SYSUTCDATETIME()
    FROM (VALUES
        -- Seq, Gender, First, Middle, Last  (1-150 male, 151-300 female)
        (1, N'Male', N'Aarav', N'Kumar', N'Sharma'),
        (2, N'Male', N'Aayush', N'Raj', N'Thapa'),
        (3, N'Male', N'Abhishek', N'Prasad', N'Adhikari'),
        (4, N'Male', N'Aditya', N'Bahadur', N'Gurung'),
        (5, N'Male', N'Aman', N'Kumar', N'Yadav'),
        (6, N'Male', N'Amit', N'Raj', N'Shrestha'),
        (7, N'Male', N'Anil', N'Prasad', N'Karki'),
        (8, N'Male', N'Anish', N'Kumar', N'Bhandari'),
        (9, N'Male', N'Ankit', N'Raj', N'Poudel'),
        (10, N'Male', N'Arjun', N'Bahadur', N'Rana'),
        (11, N'Male', N'Ashish', N'Kumar', N'Chaudhary'),
        (12, N'Male', N'Bibek', N'Raj', N'KC'),
        (13, N'Male', N'Bikash', N'Prasad', N'Magar'),
        (14, N'Male', N'Binod', N'Kumar', N'Tamang'),
        (15, N'Male', N'Bishal', N'Raj', N'Basnet'),
        (16, N'Male', N'Deepak', N'Bahadur', N'Ghimire'),
        (17, N'Male', N'Dipesh', N'Kumar', N'Joshi'),
        (18, N'Male', N'Ganesh', N'Prasad', N'Pandey'),
        (19, N'Male', N'Gopal', N'Raj', N'Bhattarai'),
        (20, N'Male', N'Hari', N'Bahadur', N'Oli'),
        (21, N'Male', N'Hemant', N'Kumar', N'Mishra'),
        (22, N'Male', N'Ishaan', N'Raj', N'Khadka'),
        (23, N'Male', N'Karan', N'Kumar', N'Shah'),
        (24, N'Male', N'Kiran', N'Prasad', N'Rijal'),
        (25, N'Male', N'Krishna', N'Bahadur', N'Baral'),
        (26, N'Male', N'Laxman', N'Raj', N'Acharya'),
        (27, N'Male', N'Manish', N'Kumar', N'Dhakal'),
        (28, N'Male', N'Milan', N'Raj', N'Koirala'),
        (29, N'Male', N'Nabin', N'Prasad', N'Gautam'),
        (30, N'Male', N'Nischal', N'Kumar', N'Pandey'),
        (31, N'Male', N'Nishan', N'Raj', N'Thakuri'),
        (32, N'Male', N'Nitesh', N'Kumar', N'Bista'),
        (33, N'Male', N'Pawan', N'Raj', N'Shahi'),
        (34, N'Male', N'Prabin', N'Kumar', N'Pokhrel'),
        (35, N'Male', N'Prakash', N'Bahadur', N'Regmi'),
        (36, N'Male', N'Pramod', N'Raj', N'Neupane'),
        (37, N'Male', N'Prashant', N'Kumar', N'Sapkota'),
        (38, N'Male', N'Pratik', N'Raj', N'Aryal'),
        (39, N'Male', N'Rajan', N'Kumar', N'Baniya'),
        (40, N'Male', N'Ramesh', N'Prasad', N'Dahal'),
        (41, N'Male', N'Ranjan', N'Raj', N'Kandel'),
        (42, N'Male', N'Roshan', N'Kumar', N'Mahato'),
        (43, N'Male', N'Sagar', N'Raj', N'Mandal'),
        (44, N'Male', N'Sameer', N'Kumar', N'Yadav'),
        (45, N'Male', N'Sandeep', N'Raj', N'Sharma'),
        (46, N'Male', N'Sandesh', N'Kumar', N'Thapa'),
        (47, N'Male', N'Santosh', N'Prasad', N'Adhikari'),
        (48, N'Male', N'Saroj', N'Raj', N'Gurung'),
        (49, N'Male', N'Saurav', N'Kumar', N'Shrestha'),
        (50, N'Male', N'Suman', N'Raj', N'Karki'),
        (51, N'Male', N'Sunil', N'Kumar', N'Bhandari'),
        (52, N'Male', N'Suraj', N'Prasad', N'Poudel'),
        (53, N'Male', N'Sujan', N'Raj', N'Rana'),
        (54, N'Male', N'Sushant', N'Kumar', N'Chaudhary'),
        (55, N'Male', N'Ujjwal', N'Raj', N'KC'),
        (56, N'Male', N'Umesh', N'Prasad', N'Magar'),
        (57, N'Male', N'Vivek', N'Kumar', N'Tamang'),
        (58, N'Male', N'Yogesh', N'Raj', N'Basnet'),
        (59, N'Male', N'Ritesh', N'Kumar', N'Ghimire'),
        (60, N'Male', N'Rohan', N'Raj', N'Joshi'),
        (61, N'Male', N'Akash', N'Kumar', N'Pandey'),
        (62, N'Male', N'Alok', N'Raj', N'Bhattarai'),
        (63, N'Male', N'Amar', N'Prasad', N'Oli'),
        (64, N'Male', N'Anup', N'Kumar', N'Mishra'),
        (65, N'Male', N'Arnav', N'Raj', N'Khadka'),
        (66, N'Male', N'Ashok', N'Bahadur', N'Shah'),
        (67, N'Male', N'Bimal', N'Kumar', N'Rijal'),
        (68, N'Male', N'Chandan', N'Raj', N'Baral'),
        (69, N'Male', N'Dinesh', N'Prasad', N'Acharya'),
        (70, N'Male', N'Dipak', N'Kumar', N'Dhakal'),
        (71, N'Male', N'Dhiraj', N'Raj', N'Koirala'),
        (72, N'Male', N'Eshan', N'Kumar', N'Gautam'),
        (73, N'Male', N'Firoj', N'Ali', N'Khan'),
        (74, N'Male', N'Gaurav', N'Raj', N'Thakuri'),
        (75, N'Male', N'Girish', N'Kumar', N'Bista'),
        (76, N'Male', N'Hitesh', N'Raj', N'Shahi'),
        (77, N'Male', N'Jitendra', N'Kumar', N'Pokhrel'),
        (78, N'Male', N'Kamal', N'Prasad', N'Regmi'),
        (79, N'Male', N'Kapil', N'Raj', N'Neupane'),
        (80, N'Male', N'Kushal', N'Kumar', N'Sapkota'),
        (81, N'Male', N'Lokesh', N'Raj', N'Aryal'),
        (82, N'Male', N'Madan', N'Bahadur', N'Baniya'),
        (83, N'Male', N'Mohan', N'Prasad', N'Dahal'),
        (84, N'Male', N'Mukesh', N'Kumar', N'Kandel'),
        (85, N'Male', N'Nirmal', N'Raj', N'Mahato'),
        (86, N'Male', N'Niraj', N'Kumar', N'Mandal'),
        (87, N'Male', N'Pritam', N'Raj', N'Yadav'),
        (88, N'Male', N'Rabi', N'Kumar', N'Sharma'),
        (89, N'Male', N'Rajesh', N'Prasad', N'Thapa'),
        (90, N'Male', N'Rakesh', N'Raj', N'Adhikari'),
        (91, N'Male', N'Rishav', N'Kumar', N'Gurung'),
        (92, N'Male', N'Sachin', N'Raj', N'Shrestha'),
        (93, N'Male', N'Samir', N'Kumar', N'Karki'),
        (94, N'Male', N'Shankar', N'Prasad', N'Bhandari'),
        (95, N'Male', N'Shiva', N'Raj', N'Poudel'),
        (96, N'Male', N'Shyam', N'Bahadur', N'Rana'),
        (97, N'Male', N'Siddhant', N'Kumar', N'Chaudhary'),
        (98, N'Male', N'Srijan', N'Raj', N'KC'),
        (99, N'Male', N'Subash', N'Kumar', N'Magar'),
        (100, N'Male', N'Sudip', N'Raj', N'Tamang'),
        (101, N'Male', N'Surendra', N'Prasad', N'Basnet'),
        (102, N'Male', N'Tenzing', N'Dorje', N'Sherpa'),
        (103, N'Male', N'Uday', N'Kumar', N'Ghimire'),
        (104, N'Male', N'Vikas', N'Raj', N'Joshi'),
        (105, N'Male', N'Yubraj', N'Bahadur', N'Pandey'),
        (106, N'Male', N'Aakash', N'Raj', N'Bhattarai'),
        (107, N'Male', N'Anmol', N'Kumar', N'Oli'),
        (108, N'Male', N'Ashwin', N'Raj', N'Mishra'),
        (109, N'Male', N'Bhuvan', N'Kumar', N'Khadka'),
        (110, N'Male', N'Chirag', N'Raj', N'Shah'),
        (111, N'Male', N'Dev', N'Kumar', N'Rijal'),
        (112, N'Male', N'Dhruv', N'Raj', N'Baral'),
        (113, N'Male', N'Hriday', N'Kumar', N'Acharya'),
        (114, N'Male', N'Kabir', N'Raj', N'Dhakal'),
        (115, N'Male', N'Lakshya', N'Kumar', N'Koirala'),
        (116, N'Male', N'Manav', N'Raj', N'Gautam'),
        (117, N'Male', N'Neeraj', N'Kumar', N'Thakuri'),
        (118, N'Male', N'Om', N'Prakash', N'Bista'),
        (119, N'Male', N'Parth', N'Kumar', N'Shahi'),
        (120, N'Male', N'Raghav', N'Raj', N'Pokhrel'),
        (121, N'Male', N'Reyansh', N'Kumar', N'Regmi'),
        (122, N'Male', N'Rudra', N'Raj', N'Neupane'),
        (123, N'Male', N'Samarth', N'Kumar', N'Sapkota'),
        (124, N'Male', N'Shaurya', N'Raj', N'Aryal'),
        (125, N'Male', N'Shubham', N'Kumar', N'Baniya'),
        (126, N'Male', N'Tanmay', N'Raj', N'Dahal'),
        (127, N'Male', N'Utkarsh', N'Kumar', N'Kandel'),
        (128, N'Male', N'Vedant', N'Raj', N'Mahato'),
        (129, N'Male', N'Yash', N'Raj', N'Mandal'),
        (130, N'Male', N'Abhinav', N'Raj', N'Yadav'),
        (131, N'Male', N'Anurag', N'Kumar', N'Sharma'),
        (132, N'Male', N'Biraj', N'Raj', N'Thapa'),
        (133, N'Male', N'Dawa', N'Norbu', N'Sherpa'),
        (134, N'Male', N'Elish', N'Kumar', N'Adhikari'),
        (135, N'Male', N'Farhan', N'Ahmed', N'Khan'),
        (136, N'Male', N'Hammad', N'Ali', N'Ansari'),
        (137, N'Male', N'Imran', N'Ahmed', N'Siddiqui'),
        (138, N'Male', N'Junaid', N'Ali', N'Khan'),
        (139, N'Male', N'Kabir', N'Ahmed', N'Ansari'),
        (140, N'Male', N'Mahesh', N'Kumar', N'Gurung'),
        (141, N'Male', N'Nitesh', N'Raj', N'Shrestha'),
        (142, N'Male', N'Pranav', N'Kumar', N'Karki'),
        (143, N'Male', N'Rituraj', N'Kumar', N'Bhandari'),
        (144, N'Male', N'Shashank', N'Raj', N'Poudel'),
        (145, N'Male', N'Tejas', N'Kumar', N'Rana'),
        (146, N'Male', N'Utsav', N'Raj', N'Chaudhary'),
        (147, N'Male', N'Vaibhav', N'Kumar', N'KC'),
        (148, N'Male', N'Yaman', N'Raj', N'Magar'),
        (149, N'Male', N'Zaid', N'Ahmed', N'Khan'),
        (150, N'Male', N'Abhay', N'Kumar', N'Tamang'),
        (151, N'Female', N'Aaradhya', N'Kumari', N'Sharma'),
        (152, N'Female', N'Aastha', N'Kumari', N'Thapa'),
        (153, N'Female', N'Aayushi', N'Kumari', N'Adhikari'),
        (154, N'Female', N'Abha', N'Kumari', N'Gurung'),
        (155, N'Female', N'Aditi', N'Kumari', N'Yadav'),
        (156, N'Female', N'Aisha', N'Noor', N'Shrestha'),
        (157, N'Female', N'Alisha', N'Kumari', N'Karki'),
        (158, N'Female', N'Amrita', N'Kumari', N'Bhandari'),
        (159, N'Female', N'Anisha', N'Kumari', N'Poudel'),
        (160, N'Female', N'Anjali', N'Kumari', N'Rana'),
        (161, N'Female', N'Ankita', N'Kumari', N'Chaudhary'),
        (162, N'Female', N'Anu', N'Kumari', N'KC'),
        (163, N'Female', N'Anusha', N'Kumari', N'Magar'),
        (164, N'Female', N'Anushka', N'Kumari', N'Tamang'),
        (165, N'Female', N'Aparna', N'Kumari', N'Basnet'),
        (166, N'Female', N'Archana', N'Kumari', N'Ghimire'),
        (167, N'Female', N'Arpana', N'Kumari', N'Joshi'),
        (168, N'Female', N'Ashika', N'Kumari', N'Pandey'),
        (169, N'Female', N'Asmita', N'Kumari', N'Bhattarai'),
        (170, N'Female', N'Ayusha', N'Kumari', N'Oli'),
        (171, N'Female', N'Barsha', N'Kumari', N'Mishra'),
        (172, N'Female', N'Bhumika', N'Kumari', N'Khadka'),
        (173, N'Female', N'Binita', N'Kumari', N'Shah'),
        (174, N'Female', N'Bipana', N'Kumari', N'Rijal'),
        (175, N'Female', N'Brinda', N'Kumari', N'Baral'),
        (176, N'Female', N'Chandani', N'Kumari', N'Acharya'),
        (177, N'Female', N'Chanda', N'Kumari', N'Dhakal'),
        (178, N'Female', N'Deepa', N'Kumari', N'Koirala'),
        (179, N'Female', N'Deepika', N'Kumari', N'Gautam'),
        (180, N'Female', N'Diya', N'Kumari', N'Thakuri'),
        (181, N'Female', N'Elina', N'Kumari', N'Bista'),
        (182, N'Female', N'Ganga', N'Kumari', N'Shahi'),
        (183, N'Female', N'Garima', N'Kumari', N'Pokhrel'),
        (184, N'Female', N'Gita', N'Kumari', N'Regmi'),
        (185, N'Female', N'Grishma', N'Kumari', N'Neupane'),
        (186, N'Female', N'Hema', N'Kumari', N'Sapkota'),
        (187, N'Female', N'Isha', N'Kumari', N'Aryal'),
        (188, N'Female', N'Ishani', N'Kumari', N'Baniya'),
        (189, N'Female', N'Janaki', N'Kumari', N'Dahal'),
        (190, N'Female', N'Jasmine', N'Noor', N'Kandel'),
        (191, N'Female', N'Jenisha', N'Kumari', N'Mahato'),
        (192, N'Female', N'Jharana', N'Kumari', N'Mandal'),
        (193, N'Female', N'Jyoti', N'Kumari', N'Yadav'),
        (194, N'Female', N'Kabita', N'Kumari', N'Sharma'),
        (195, N'Female', N'Kalpana', N'Kumari', N'Thapa'),
        (196, N'Female', N'Karishma', N'Kumari', N'Adhikari'),
        (197, N'Female', N'Karuna', N'Kumari', N'Gurung'),
        (198, N'Female', N'Kiran', N'Kumari', N'Shrestha'),
        (199, N'Female', N'Kriti', N'Kumari', N'Karki'),
        (200, N'Female', N'Kusum', N'Kumari', N'Bhandari'),
        (201, N'Female', N'Laxmi', N'Kumari', N'Poudel'),
        (202, N'Female', N'Manisha', N'Kumari', N'Rana'),
        (203, N'Female', N'Manju', N'Kumari', N'Chaudhary'),
        (204, N'Female', N'Maya', N'Kumari', N'KC'),
        (205, N'Female', N'Menuka', N'Kumari', N'Magar'),
        (206, N'Female', N'Mina', N'Kumari', N'Tamang'),
        (207, N'Female', N'Monika', N'Kumari', N'Basnet'),
        (208, N'Female', N'Muskan', N'Kumari', N'Ghimire'),
        (209, N'Female', N'Namrata', N'Kumari', N'Joshi'),
        (210, N'Female', N'Nandini', N'Kumari', N'Pandey'),
        (211, N'Female', N'Nisha', N'Kumari', N'Bhattarai'),
        (212, N'Female', N'Nitu', N'Kumari', N'Oli'),
        (213, N'Female', N'Pabitra', N'Kumari', N'Mishra'),
        (214, N'Female', N'Pooja', N'Kumari', N'Khadka'),
        (215, N'Female', N'Pragati', N'Kumari', N'Shah'),
        (216, N'Female', N'Prakriti', N'Kumari', N'Rijal'),
        (217, N'Female', N'Pratima', N'Kumari', N'Baral'),
        (218, N'Female', N'Pratiksha', N'Kumari', N'Acharya'),
        (219, N'Female', N'Priya', N'Kumari', N'Dhakal'),
        (220, N'Female', N'Purnima', N'Kumari', N'Koirala'),
        (221, N'Female', N'Rachana', N'Kumari', N'Gautam'),
        (222, N'Female', N'Radhika', N'Kumari', N'Thakuri'),
        (223, N'Female', N'Rajani', N'Kumari', N'Bista'),
        (224, N'Female', N'Raksha', N'Kumari', N'Shahi'),
        (225, N'Female', N'Ramita', N'Kumari', N'Pokhrel'),
        (226, N'Female', N'Ranjana', N'Kumari', N'Regmi'),
        (227, N'Female', N'Rashmi', N'Kumari', N'Neupane'),
        (228, N'Female', N'Reema', N'Kumari', N'Sapkota'),
        (229, N'Female', N'Rekha', N'Kumari', N'Aryal'),
        (230, N'Female', N'Richa', N'Kumari', N'Baniya'),
        (231, N'Female', N'Rina', N'Kumari', N'Dahal'),
        (232, N'Female', N'Ritu', N'Kumari', N'Kandel'),
        (233, N'Female', N'Riya', N'Kumari', N'Mahato'),
        (234, N'Female', N'Roshani', N'Kumari', N'Mandal'),
        (235, N'Female', N'Rubina', N'Noor', N'Yadav'),
        (236, N'Female', N'Sabina', N'Kumari', N'Sharma'),
        (237, N'Female', N'Samiksha', N'Kumari', N'Thapa'),
        (238, N'Female', N'Samjhana', N'Kumari', N'Adhikari'),
        (239, N'Female', N'Sangita', N'Kumari', N'Gurung'),
        (240, N'Female', N'Sarita', N'Kumari', N'Shrestha'),
        (241, N'Female', N'Sarmila', N'Kumari', N'Karki'),
        (242, N'Female', N'Sashi', N'Kumari', N'Bhandari'),
        (243, N'Female', N'Shanti', N'Kumari', N'Poudel'),
        (244, N'Female', N'Sharmila', N'Kumari', N'Rana'),
        (245, N'Female', N'Shilpa', N'Kumari', N'Chaudhary'),
        (246, N'Female', N'Shreya', N'Kumari', N'KC'),
        (247, N'Female', N'Shraddha', N'Kumari', N'Magar'),
        (248, N'Female', N'Sita', N'Kumari', N'Tamang'),
        (249, N'Female', N'Smriti', N'Kumari', N'Basnet'),
        (250, N'Female', N'Sneha', N'Kumari', N'Ghimire'),
        (251, N'Female', N'Sonam', N'Dolma', N'Sherpa'),
        (252, N'Female', N'Sunita', N'Kumari', N'Joshi'),
        (253, N'Female', N'Supriya', N'Kumari', N'Pandey'),
        (254, N'Female', N'Sushma', N'Kumari', N'Bhattarai'),
        (255, N'Female', N'Sweta', N'Kumari', N'Oli'),
        (256, N'Female', N'Tamanna', N'Noor', N'Khan'),
        (257, N'Female', N'Tanisha', N'Kumari', N'Mishra'),
        (258, N'Female', N'Trishna', N'Kumari', N'Khadka'),
        (259, N'Female', N'Uma', N'Kumari', N'Shah'),
        (260, N'Female', N'Urmila', N'Kumari', N'Rijal'),
        (261, N'Female', N'Usha', N'Kumari', N'Baral'),
        (262, N'Female', N'Vandana', N'Kumari', N'Acharya'),
        (263, N'Female', N'Varsha', N'Kumari', N'Dhakal'),
        (264, N'Female', N'Vedika', N'Kumari', N'Koirala'),
        (265, N'Female', N'Yashaswi', N'Kumari', N'Gautam'),
        (266, N'Female', N'Yogita', N'Kumari', N'Thakuri'),
        (267, N'Female', N'Zara', N'Noor', N'Khan'),
        (268, N'Female', N'Zunaira', N'Fatima', N'Ansari'),
        (269, N'Female', N'Aakriti', N'Kumari', N'Bista'),
        (270, N'Female', N'Aanchal', N'Kumari', N'Shahi'),
        (271, N'Female', N'Aarya', N'Kumari', N'Pokhrel'),
        (272, N'Female', N'Adrija', N'Kumari', N'Regmi'),
        (273, N'Female', N'Akanksha', N'Kumari', N'Neupane'),
        (274, N'Female', N'Amisha', N'Kumari', N'Sapkota'),
        (275, N'Female', N'Anamika', N'Kumari', N'Aryal'),
        (276, N'Female', N'Anaya', N'Kumari', N'Baniya'),
        (277, N'Female', N'Anwesha', N'Kumari', N'Dahal'),
        (278, N'Female', N'Avani', N'Kumari', N'Kandel'),
        (279, N'Female', N'Bhawana', N'Kumari', N'Mahato'),
        (280, N'Female', N'Charu', N'Kumari', N'Mandal'),
        (281, N'Female', N'Disha', N'Kumari', N'Yadav'),
        (282, N'Female', N'Esha', N'Kumari', N'Sharma'),
        (283, N'Female', N'Falgun', N'Kumari', N'Thapa'),
        (284, N'Female', N'Harshita', N'Kumari', N'Adhikari'),
        (285, N'Female', N'Ira', N'Kumari', N'Gurung'),
        (286, N'Female', N'Jiya', N'Kumari', N'Shrestha'),
        (287, N'Female', N'Kalyani', N'Kumari', N'Karki'),
        (288, N'Female', N'Kashish', N'Kumari', N'Bhandari'),
        (289, N'Female', N'Khushi', N'Kumari', N'Poudel'),
        (290, N'Female', N'Lavanya', N'Kumari', N'Rana'),
        (291, N'Female', N'Mahima', N'Kumari', N'Chaudhary'),
        (292, N'Female', N'Navya', N'Kumari', N'KC'),
        (293, N'Female', N'Ojaswi', N'Kumari', N'Magar'),
        (294, N'Female', N'Palak', N'Kumari', N'Tamang'),
        (295, N'Female', N'Pari', N'Kumari', N'Basnet'),
        (296, N'Female', N'Prisha', N'Kumari', N'Ghimire'),
        (297, N'Female', N'Saanvi', N'Kumari', N'Joshi'),
        (298, N'Female', N'Shanaya', N'Kumari', N'Pandey'),
        (299, N'Female', N'Tisha', N'Kumari', N'Bhattarai'),
        (300, N'Female', N'Vanya', N'Kumari', N'Oli')
    ) s(Seq, Gender, FirstName, MiddleName, LastName)
    INNER JOIN dbo.Gender g ON g.GenderName = s.Gender
    CROSS APPLY (
        SELECT CAST(DATEADD(DAY, (s.Seq * 37) % 5840, CAST('2010-01-04' AS DATE)) AS DATE) AS JoinedDate
    ) jd
    CROSS APPLY (
        SELECT DATEADD(YEAR, -(22 + s.Seq % 18), jd.JoinedDate) AS DateOfBirth
    ) dob
    CROSS APPLY (
        SELECT CASE WHEN s.Seq = 1 THEN 1 WHEN s.Seq <= 3 THEN 2 WHEN s.Seq <= 8 THEN 3
                    WHEN s.Seq <= 70 THEN 5 ELSE 8 END AS Lo,
               CASE WHEN s.Seq = 1 THEN 1 WHEN s.Seq <= 3 THEN 2 WHEN s.Seq <= 8 THEN 3
                    WHEN s.Seq <= 70 THEN 7 ELSE 14 END AS Hi
    ) band
    OUTER APPLY (
        SELECT TOP (1) d.DesignationId
        FROM dbo.Designations d
        WHERE d.IsActive = 1 AND d.[Rank] BETWEEN band.Lo AND band.Hi
        ORDER BY ((d.DesignationId * 7 + s.Seq) % 11), d.DesignationId
    ) des
    CROSS APPLY (
        SELECT p.DepartmentId
        FROM (SELECT DepartmentId, ROW_NUMBER() OVER (ORDER BY DepartmentId) AS rn,
                     COUNT(*) OVER () AS cnt
              FROM dbo.Departments WHERE IsActive = 1) p
        WHERE p.rn = ((s.Seq - 1) % p.cnt) + 1
    ) dep
    CROSS APPLY (
        SELECT p.JobTypeId
        FROM (SELECT JobTypeId, ROW_NUMBER() OVER (ORDER BY JobTypeId) AS rn,
                     COUNT(*) OVER () AS cnt
              FROM dbo.JobTypes WHERE IsActive = 1) p
        WHERE p.rn = ((s.Seq - 1) % p.cnt) + 1
    ) jt
    CROSS APPLY (
        SELECT p.BloodGroupId
        FROM (SELECT BloodGroupId, ROW_NUMBER() OVER (ORDER BY BloodGroupId) AS rn,
                     COUNT(*) OVER () AS cnt
              FROM dbo.BloodGroups WHERE IsActive = 1) p
        WHERE p.rn = ((s.Seq - 1) % p.cnt) + 1
    ) bg
    CROSS APPLY (
        SELECT p.TeamId
        FROM (SELECT TeamId, ROW_NUMBER() OVER (ORDER BY TeamId) AS rn,
                     COUNT(*) OVER () AS cnt
              FROM dbo.Teams WHERE IsActive = 1) p
        WHERE p.rn = ((s.Seq - 1) % p.cnt) + 1
    ) tm
    OUTER APPLY (
        -- business units are optional; an empty master leaves the column NULL
        SELECT p.BusinessUnitId
        FROM (SELECT BusinessUnitId, ROW_NUMBER() OVER (ORDER BY BusinessUnitId) AS rn,
                     COUNT(*) OVER () AS cnt
              FROM dbo.BusinessUnits WHERE COALESCE(IsActive, 1) = 1) p
        WHERE p.rn = ((s.Seq - 1) % p.cnt) + 1
    ) bu
    OUTER APPLY (
        SELECT p.RecruitmentId
        FROM (SELECT RecruitmentId, ROW_NUMBER() OVER (ORDER BY RecruitmentId) AS rn,
                     COUNT(*) OVER () AS cnt
              FROM dbo.Recruitments WHERE IsActive = 1) p
        WHERE p.rn = ((s.Seq - 1) % p.cnt) + 1
    ) rec
    OUTER APPLY (
        SELECT p.CotivityEmployeeId
        FROM (SELECT CotivityEmployeeId, ROW_NUMBER() OVER (ORDER BY CotivityEmployeeId) AS rn
              FROM dbo.CotivityEmployees) p
        WHERE p.rn = s.Seq
    ) cot
    CROSS APPLY (
        SELECT
            dad.DadName + ' ' + s.LastName AS FatherName,
            mom.MomName + ' ' + s.LastName AS MotherName,
            CASE WHEN s.Seq % 4 = 0 THEN
                (CASE WHEN s.Gender = N'Male' THEN N'Anita' ELSE N'Ramesh' END) + ' ' + s.LastName
            END AS SpouseName
        FROM (SELECT TOP (1) f.DadName
              FROM (VALUES (1, N'Ramesh'), (2, N'Suresh'), (3, N'Bikash'), (4, N'Deepak'),
                           (5, N'Nabin'), (6, N'Prakash'), (7, N'Sanjay'), (8, N'Krishna')) f(N, DadName)
              WHERE f.N = ((s.Seq - 1) % 8) + 1) dad
        CROSS JOIN (SELECT TOP (1) m.MomName
              FROM (VALUES (1, N'Sita'), (2, N'Gita'), (3, N'Mina'), (4, N'Laxmi'),
                           (5, N'Sarita'), (6, N'Bina'), (7, N'Rita'), (8, N'Anita')) m(N, MomName)
              WHERE m.N = ((s.Seq - 1) % 8) + 1) mom
    ) fam;

    /* The self-referencing FK is satisfied only after the flat insert, so the
       hierarchy is linked tier by tier, top-down. */
    UPDATE e SET ReportsToEmployeeId = boss.EmployeeId
    FROM dbo.Employees e
    CROSS JOIN (SELECT EmployeeId FROM dbo.Employees WHERE ENumber = 5001) boss
    WHERE e.ENumber BETWEEN 5002 AND 5008;

    UPDATE e SET ReportsToEmployeeId = head.EmployeeId
    FROM dbo.Employees e
    OUTER APPLY (SELECT TOP (1) h.EmployeeId
                 FROM dbo.Employees h
                 WHERE h.ENumber BETWEEN 5004 AND 5008
                   AND h.DepartmentId = e.DepartmentId
                 ORDER BY h.ENumber) head
    WHERE e.ENumber BETWEEN 5009 AND 5070;

    UPDATE e SET ReportsToEmployeeId = mgr.EmployeeId
    FROM dbo.Employees e
    OUTER APPLY (SELECT TOP (1) m.EmployeeId
                 FROM dbo.Employees m
                 WHERE m.ENumber BETWEEN 5009 AND 5070
                   AND m.DepartmentId = e.DepartmentId
                 ORDER BY m.ENumber) mgr
    WHERE e.ENumber BETWEEN 5071 AND 5300;

    -- a department without a head or manager reports straight to the top
    UPDATE e SET ReportsToEmployeeId = boss.EmployeeId
    FROM dbo.Employees e
    CROSS JOIN (SELECT EmployeeId FROM dbo.Employees WHERE ENumber = 5001) boss
    WHERE e.ENumber BETWEEN 5002 AND 5300
      AND e.ReportsToEmployeeId IS NULL;

    -- the functional lead of the staff tier is their department head
    UPDATE e SET LeadEmployeeId = head.EmployeeId
    FROM dbo.Employees e
    OUTER APPLY (SELECT TOP (1) h.EmployeeId
                 FROM dbo.Employees h
                 WHERE h.ENumber BETWEEN 5004 AND 5008
                   AND h.DepartmentId = e.DepartmentId
                 ORDER BY h.ENumber) head
    WHERE e.ENumber BETWEEN 5071 AND 5300;

    COMMIT TRANSACTION;
END
GO

/* =========================================================
   4) EMPLOYEE ADDRESSES
      One permanent address per employee, plus a current address
      for every third employee (ENumber = 5000 + Seq).
   ========================================================= */
IF OBJECT_ID(N'dbo.EmployeeAddresses', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.EmployeeAddresses)
BEGIN
    INSERT INTO dbo.EmployeeAddresses
        (EmployeeId, AddressTypeId, AddressLine1, AddressLine2, City, [State],
         Latitude, Longitude, IsPrimary, CreatedBy, CreatedDate)
    SELECT e.EmployeeId, at.AddressTypeId,
           CAST(e.ENumber - 5000 AS VARCHAR(5)) + ' ' + loc.Street, NULL,
           loc.City, loc.[State], loc.Latitude, loc.Longitude, slot.IsPrimary,
           'seed', SYSUTCDATETIME()
    FROM dbo.Employees e
    INNER JOIN dbo.AddressType at ON at.AddressTypeName IN (N'Permanent', N'Current')
    CROSS APPLY (SELECT CASE WHEN at.AddressTypeName = N'Permanent' THEN 1 ELSE 0 END AS IsPrimary) slot
    CROSS APPLY (
        SELECT p.Street, p.City, p.[State], p.Latitude, p.Longitude
        FROM (VALUES
            (1, N'Lakeview Road', N'Kathmandu',  N'Bagmati', 27.71720000, 85.32400000),
            (2, N'Ring Road',     N'Lalitpur',   N'Bagmati', 27.67150000, 85.32500000),
            (3, N'New Road',      N'Bhaktapur',  N'Bagmati', 27.67150000, 85.42900000),
            (4, N'Lakeside',      N'Pokhara',    N'Gandaki', 28.20960000, 83.98560000),
            (5, N'Main Road',     N'Biratnagar', N'Koshi',   26.45250000, 87.27180000),
            (6, N'Station Road',  N'Nepalgunj',  N'Lumbini', 28.05000000, 81.61670000)
        ) p(N, Street, City, [State], Latitude, Longitude)
        WHERE p.N = ((e.ENumber - 5001) % 6) + 1
    ) loc
    WHERE e.ENumber BETWEEN 5001 AND 5300
      AND (at.AddressTypeName = N'Permanent' OR (e.ENumber - 5000) % 3 = 0);
END
GO

/* =========================================================
   5) EMPLOYEE SKILLS  (two skills per employee, SkillLevel 2-5)
   ========================================================= */
IF OBJECT_ID(N'dbo.EmployeeSkills', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.EmployeeSkills)
BEGIN
    INSERT INTO dbo.EmployeeSkills
        (EmployeeId, SkillId, SkillLevel, IsPrimary, CreatedBy, CreatedDate)
    SELECT e.EmployeeId, sk.SkillId,
           CASE WHEN slot.SlotNo = 1 THEN 3 + (e.ENumber % 3) ELSE 2 + (e.ENumber % 4) END,
           CASE WHEN slot.SlotNo = 1 THEN 1 ELSE 0 END,
           'seed', SYSUTCDATETIME()
    FROM dbo.Employees e
    CROSS JOIN (VALUES (1), (2)) slot(SlotNo)
    CROSS APPLY (
        SELECT p.SkillId
        FROM (SELECT SkillId, ROW_NUMBER() OVER (ORDER BY SkillId) AS rn, COUNT(*) OVER () AS cnt
              FROM dbo.Skills WHERE IsActive = 1) p
        WHERE p.rn = (((e.ENumber - 5001) + (slot.SlotNo - 1) * 5) % p.cnt) + 1
    ) sk
    WHERE e.ENumber BETWEEN 5001 AND 5300;
END
GO

/* =========================================================
   6) EMPLOYEE IMAGES

      ImagePath is stored web-root relative, exactly like the value
      produced by ToFileUrl ("InfiniteImages/<file>"). Only files that
      exist under wwwroot are referenced: referencing missing files made
      every image decode blow up the whole list request.
   ========================================================= */
IF OBJECT_ID(N'dbo.EmployeeImages', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.EmployeeImages)
BEGIN
    INSERT INTO dbo.EmployeeImages (EmployeeId, ImagePath, CreatedBy, CreatedDate)
    SELECT e.EmployeeId, f.ImagePath, 'seed', SYSUTCDATETIME()
    FROM dbo.Employees e
    CROSS APPLY (SELECT ((e.ENumber - 5001) % 5) + 1 AS SlotNo) n
    CROSS APPLY (
        SELECT ImagePath FROM (VALUES
            (1, 'InfiniteImages/employee-6-c2f3dbd1694246cab4d4ec62c8c8706e.jpg'),
            (2, 'InfiniteImages/employee-8-04b8b561fb4947c4afc2b7ad988110c3.png'),
            (3, 'InfiniteImages/employee-9-42f51b5850f7404882ec15da1a9d6971.png'),
            (4, 'InfiniteImages/employee-10-c9712bc372f048208ac7d699e685192a.png'),
            (5, 'InfiniteImages/employee-91-48b984c516424b7596e3a914a3ad35d1.png')
        ) v(SlotNo, ImagePath)
        WHERE v.SlotNo = n.SlotNo
    ) f
    WHERE e.ENumber BETWEEN 5001 AND 5030;
END
GO

-- CotivityEmployeeImages
IF NOT EXISTS (SELECT 1 FROM dbo.CotivityEmployeeImages)
BEGIN
    INSERT INTO dbo.CotivityEmployeeImages (CotivityEmployeeId, CotivityImagePath, CreatedBy, CreatedDate)
    SELECT ce.CotivityEmployeeId, f.ImagePath, 'seed', SYSUTCDATETIME()
    FROM dbo.CotivityEmployees ce
    INNER JOIN (VALUES
        ('Alice Turner', 'CotivityImages/cotivity-employee-1-d4027a2819dc4775bddaa1c3b6817ec5.png'),
        ('Bob Cox',      'CotivityImages/cotivity-employee-5-c61d438753b14e8cbf4d20cdc3a0c894.png'),
        ('Cara Diaz',    'CotivityImages/cotivity-employee-3-c50923e80fe2404fa2cdc06a60616e93.png'),
        ('Dan Frost',    'CotivityImages/cotivity-employee-4-f31094d12d534255b7deecb4cdbbeb3c.png')
    ) f(CotivityEmployeeName, ImagePath) ON f.CotivityEmployeeName = ce.CotivityEmployeeName;
END
GO

/* =========================================================
   7) EMPLOYEE DRESSES  (Shirt for odd Seq, Trouser for even)
   ========================================================= */
IF OBJECT_ID(N'dbo.EmployeeDresses', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.DressSizes', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.EmployeeDresses)
BEGIN
    INSERT INTO dbo.EmployeeDresses (EmployeeId, DressId, CreatedBy, CreatedDate)
    SELECT e.EmployeeId, ds.DressId, 'seed', SYSUTCDATETIME()
    FROM dbo.Employees e
    CROSS APPLY (SELECT e.ENumber - 5000 AS SeqNo) n
    CROSS APPLY (
        SELECT d.DressId
        FROM dbo.DressSizes d
        WHERE d.DressName = CASE WHEN n.SeqNo % 2 = 1 THEN N'Shirt' ELSE N'Trouser' END
          AND d.[Size] = CASE WHEN n.SeqNo % 2 = 1
                              THEN CHOOSE(((n.SeqNo - 1) / 2) % 4 + 1, 'S', 'M', 'L', 'XL')
                              ELSE CHOOSE((n.SeqNo / 2) % 3 + 1, '30', '32', '34') END
    ) ds
    WHERE e.ENumber BETWEEN 5001 AND 5300;
END
GO

/* =========================================================
   8) EMPLOYEE RELEASES (resignations)
      One record per ReleaseStatusWorkflow status.
   ========================================================= */
IF OBJECT_ID(N'dbo.EmployeeReleases', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.EmployeeReleases)
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
        (5291, 'Resignation', '2026-01-05', NULL, NULL, 30, 0,
            'Better career opportunity', NULL, 'Draft filed by the employee',
            0, 0, 0, NULL, 0, 0, 'Draft', NULL, NULL),
        (5292, 'Resignation', '2025-12-15', '2025-12-15', '2026-01-31', 45, 45,
            'Relocating abroad with family', NULL, 'Knowledge transfer in progress',
            1, 0, 0, 85000.00, 0, 0, 'Submitted', NULL, NULL),
        (5293, 'Resignation', '2025-11-20', '2025-11-20', '2026-01-05', 45, 45,
            'Pursuing higher studies', NULL, 'Awaiting approver decision',
            1, 1, 0, 90000.00, 0, 0, 'PendingApproval', NULL, NULL),
        (5294, 'Resignation', '2025-10-01', '2025-10-01', '2025-11-15', 45, 45,
            'Family relocation', NULL, 'Approved by the reporting manager',
            1, 1, 1, 75000.00, 1, 0, 'Approved', 'Aarav Kumar Sharma', '2025-10-28 10:00:00'),
        (5295, 'Resignation', '2025-08-01', '2025-08-01', '2025-09-15', 45, 45,
            'Seeking growth outside the organisation', 'Notice shortened at employee request',
            'Full handover completed',
            1, 1, 1, 120000.00, 1, 1, 'Released', 'Aarav Kumar Sharma', '2025-09-15 09:30:00')
    ) s(ENumber, ReleaseType, ResignationDate, NoticeDate, LastWorkingDate,
        NoticePeriodDays, ServedNoticeDays, Reason, EarlyReleaseReason, Remarks,
        KnowledgeTransferCompleted, AssetsReturned, ExitInterviewCompleted,
        SettlementAmount, RelievingLetterIssued, ExperienceLetterIssued, Status, ApprovedBy, ApprovedDate)
    INNER JOIN dbo.Employees e ON e.ENumber = s.ENumber;
END
GO

/* =========================================================
   9) EMPLOYEE TERMINATIONS  (table is dbo.EmployeeTerminates)
      Statuses match TerminationStatusWorkflow.
   ========================================================= */
IF OBJECT_ID(N'dbo.EmployeeTerminates', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.EmployeeTerminates)
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
        (5296, 'Policy Violation', '2025-12-31', 1,
            'Investigation confirmed a workplace policy breach',
            2, '2025-10-15', 'Repeated violation of workplace policy',
            'Final settlement after asset recovery', 1, 1, 1, 60000.00, 0, 1, '2025-12-31',
            'Terminated', 'Aarav Kumar Sharma', '2025-12-31 09:00:00'),
        (5297, 'Performance', '2025-11-30', 0, NULL,
            1, '2025-09-01', 'Sustained underperformance after the improvement plan',
            'PIP closed without improvement', 1, 1, 1, 45000.00, 1, 1, '2025-11-30',
            'Terminated', 'Aarav Kumar Sharma', '2025-11-30 10:00:00'),
        (5298, 'Redundancy', '2026-01-15', 0, NULL,
            0, NULL, 'Role made redundant in the restructuring',
            'Eligible for rehire if the role reopens', 1, 1, 0, 100000.00, 1, 0, NULL,
            'Approved', NULL, NULL)
    ) s(ENumber, TerminationType, EffectiveDate, InvestigationRequired, InvestigationSummary,
        WarningCount, LastWarningDate, Reason, Remarks, AssetRecovered, SystemAccessRevoked,
        ExitInterviewCompleted, SettlementAmount, EligibleForRehire, TerminationLetterIssued,
        TerminationLetterDate, Status, ApprovedBy, ApprovedDate)
    INNER JOIN dbo.Employees e ON e.ENumber = s.ENumber;
END
GO

/* =========================================================
   10) AUTH USERS  (no passwords - Microsoft Entra ID is the sign-in mechanism;
        the Development bypass signs these accounts in passwordlessly.)

       One account per seeded role, so every role the Role Permission
       Matrix knows about can be signed in as and inspected end to end.
       These role names must also appear in Auth:DevSignIn:AllowedRoles
       (appsettings.json): GET /api/auth/dev-users and
       POST /api/auth/dev-signin both drop an account whose roles are not
       on that list.

       Every employee below is taken from the active tiers (ENumber <= 5290).
   ========================================================= */
DECLARE @SignInAccounts TABLE
(
    ENumber   INT           PRIMARY KEY,
    Username  NVARCHAR(100) NOT NULL,
    RoleName  NVARCHAR(100) NOT NULL
);

INSERT INTO @SignInAccounts (ENumber, Username, RoleName) VALUES
    (5001, N'Admin',      N'Admin'),        -- company head (Seq 1)
    (5002, N'SuperAdmin', N'Super Admin'),  -- executive tier
    (5003, N'Director',   N'Director'),     -- executive tier
    (5004, N'HR',         N'HR'),           -- Hr Director
    (5009, N'Manager',    N'Manager'),      -- first department manager
    (5071, N'Developer',  N'Developer'),    -- staff engineer
    (5072, N'IT',         N'IT'),           -- staff engineer
    (5073, N'Trainee',    N'Trainee'),      -- staff engineer
    (5075, N'NormalUser', N'NormalUser');   -- plain staff member

INSERT INTO dbo.Users (EmployeeId, Username, Email, IsActive, CreatedDate)
SELECT e.EmployeeId, a.Username, e.Email, 1, SYSUTCDATETIME()
FROM @SignInAccounts a
INNER JOIN dbo.Employees e ON e.ENumber = a.ENumber
WHERE NOT EXISTS (SELECT 1 FROM dbo.Users u WHERE u.EmployeeId = e.EmployeeId OR u.Username = a.Username);

/* Each account carries exactly one role in the authoritative UserRoles
   junction (the legacy Employees.RoleId column has been removed). */
IF OBJECT_ID(N'dbo.UserRoles', N'U') IS NOT NULL
BEGIN
    INSERT INTO dbo.UserRoles (UserId, RoleId, CreatedBy, CreatedDate)
    SELECT u.UserId, r.RoleId, 'seed', SYSUTCDATETIME()
    FROM @SignInAccounts a
    INNER JOIN dbo.Users u ON u.Username = a.Username
    INNER JOIN dbo.Roles r ON r.RoleName = a.RoleName AND r.IsActive = 1
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
    INSERT INTO dbo.Permissions (PermissionCode, PermissionName, Module, Description, IsActive, CreatedBy, CreatedDate)
    SELECT 'resignations.button', N'Show Resignations Navigation', N'Resignations',
           N'Show the Resignations navigation button.', 1, 'seed', SYSUTCDATETIME()
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.Permissions
        WHERE PermissionCode = 'resignations.button'
    );

    INSERT INTO dbo.Permissions (PermissionCode, PermissionName, Module, Description, IsActive, CreatedBy, CreatedDate)
    SELECT permissions.PermissionCode, permissions.PermissionName, N'Settings', permissions.Description, 1, 'seed', SYSUTCDATETIME()
    FROM (VALUES
        ('setting.gear', N'Show Settings Gear', N'Show the settings gear menu.'),
        ('setting.gear.EmployeeRole', N'Open Employee Role Matrix', N'Open the Employee Role Matrix from the settings gear menu.'),
        ('setting.gear.RolePermission', N'Open Role Permission Matrix', N'Open the Role Permission Matrix from the settings gear menu.')
    ) permissions(PermissionCode, PermissionName, Description)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.Permissions existing
        WHERE existing.PermissionCode = permissions.PermissionCode
    );

    INSERT INTO dbo.Permissions (PermissionCode, PermissionName, Module, Description, IsActive, CreatedBy, CreatedDate)
    SELECT permissions.PermissionCode, permissions.PermissionName, N'People', permissions.Description, 1, 'seed', SYSUTCDATETIME()
    FROM (VALUES
        ('people.delete', N'Delete Employee', N'Soft-delete employee records while retaining history.'),
        ('people.cardview', N'View Employee Cards', N'Open employee card views from the People module.')
    ) permissions(PermissionCode, PermissionName, Description)
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.Permissions existing
        WHERE existing.PermissionCode = permissions.PermissionCode
    );

    INSERT INTO dbo.RolePermissions (RoleId, PermissionId, CreatedBy, CreatedDate)
    SELECT r.RoleId, p.PermissionId, 'seed', SYSUTCDATETIME()
    FROM (VALUES
        -- NormalUser: a plain employee. "People" needs no code (authenticatedGuard only),
        -- so this single grant is what makes the "Apply Resignation" entry appear.
        (N'NormalUser', N'profile.view'),
        (N'NormalUser', N'profile.edit'),
        (N'Admin', N'resignations.button'),
        (N'Admin', N'resignations.view'),
        (N'NormalUser', N'resignations.add'),

        -- Manager: runs the resignation queue for their team (no termination writes).
        (N'Admin', N'setting.gear'),
        (N'Admin', N'setting.gear.EmployeeRole'),
        (N'Admin', N'setting.gear.RolePermission'),

        (N'Manager', N'resignations.button'),
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
        (N'Manager', N'people.view'),
        (N'HR', N'people.view'),
        (N'HR', N'people.create'),
        (N'HR', N'people.update'),
        (N'HR', N'people.release'),
        (N'Admin', N'people.delete'),
        (N'HR', N'people.delete'),
        (N'Admin', N'people.cardview'),
        (N'HR', N'people.cardview'),
        (N'HR', N'master.recruitment'),
        (N'HR', N'resignations.button'),
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

    /* Every seeded role needs the two self-service codes, otherwise its demo
       account can sign in but cannot open or edit its own profile. */
    INSERT INTO dbo.RolePermissions (RoleId, PermissionId, CreatedBy, CreatedDate)
    SELECT r.RoleId, p.PermissionId, 'seed', SYSUTCDATETIME()
    FROM dbo.Roles r
    INNER JOIN dbo.Permissions p
            ON p.PermissionCode IN (N'profile.view', N'profile.edit') AND p.IsActive = 1
    WHERE r.IsActive = 1
      AND NOT EXISTS (
          SELECT 1 FROM dbo.RolePermissions rp
          WHERE rp.RoleId = r.RoleId AND rp.PermissionId = p.PermissionId
      );

    /* Super Admin mirrors Admin exactly. rbac-schema.sql only mirrors the
       catalog onto Admin, so without this the top role would sign in empty. */
    INSERT INTO dbo.RolePermissions (RoleId, PermissionId, CreatedBy, CreatedDate)
    SELECT sa.RoleId, rp.PermissionId, 'seed', SYSUTCDATETIME()
    FROM dbo.Roles sa
    INNER JOIN dbo.Roles admin ON admin.RoleName = N'Admin' AND admin.IsActive = 1
    INNER JOIN dbo.RolePermissions rp ON rp.RoleId = admin.RoleId
    WHERE sa.RoleName = N'Super Admin' AND sa.IsActive = 1
      AND NOT EXISTS (
          SELECT 1 FROM dbo.RolePermissions existing
          WHERE existing.RoleId = sa.RoleId AND existing.PermissionId = rp.PermissionId
      );
END
GO

PRINT 'TeamHub seed data applied.';
GO
