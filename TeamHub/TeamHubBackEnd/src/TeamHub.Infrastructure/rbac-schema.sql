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

/* Rename legacy employee permissions without dropping their role grants. */
BEGIN TRANSACTION;

DECLARE @EmployeePermissionCodes TABLE
(
    OldCode VARCHAR(100) NOT NULL,
    NewCode VARCHAR(100) NOT NULL
);

INSERT INTO @EmployeePermissionCodes (OldCode, NewCode)
VALUES
    ('people.view', 'administration.view'),
    ('people.create', 'administration.create'),
    ('people.update', 'administration.update'),
    ('people.release', 'administration.release'),
    ('people.terminate', 'administration.terminate'),
    ('people.hierarchy.view', 'administration.hierarchy.view'),
    ('people.delete', 'administration.delete'),
    ('people.cardview', 'administration.cardview'),
    ('allemployee.search.view', 'people.allemployee.search.view');

DELETE oldGrant
FROM dbo.RolePermissions oldGrant
INNER JOIN dbo.Permissions oldPermission ON oldPermission.PermissionId = oldGrant.PermissionId
INNER JOIN @EmployeePermissionCodes codeMap ON codeMap.OldCode = oldPermission.PermissionCode
INNER JOIN dbo.Permissions newPermission ON newPermission.PermissionCode = codeMap.NewCode
WHERE EXISTS
(
    SELECT 1
    FROM dbo.RolePermissions existingGrant
    WHERE existingGrant.RoleId = oldGrant.RoleId
      AND existingGrant.PermissionId = newPermission.PermissionId
);

UPDATE oldGrant
SET PermissionId = newPermission.PermissionId
FROM dbo.RolePermissions oldGrant
INNER JOIN dbo.Permissions oldPermission ON oldPermission.PermissionId = oldGrant.PermissionId
INNER JOIN @EmployeePermissionCodes codeMap ON codeMap.OldCode = oldPermission.PermissionCode
INNER JOIN dbo.Permissions newPermission ON newPermission.PermissionCode = codeMap.NewCode;

DELETE oldPermission
FROM dbo.Permissions oldPermission
INNER JOIN @EmployeePermissionCodes codeMap ON codeMap.OldCode = oldPermission.PermissionCode
WHERE EXISTS
(
    SELECT 1
    FROM dbo.Permissions newPermission
    WHERE newPermission.PermissionCode = codeMap.NewCode
);

UPDATE oldPermission
SET PermissionCode = codeMap.NewCode,
    Module = CASE WHEN codeMap.NewCode = 'people.allemployee.search.view'
                  THEN N'People' ELSE N'Administration' END,
    LastModifiedBy = 'permission-migration',
    LastModifiedDate = SYSUTCDATETIME()
FROM dbo.Permissions oldPermission
INNER JOIN @EmployeePermissionCodes codeMap ON codeMap.OldCode = oldPermission.PermissionCode;

COMMIT TRANSACTION;
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
    ('people.allemployee.search.view', N'Search All Employees', N'People', N'Open the Employee search page and view active employee cards.'),
    ('administration.view',             N'View Employees',          N'Administration', N'View the employee directory and profiles.'),
    ('administration.create',           N'Add Employee',            N'Administration', N'Create new employee records.'),
    ('administration.update',           N'Update Employee',         N'Administration', N'Edit existing employee records.'),
    ('administration.delete',           N'Delete Employee',         N'Administration', N'Soft-delete employee records while retaining history.'),
    ('administration.cardview',         N'View Employee Cards',     N'Administration', N'Open employee card views from the Administration module.'),
    ('administration.release',          N'Release Employee',        N'Administration', N'Release an employee from the organization.'),
    ('administration.terminate',        N'Terminate Employee',      N'Administration', N'Terminate an employee record.'),
    ('administration.hierarchy.view',   N'View Hierarchy',          N'Administration', N'View the organization hierarchy chart.'),
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
    ('role-permissions.manage', N'Manage Role Permissions', N'RBAC', N'Grant permissions to roles in the matrix.'),
    ('setting.gear', N'Show Settings Gear', N'Settings', N'Show the settings gear menu.'),
    ('setting.gear.EmployeeRole', N'Open Employee Role Matrix', N'Settings',  N'Open the Employee Role Matrix from the settings gear menu.'),
    ('setting.gear.RolePermission', N'Open Role Permission Matrix', N'Settings', N'Open the Role Permission Matrix from the settings gear menu.')
) v(PermissionCode, PermissionName, Module, Description)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Permissions p WHERE p.PermissionCode = v.PermissionCode
);

/* UI-level master permissions: the existing master.<key> codes remain the
   backend API authorization contract; these codes control frontend visibility. */
INSERT INTO dbo.Permissions (PermissionCode, PermissionName, Module, Description, IsActive, CreatedBy, CreatedDate)
SELECT
    CONCAT('master.', m.MasterKey, '.', a.ActionKey),
    CONCAT(a.ActionName, N' ', m.MasterName),
    m.Module,
    CONCAT(a.ActionName, N' access to the ', m.MasterName, N' master page.'),
    1,
    'permission-seed',
    SYSUTCDATETIME()
FROM (VALUES
    ('department', N'Department', N'Organization Master'),
    ('designation', N'Designation', N'Organization Master'),
    ('business-unit', N'Business Unit', N'Organization Master'),
    ('dress-size', N'Dress Size', N'Organization Master'),
    ('address-type', N'Address Type', N'Organization Master'),
    ('blood-group', N'Blood Group', N'Organization Master'),
    ('skills', N'Skills', N'Organization Master'),
    ('job-type', N'Job Type', N'Organization Master'),
    ('team', N'Team', N'Organization Master'),
    ('gender', N'Gender', N'Organization Master'),
    ('cotivity-department', N'Cotivity Department', N'Cotivity'),
    ('cotivity-designation', N'Cotivity Designation', N'Cotivity'),
    ('cotivity-employee', N'Cotivity Employee', N'Cotivity'),
    ('role', N'Role', N'Settings'),
    ('recruitment', N'Recruitment', N'Settings')
) m(MasterKey, MasterName, Module)
CROSS JOIN (VALUES
    ('view', N'View'),
    ('create', N'Create'),
    ('update', N'Update'),
    ('delete', N'Delete')
) a(ActionKey, ActionName)
WHERE NOT EXISTS (
    SELECT 1
    FROM dbo.Permissions p
    WHERE p.PermissionCode = CONCAT('master.', m.MasterKey, '.', a.ActionKey)
);

INSERT INTO dbo.Permissions (PermissionCode, PermissionName, Module, Description, IsActive, CreatedBy, CreatedDate)
SELECT v.PermissionCode, v.PermissionName, v.Module, v.Description, 1, 'permission-seed', SYSUTCDATETIME()
FROM (VALUES
    ('setting.gear.role.view', N'View Roles', N'Settings', N'View user roles.'),
    ('setting.gear.role.create', N'Create Role', N'Settings', N'Create new user roles.'),
    ('setting.gear.role.edit', N'Edit Role', N'Settings', N'Edit existing user roles.'),
    ('setting.gear.role.delete', N'Delete Role', N'Settings', N'Delete user roles.'),
    ('setting.gear.manage.permissions.view', N'View Permissions', N'Settings', N'View application permissions.'),
    ('setting.gear.manage.permissions.create', N'Create Permission', N'Settings', N'Create new application permissions.'),
    ('setting.gear.manage.permissions.edit', N'Edit Permission', N'Settings', N'Edit existing application permissions.'),
    ('setting.gear.manage.permissions.delete', N'Delete Permission', N'Settings', N'Delete application permissions.')
) v(PermissionCode, PermissionName, Module, Description)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Permissions p WHERE p.PermissionCode = v.PermissionCode
);

INSERT INTO dbo.Permissions (PermissionCode, PermissionName, Module, Description, IsActive, CreatedBy, CreatedDate)
SELECT v.PermissionCode, v.PermissionName, v.Module, v.Description, 1, 'permission-seed', SYSUTCDATETIME()
FROM (VALUES
    ('cotivity.department.view', N'View Department', N'Cotivity', N'View Cotivity departments.'),
    ('cotivity.department.create', N'Create Department', N'Cotivity', N'Create new Cotivity departments.'),
    ('cotivity.department.edit', N'Edit Department', N'Cotivity', N'Edit existing Cotivity departments.'),
    ('cotivity.department.delete', N'Delete Department', N'Cotivity', N'Delete Cotivity departments.'),
    ('cotivity.designation.view', N'View Designation', N'Cotivity', N'View Cotivity designations.'),
    ('cotivity.designation.create', N'Create Designation', N'Cotivity', N'Create new Cotivity designations.'),
    ('cotivity.designation.edit', N'Edit Designation', N'Cotivity', N'Edit existing Cotivity designations.'),
    ('cotivity.designation.delete', N'Delete Designation', N'Cotivity', N'Delete Cotivity designations.'),
    ('cotivity.employee.view', N'View Employee', N'Cotivity', N'View Cotivity employee records.'),
    ('cotivity.employee.create', N'Create Employee', N'Cotivity', N'Create new Cotivity employee records.'),
    ('cotivity.employee.edit', N'Edit Employee', N'Cotivity', N'Edit existing Cotivity employee records.'),
    ('cotivity.employee.delete', N'Delete Employee', N'Cotivity', N'Delete Cotivity employee records.')
) v(PermissionCode, PermissionName, Module, Description)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Permissions p WHERE p.PermissionCode = v.PermissionCode
);

INSERT INTO dbo.Permissions (PermissionCode, PermissionName, Module, Description, IsActive, CreatedBy, CreatedDate)
SELECT v.PermissionCode, v.PermissionName, v.Module, v.Description, 1, 'permission-seed', SYSUTCDATETIME()
FROM (VALUES
    ('organization.department.view', N'View Department', N'Organization', N'View organization departments.'),
    ('organization.department.create', N'Create Department', N'Organization', N'Create new organization departments.'),
    ('organization.department.edit', N'Edit Department', N'Organization', N'Edit existing organization departments.'),
    ('organization.department.delete', N'Delete Department', N'Organization', N'Delete organization departments.'),
    ('organization.designation.view', N'View Designation', N'Organization', N'View organization designations.'),
    ('organization.designation.create', N'Create Designation', N'Organization', N'Create new organization designations.'),
    ('organization.designation.edit', N'Edit Designation', N'Organization', N'Edit existing organization designations.'),
    ('organization.designation.delete', N'Delete Designation', N'Organization', N'Delete organization designations.'),
    ('organization.businessunit.view', N'View Business Unit', N'Organization', N'View organization business units.'),
    ('organization.businessunit.create', N'Create Business Unit', N'Organization', N'Create new organization business units.'),
    ('organization.businessunit.edit', N'Edit Business Unit', N'Organization', N'Edit existing organization business units.'),
    ('organization.businessunit.delete', N'Delete Business Unit', N'Organization', N'Delete organization business units.'),
    ('organization.dresssize.view', N'View Dress Size', N'Organization', N'View available dress sizes.'),
    ('organization.dresssize.create', N'Create Dress Size', N'Organization', N'Create new dress sizes.'),
    ('organization.dresssize.edit', N'Edit Dress Size', N'Organization', N'Edit existing dress sizes.'),
    ('organization.dresssize.delete', N'Delete Dress Size', N'Organization', N'Delete dress sizes.'),
    ('organization.addresstype.view', N'View Address Type', N'Organization', N'View address types.'),
    ('organization.addresstype.create', N'Create Address Type', N'Organization', N'Create new address types.'),
    ('organization.addresstype.edit', N'Edit Address Type', N'Organization', N'Edit existing address types.'),
    ('organization.addresstype.delete', N'Delete Address Type', N'Organization', N'Delete address types.'),
    ('organization.skills.view', N'View Skills', N'Organization', N'View available skills.'),
    ('organization.skills.create', N'Create Skill', N'Organization', N'Create new skills.'),
    ('organization.skills.edit', N'Edit Skill', N'Organization', N'Edit existing skills.'),
    ('organization.skills.delete', N'Delete Skill', N'Organization', N'Delete skills.'),
    ('organization.jobtype.view', N'View Job Type', N'Organization', N'View job types.'),
    ('organization.jobtype.create', N'Create Job Type', N'Organization', N'Create new job types.'),
    ('organization.jobtype.edit', N'Edit Job Type', N'Organization', N'Edit existing job types.'),
    ('organization.jobtype.delete', N'Delete Job Type', N'Organization', N'Delete job types.'),
    ('organization.team.view', N'View Team', N'Organization', N'View organization teams.'),
    ('organization.team.create', N'Create Team', N'Organization', N'Create new organization teams.'),
    ('organization.team.edit', N'Edit Team', N'Organization', N'Edit existing organization teams.'),
    ('organization.team.delete', N'Delete Team', N'Organization', N'Delete organization teams.'),
    ('organization.gender.view', N'View Gender', N'Organization', N'View gender options.'),
    ('organization.gender.create', N'Create Gender', N'Organization', N'Create new gender options.'),
    ('organization.gender.edit', N'Edit Gender', N'Organization', N'Edit existing gender options.'),
    ('organization.gender.delete', N'Delete Gender', N'Organization', N'Delete gender options.'),
    ('organization.bloodgroup.view', N'View Blood Group', N'Organization', N'View available blood groups.'),
    ('organization.bloodgroup.create', N'Create Blood Group', N'Organization', N'Create new blood groups.'),
    ('organization.bloodgroup.edit', N'Edit Blood Group', N'Organization', N'Edit existing blood groups.'),
    ('organization.bloodgroup.delete', N'Delete Blood Group', N'Organization', N'Delete blood groups.')
) v(PermissionCode, PermissionName, Module, Description)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Permissions p WHERE p.PermissionCode = v.PermissionCode
);

/* Preserve grants on the previous generic UI-level codes during this rename. */
INSERT INTO dbo.RolePermissions (RoleId, PermissionId, CreatedBy, CreatedDate)
SELECT oldGrant.RoleId, newPermission.PermissionId, 'permission-migration', SYSUTCDATETIME()
FROM dbo.RolePermissions oldGrant
INNER JOIN dbo.Permissions oldPermission ON oldPermission.PermissionId = oldGrant.PermissionId
CROSS JOIN (VALUES
    ('department', 'department'),
    ('designation', 'designation'),
    ('business-unit', 'businessunit'),
    ('dress-size', 'dresssize'),
    ('address-type', 'addresstype'),
    ('blood-group', 'bloodgroup'),
    ('skills', 'skills'),
    ('job-type', 'jobtype'),
    ('team', 'team'),
    ('gender', 'gender')
) masterMap(MasterKey, OrganizationKey)
CROSS JOIN (VALUES
    ('view', 'view'),
    ('create', 'create'),
    ('update', 'edit'),
    ('delete', 'delete')
) actionMap(OldAction, NewAction)
INNER JOIN dbo.Permissions newPermission
    ON newPermission.PermissionCode = CONCAT('organization.', masterMap.OrganizationKey, '.', actionMap.NewAction)
WHERE oldPermission.PermissionCode = CONCAT('master.', masterMap.MasterKey, '.', actionMap.OldAction)
  AND NOT EXISTS (
      SELECT 1 FROM dbo.RolePermissions existingGrant
      WHERE existingGrant.RoleId = oldGrant.RoleId
        AND existingGrant.PermissionId = newPermission.PermissionId
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
