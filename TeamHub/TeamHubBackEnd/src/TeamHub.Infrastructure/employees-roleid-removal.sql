/*
    Employees.RoleId removal migration for TeamHub.

    Roles are now authoritative ONLY in dbo.UserRoles (per user, managed by the employee
    sign-in form and the Employee Role Matrix). The legacy single-role column
    dbo.Employees.RoleId is removed.

    Idempotent and safe to re-run. It:
      1. Preserves existing role assignments by copying any Employees.RoleId into UserRoles
         for accounts that already exist (guards against losing a login's role).
      2. Drops any foreign-key / index / default bound to Employees.RoleId.
      3. Drops the Employees.RoleId column.

    Column DROP is irreversible - apply step 1, verify the app, then the drop runs in the same
    script. Run with a runner that executes each GO batch (SqlClient cannot parse GO itself).
*/

/* ---- 1. Preserve roles: copy Employees.RoleId into UserRoles for existing accounts ---- */
IF COL_LENGTH(N'dbo.Employees', N'RoleId') IS NOT NULL AND OBJECT_ID(N'dbo.UserRoles') IS NOT NULL
BEGIN
    INSERT INTO dbo.UserRoles (UserId, RoleId, CreatedBy, CreatedDate)
    SELECT u.UserId, e.RoleId, 'roleid-migration', SYSUTCDATETIME()
    FROM dbo.Users u
    INNER JOIN dbo.Employees e ON e.EmployeeId = u.EmployeeId
    INNER JOIN dbo.Roles r ON r.RoleId = e.RoleId AND r.IsActive = 1
    WHERE NOT EXISTS (
        SELECT 1 FROM dbo.UserRoles ur WHERE ur.UserId = u.UserId AND ur.RoleId = e.RoleId
    );
END
GO

/* ---- 2a. Drop foreign keys bound to Employees.RoleId ---- */
DECLARE @sql NVARCHAR(MAX) = N'';
SELECT @sql = @sql + N'ALTER TABLE dbo.Employees DROP CONSTRAINT [' + fk.name + N'];'
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fkc ON fk.object_id = fkc.constraint_object_id
INNER JOIN sys.columns c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE fk.parent_object_id = OBJECT_ID(N'dbo.Employees') AND c.name = N'RoleId';
IF @sql <> N'' EXEC(@sql);
GO

/* ---- 2b. Drop any default constraint bound to Employees.RoleId ---- */
DECLARE @df NVARCHAR(258);
SELECT @df = dc.name
FROM sys.default_constraints dc
INNER JOIN sys.columns c
    ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Employees') AND c.name = N'RoleId';
IF @df IS NOT NULL
    EXEC(N'ALTER TABLE dbo.Employees DROP CONSTRAINT [' + @df + N']');
GO

/* ---- 2c. Drop the nonclustered index on Employees.RoleId (if present) ---- */
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_Employees_RoleId' AND object_id = OBJECT_ID(N'dbo.Employees'))
    EXEC(N'DROP INDEX IX_Employees_RoleId ON dbo.Employees');
GO

/* ---- 3. Drop the column ---- */
IF COL_LENGTH(N'dbo.Employees', N'RoleId') IS NOT NULL
    ALTER TABLE dbo.Employees DROP COLUMN RoleId;
GO

/* ---- Validation queries (run after applying) ----
   EmployeesRoleIdLength  = NULL  (column gone)
   UserCount / UserRoleCount unchanged or higher (roles preserved in UserRoles)
*/
SELECT
    COL_LENGTH(N'dbo.Employees', N'RoleId') AS EmployeesRoleIdLength,
    (SELECT COUNT(1) FROM dbo.Users)        AS UserCount,
    (SELECT COUNT(1) FROM dbo.UserRoles)    AS UserRoleCount,
    (SELECT COUNT(1) FROM dbo.Users u
        INNER JOIN dbo.UserRoles ur ON ur.UserId = u.UserId) AS UsersWithRoleCount;
GO
