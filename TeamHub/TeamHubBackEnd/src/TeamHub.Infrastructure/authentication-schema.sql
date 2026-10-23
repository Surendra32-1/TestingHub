/*
    Authentication schema for TeamHub.
    Run against the existing TeamHub database after databasesesign.sql.
    Employee roles are stored in the dbo.UserRoles junction (per user); the legacy
    dbo.Employees.RoleId column has been removed.
*/

IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        UserId INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
        EmployeeId INT NOT NULL,
        Username NVARCHAR(100) NOT NULL,
        Email NVARCHAR(255)  NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
        LastLoginDate DATETIME2 NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedDate DEFAULT (SYSUTCDATETIME()),
        UpdatedDate DATETIME2 NULL,
        CONSTRAINT UQ_Users_EmployeeId UNIQUE (EmployeeId),
        CONSTRAINT UQ_Users_Username UNIQUE (Username),
        CONSTRAINT FK_Users_Employees FOREIGN KEY (EmployeeId) REFERENCES dbo.Employees(EmployeeId)
    );
END
GO

IF OBJECT_ID(N'dbo.UserLoginHistory', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserLoginHistory
    (
        LoginHistoryId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_UserLoginHistory PRIMARY KEY,
        UserId INT NOT NULL,
        LoginDate DATETIME2 NOT NULL CONSTRAINT DF_UserLoginHistory_LoginDate DEFAULT (SYSUTCDATETIME()),
        LogoutDate DATETIME2 NULL,
        IpAddress NVARCHAR(50) NULL,
        UserAgent NVARCHAR(500) NULL,
        IsSuccessful BIT NOT NULL,
        CONSTRAINT FK_UserLoginHistory_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId)
    );
END
GO

IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshTokens
    (
        RefreshTokenId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RefreshTokens PRIMARY KEY,
        UserId INT NOT NULL,
        LoginHistoryId BIGINT NOT NULL,
        Token NVARCHAR(500) NOT NULL,
        ExpiryDate DATETIME2 NOT NULL,
        CreatedDate DATETIME2 NOT NULL CONSTRAINT DF_RefreshTokens_CreatedDate DEFAULT (SYSUTCDATETIME()),
        RevokedDate DATETIME2 NULL,
        ReplacedByToken NVARCHAR(500) NULL,
        CreatedByIp NVARCHAR(50) NULL,
        RevokedByIp NVARCHAR(50) NULL,
        UserAgent NVARCHAR(500) NULL,
        CONSTRAINT FK_RefreshTokens_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(UserId),
        CONSTRAINT FK_RefreshTokens_LoginHistory FOREIGN KEY (LoginHistoryId)
            REFERENCES dbo.UserLoginHistory(LoginHistoryId)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RefreshTokens_Token' AND object_id = OBJECT_ID(N'dbo.RefreshTokens'))
    CREATE UNIQUE INDEX IX_RefreshTokens_Token ON dbo.RefreshTokens(Token);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_RefreshTokens_UserId_Active' AND object_id = OBJECT_ID(N'dbo.RefreshTokens'))
    CREATE INDEX IX_RefreshTokens_UserId_Active ON dbo.RefreshTokens(UserId, RevokedDate, ExpiryDate);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_UserLoginHistory_UserId' AND object_id = OBJECT_ID(N'dbo.UserLoginHistory'))
    CREATE INDEX IX_UserLoginHistory_UserId ON dbo.UserLoginHistory(UserId, LoginDate);
GO

IF EXISTS
(
    SELECT 1
    FROM (VALUES (287, 2, N'Admin'), (10, 10, N'NormalUser')) AS seed(EmployeeId, RoleId, Username)
    LEFT JOIN dbo.Employees e ON e.EmployeeId = seed.EmployeeId
    LEFT JOIN dbo.Roles r ON r.RoleId = seed.RoleId
    WHERE e.EmployeeId IS NULL OR e.IsActive = 0 OR r.RoleId IS NULL OR r.IsActive = 0
)
    THROW 51000, 'Cannot seed users: a specified employee or active role is missing.', 1;
GO

IF EXISTS
(
    SELECT 1
    FROM (VALUES (287, N'Admin'), (10, N'NormalUser')) AS seed(EmployeeId, Username)
    INNER JOIN dbo.Users u
        ON (u.EmployeeId = seed.EmployeeId AND u.Username <> seed.Username)
        OR (u.Username = seed.Username AND u.EmployeeId <> seed.EmployeeId)
)
    THROW 51001, 'Cannot seed users: a username or employee is already assigned to a different account.', 1;
GO

INSERT INTO dbo.Users
    (EmployeeId, Username, Email, IsActive, CreatedDate)
SELECT
    seed.EmployeeId,
    seed.Username,
    e.Email,
    1,
    SYSUTCDATETIME()
FROM
(
    VALUES
        (287, N'Admin'),
        (10, N'NormalUser')
) AS seed(EmployeeId, Username)
INNER JOIN dbo.Employees e ON e.EmployeeId = seed.EmployeeId
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.Users u
    WHERE u.EmployeeId = seed.EmployeeId OR u.Username = seed.Username
);
GO

/* Seed the sign-in accounts' roles into the authoritative UserRoles junction.
   Guarded: only runs when dbo.UserRoles (created by rbac-schema.sql) already exists. */
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
