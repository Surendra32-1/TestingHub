/* =========================================================
   TeamHub - RBAC SCHEMA
   Role-Based Access Control tables + data migration.

   Run AGAINST THE EXISTING TeamHub database AFTER:
     1) databasesesign.sql        (Roles, Employees, ...)
     2) authentication-schema.sql (Users)

   Conventions mirror databasesesign.sql:
     - INT IDENTITY primary keys
     - VARCHAR reference-style columns
     - IsActive BIT NOT NULL DEFAULT(1)
     - CreatedBy/CreatedDate/LastModifiedBy/LastModifiedDate audit columns

   IMPORTANT
     - A permission CATALOG is seeded (see PERMISSION CATALOG SEED) so the
       Role Permission Matrix has definitions to display and paginate.
     - The migration block only copies EXISTING single-role assignments
       (Employees.RoleId) into the new UserRoles junction so that current
       logins keep working. It does not invent user/role data.
     - Employees.RoleId is retained (non-authoritative) - NOT dropped here.

   Apply with SSMS or:
     sqlcmd -S ".\SQLEXPRESS" -d TeamHub -E -i rbac-schema.sql
   ========================================================= */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

/* =========================================================
   PERMISSIONS (permission definitions)
========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='Permissions')
BEGIN
    CREATE TABLE dbo.Permissions
    (
        PermissionId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        PermissionCode VARCHAR(100) NOT NULL,
        PermissionName VARCHAR(150) NOT NULL,
        Module VARCHAR(100) NULL,
        Description NVARCHAR(500) NULL,
        IsActive BIT NOT NULL DEFAULT(1),
        CreatedBy VARCHAR(100) NULL,
        CreatedDate DATETIME2 NULL,
        LastModifiedBy VARCHAR(100) NULL,
        LastModifiedDate DATETIME2 NULL,
        CONSTRAINT UQ_Permissions_PermissionCode UNIQUE (PermissionCode)
    );
END
GO

/* =========================================================
   ROLEPERMISSIONS (role -> permission junction)
========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='RolePermissions')
BEGIN
    CREATE TABLE dbo.RolePermissions
    (
        RolePermissionId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        RoleId INT NOT NULL,
        PermissionId INT NOT NULL,
        CreatedBy VARCHAR(100) NULL,
        CreatedDate DATETIME2 NULL,
        LastModifiedBy VARCHAR(100) NULL,
        LastModifiedDate DATETIME2 NULL,
        CONSTRAINT UQ_RolePermissions_Role_Permission UNIQUE (RoleId, PermissionId),
        CONSTRAINT FK_RolePermissions_Role
            FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId),
        CONSTRAINT FK_RolePermissions_Permission
            FOREIGN KEY (PermissionId) REFERENCES dbo.Permissions(PermissionId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RolePermissions_RoleId' AND object_id=OBJECT_ID('dbo.RolePermissions'))
    CREATE INDEX IX_RolePermissions_RoleId ON dbo.RolePermissions(RoleId);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RolePermissions_PermissionId' AND object_id=OBJECT_ID('dbo.RolePermissions'))
    CREATE INDEX IX_RolePermissions_PermissionId ON dbo.RolePermissions(PermissionId);
GO

/* =========================================================
   USERROLES (user -> role junction; multiple roles per user)
========================================================= */
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name='UserRoles')
BEGIN
    CREATE TABLE dbo.UserRoles
    (
        UserRoleId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        UserId INT NOT NULL,
        RoleId INT NOT NULL,
        CreatedBy VARCHAR(100) NULL,
        CreatedDate DATETIME2 NULL,
        LastModifiedBy VARCHAR(100) NULL,
        LastModifiedDate DATETIME2 NULL,
        CONSTRAINT UQ_UserRoles_User_Role UNIQUE (UserId, RoleId),
        CONSTRAINT FK_UserRoles_User
            FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_UserRoles_Role
            FOREIGN KEY (RoleId) REFERENCES dbo.Roles(RoleId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_UserRoles_RoleId' AND object_id=OBJECT_ID('dbo.UserRoles'))
    CREATE INDEX IX_UserRoles_RoleId ON dbo.UserRoles(RoleId);
GO

/* =========================================================
   PERMISSION CATALOG SEED
   Populates dbo.Permissions so the Role Permission Matrix has
   real definitions to list and paginate. Idempotent: inserts
   only codes not already present, so re-running is safe.
   Codes cover the app modules plus the 3 stable RBAC codes
   referenced end-to-end by RbacPolicies.PolicyPermissions.
========================================================= */
BEGIN TRANSACTION;

INSERT INTO dbo.Permissions (PermissionCode, PermissionName, Module, Description, IsActive, CreatedBy, CreatedDate)
SELECT v.PermissionCode, v.PermissionName, v.Module, v.Description, 1, 'permission-seed', SYSUTCDATETIME()
FROM (VALUES
    ('profile.view',            N'View Own Profile',          N'Profile',      N'View the authenticated employee''s own employee record and related profile data.'),
    ('profile.edit',            N'Edit Own Profile',          N'Profile',      N'Update the authenticated employee''s own permitted profile fields and profile picture.'),
    ('people.view',             N'View Employees',          N'People',       N'View the employee directory and profiles.'),
    ('people.create',           N'Add Employee',            N'People',       N'Create new employee records.'),
    ('people.update',           N'Update Employee',         N'People',       N'Edit existing employee records.'),
    ('people.delete',           N'Delete Employee',         N'People',       N'Soft-delete employee records while retaining history.'),
    ('people.cardview',         N'View Employee Cards',     N'People',       N'Open employee card views from the People module.'),
    ('people.release',          N'Release Employee',        N'People',       N'Release an employee from the organization.'),
    ('people.terminate',        N'Terminate Employee',      N'People',       N'Terminate an employee record.'),
    ('people.hierarchy.view',   N'View Hierarchy',          N'People',       N'View the organization hierarchy chart.'),
    ('resignations.button',     N'Show Resignations Navigation', N'Resignations', N'Show the Resignations navigation button.'),
    ('resignations.view',       N'View Resignations',       N'Resignations', N'View resignation records.'),
    ('resignations.manage',     N'Manage Resignations',     N'Resignations', N'Create and update resignation records.'),
    ('resignations.add',                    N'File Resignation',        N'Resignations', N'File a self-service resignation (locked to the logged-in employee).'),
    ('resignations.action.submit',          N'Submit Resignation',      N'Resignations', N'Move a resignation from Draft to Submitted.'),
    ('resignations.action.sendforapproval', N'Send Resignation for Approval', N'Resignations', N'Move a resignation to Pending Approval.'),
    ('resignations.action.approve',         N'Approve Resignation',     N'Resignations', N'Approve a resignation.'),
    ('resignations.action.reject',          N'Reject Resignation',      N'Resignations', N'Reject a resignation.'),
    ('resignations.action.cancel',          N'Cancel Resignation',      N'Resignations', N'Cancel a resignation.'),
    ('resignations.action.exitchecklist',   N'Manage Exit Checklist',   N'Resignations', N'Update the resignation exit-clearance checklist.'),
    ('resignations.action.release',         N'Release Resignation',     N'Resignations', N'Move an approved, cleared resignation to Released.'),
    ('resignations.search',            N'Search Resignations',            N'Resignations', N'Search resignation records.'),
    ('resignations.search.status',     N'Search Resignations by Status',  N'Resignations', N'Filter resignation records by status.'),
    ('resignations.search.department', N'Search Resignations by Department', N'Resignations', N'Filter resignation records by department.'),
    ('terminations.view',       N'View Terminations',       N'Terminations', N'View termination records.'),
    ('terminations.add',        N'Add Termination',         N'Terminations', N'File a new termination record for an employee.'),
    ('terminations.manage',     N'Manage Terminations',     N'Terminations', N'Update and transition termination records.'),
    ('terminations.search',            N'Search Terminations',            N'Terminations', N'Search termination records.'),
    ('terminations.search.status',     N'Search Terminations by Status',  N'Terminations', N'Filter termination records by status.'),
    ('terminations.search.department', N'Search Terminations by Department', N'Terminations', N'Filter termination records by department.'),
    ('master.department',           N'Master: Department',          N'Settings',     N'Maintain the Department master data.'),
    ('master.designation',          N'Master: Designation',         N'Settings',     N'Maintain the Designation master data.'),
    ('master.business-unit',        N'Master: Business Unit',       N'Settings',     N'Maintain the Business Unit master data.'),
    ('master.dress-size',           N'Master: Dress Size',          N'Settings',     N'Maintain the Dress Size master data.'),
    ('master.cotivity-department',  N'Master: Cotivity Department', N'Settings',     N'Maintain the Cotivity Department master data.'),
    ('master.cotivity-designation', N'Master: Cotivity Designation',N'Settings',     N'Maintain the Cotivity Designation master data.'),
    ('master.cotivity-employee',    N'Master: Cotivity Employee',   N'Settings',     N'Maintain the Cotivity Employee master data.'),
    ('master.role',                 N'Master: Role',                N'Settings',     N'Maintain the Role master data.'),
    ('master.address-type',         N'Master: Address Type',        N'Settings',     N'Maintain the Address Type master data.'),
    ('master.blood-group',          N'Master: Blood Group',         N'Settings',     N'Maintain the Blood Group master data.'),
    ('master.skills',               N'Master: Skills',              N'Settings',     N'Maintain the Skills master data.'),
    ('master.job-type',             N'Master: Job Type',            N'Settings',     N'Maintain the Job Type master data.'),
    ('master.team',                 N'Master: Team',                N'Settings',     N'Maintain the Team master data.'),
    ('master.gender',               N'Master: Gender',              N'Settings',     N'Maintain the Gender master data.'),
    ('master.recruitment',          N'Master: Recruitment',         N'Settings',     N'Maintain the Recruitment Source master data.'),
    ('permissions.manage',      N'Manage Permissions',      N'RBAC',         N'Create and maintain permission definitions.'),
    ('user-roles.assign',       N'Assign User Roles',       N'RBAC',         N'Assign roles to employees in the matrix.'),
    ('role-permissions.manage', N'Manage Role Permissions', N'RBAC',         N'Grant permissions to roles in the matrix.')
    ('role-permissions.manage', N'Manage Role Permissions', N'RBAC',         N'Grant permissions to roles in the matrix.'),
    ('setting.gear', N'Show Settings Gear', N'Settings', N'Show the settings gear menu.'),
    ('setting.gear.EmployeeRole', N'Open Employee Role Matrix', N'Settings',  N'Open the Employee Role Matrix from the settings gear menu.'),
    ('setting.gear.RolePermission', N'Open Role Permission Matrix', N'Settings', N'Open the Role Permission Matrix from the settings gear menu.')
) v(PermissionCode, PermissionName, Module, Description)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Permissions p WHERE p.PermissionCode = v.PermissionCode
);

/* Mirror super-admin access: grant the full catalog to the Admin role. */
INSERT INTO dbo.RolePermissions (RoleId, PermissionId, CreatedBy, CreatedDate)
SELECT r.RoleId, p.PermissionId, 'permission-seed', SYSUTCDATETIME()
FROM dbo.Roles r
CROSS JOIN dbo.Permissions p
WHERE r.RoleName = 'Admin' AND r.IsActive = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.RolePermissions rp
      WHERE rp.RoleId = r.RoleId AND rp.PermissionId = p.PermissionId
  );

COMMIT TRANSACTION;
GO

/* =========================================================
   DATA MIGRATION
   Preserve existing single-role assignments by copying
   Employees.RoleId into UserRoles for users that already
   exist. Idempotent: re-running inserts nothing new.
========================================================= */
BEGIN TRANSACTION;

INSERT INTO dbo.UserRoles (UserId, RoleId, CreatedBy, CreatedDate)
SELECT u.UserId, e.RoleId, 'rbac-migration', SYSUTCDATETIME()
FROM dbo.Users u
INNER JOIN dbo.Employees e ON e.EmployeeId = u.EmployeeId
INNER JOIN dbo.Roles r ON r.RoleId = e.RoleId
WHERE r.IsActive = 1
  AND NOT EXISTS (
      SELECT 1 FROM dbo.UserRoles ur
      WHERE ur.UserId = u.UserId AND ur.RoleId = e.RoleId
  );

COMMIT TRANSACTION;
GO

/* =========================================================
   VERIFICATION (informational)
   Lists any user with zero active role assignments so an
   administrator can correct them in the Employee Role Matrix.
========================================================= */
SELECT u.UserId, u.Username, e.EmployeeId
FROM dbo.Users u
INNER JOIN dbo.Employees e ON e.EmployeeId = u.EmployeeId
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.UserRoles ur
    INNER JOIN dbo.Roles r ON r.RoleId = ur.RoleId AND r.IsActive = 1
    WHERE ur.UserId = u.UserId
)
ORDER BY u.Username;
GO

PRINT 'TeamHub RBAC schema applied.';
GO

/* =========================================================
   ROLLBACK (manual, only if required) - drop in reverse
   dependency order. Commented out on purpose.

   DROP TABLE IF EXISTS dbo.UserRoles;
   DROP TABLE IF EXISTS dbo.RolePermissions;
   DROP TABLE IF EXISTS dbo.Permissions;
========================================================= */
