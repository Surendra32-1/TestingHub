/*
    Entra ID authentication migration for TeamHub.
    Run against the existing TeamHub database after authentication-schema.sql.

    Idempotent and safe to re-run. It:
      1. Adds nullable Entra identity columns to dbo.Users (for linking Entra accounts later).
      2. Adds a filtered unique index enforcing one local account per linked Entra identity.
      3. Removes all password storage (PasswordHash, PasswordSalt, IsFirstLogin) because
         Microsoft Entra ID / the Development bypass replace local password authentication.

    The seeded Admin/NormalUser rows, their UserRoles assignments, and the Users.EmployeeId
    foreign key are preserved. Column DROP is irreversible - validate before dropping.

    Apply with a runner that executes each GO batch (SqlClient cannot parse GO itself).
*/

/* ---- 1. Entra identity columns ---- */
IF COL_LENGTH(N'dbo.Users', N'EntraTenantId') IS NULL
    ALTER TABLE dbo.Users ADD EntraTenantId NVARCHAR(64) NULL;
GO

IF COL_LENGTH(N'dbo.Users', N'EntraObjectId') IS NULL
    ALTER TABLE dbo.Users ADD EntraObjectId NVARCHAR(64) NULL;
GO

/* ---- 2. Filtered unique index for linked Entra identities ---- */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UQ_Users_EntraIdentity' AND object_id = OBJECT_ID(N'dbo.Users'))
    CREATE UNIQUE INDEX UQ_Users_EntraIdentity
        ON dbo.Users(EntraTenantId, EntraObjectId)
        WHERE EntraTenantId IS NOT NULL AND EntraObjectId IS NOT NULL;
GO

/* ---- 3. Remove password storage ---- */
-- Drop any default constraint bound to IsFirstLogin before dropping the column.
DECLARE @df NVARCHAR(258);
SELECT @df = dc.name
FROM sys.default_constraints dc
INNER JOIN sys.columns c
    ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Users') AND c.name = N'IsFirstLogin';
IF @df IS NOT NULL
    EXEC(N'ALTER TABLE dbo.Users DROP CONSTRAINT [' + @df + N']');
GO

IF COL_LENGTH(N'dbo.Users', N'PasswordHash') IS NOT NULL
    ALTER TABLE dbo.Users DROP COLUMN PasswordHash;
GO

IF COL_LENGTH(N'dbo.Users', N'PasswordSalt') IS NOT NULL
    ALTER TABLE dbo.Users DROP COLUMN PasswordSalt;
GO

IF COL_LENGTH(N'dbo.Users', N'IsFirstLogin') IS NOT NULL
    ALTER TABLE dbo.Users DROP COLUMN IsFirstLogin;
GO

/* ---- Validation queries (run after applying; expect the following) ----
   EntraTenantIdPresent / EntraObjectIdPresent = 64 (NVARCHAR length) or non-null column id
   PasswordHashPresent / PasswordSaltPresent / IsFirstLoginPresent = NULL
   UserCount and UserRoleCount unchanged from before the migration.
*/
SELECT
    COL_LENGTH(N'dbo.Users', N'EntraTenantId')  AS EntraTenantIdLength,
    COL_LENGTH(N'dbo.Users', N'EntraObjectId')  AS EntraObjectIdLength,
    COL_LENGTH(N'dbo.Users', N'PasswordHash')   AS PasswordHashLength,
    COL_LENGTH(N'dbo.Users', N'PasswordSalt')   AS PasswordSaltLength,
    COL_LENGTH(N'dbo.Users', N'IsFirstLogin')   AS IsFirstLoginLength;
GO

SELECT
    (SELECT COUNT(1) FROM dbo.Users)     AS UserCount,
    (SELECT COUNT(1) FROM dbo.UserRoles) AS UserRoleCount,
    (SELECT COUNT(1) FROM dbo.Users u INNER JOIN dbo.Employees e ON e.EmployeeId = u.EmployeeId) AS LinkedEmployeeCount;
GO
