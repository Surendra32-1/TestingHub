/* EmployeeEducation and EmployeeTraining tables + seed
 * Target: SQL Server (masterDB)
 * Run in DBeaver, one section at a time (each batch ends with GO).
 * Idempotent: safe to re-run.
 */

/* 1. EmployeeEducation -------------------------------------------------- */
IF OBJECT_ID('dbo.EmployeeEducation', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EmployeeEducation] (
        [EducationId]     INT           IDENTITY(1,1) NOT NULL,
        [EmployeeId]      INT           NOT NULL,
        [Qualification]   NVARCHAR(200) NOT NULL,
        [BoardUniversity] NVARCHAR(200) NULL,
        [Institution]     NVARCHAR(200) NULL,
        [Faculty]         NVARCHAR(100) NULL,
        [PassedYear]      INT           NULL,
        [JoinedDate]      DATE          NULL,
        [PassoutDate]     DATE          NULL,
        [JoinedDateBS]    NVARCHAR(10)  NULL,
        [PassoutDateBS]   NVARCHAR(10)  NULL,
        [DivisionGrade]   NVARCHAR(100) NULL,
        [Remarks]         NVARCHAR(MAX) NULL,
        [CreatedDate]     DATETIME      NULL,
        [CreatedBy]       NVARCHAR(100) NULL,
        [LastModifiedDate] DATETIME     NULL,
        [LastModifiedBy]  NVARCHAR(100) NULL,
        CONSTRAINT [PK_EmployeeEducation] PRIMARY KEY CLUSTERED ([EducationId] ASC),
        CONSTRAINT [FK_EmployeeEducation_Employees]
            FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees] ([EmployeeId]) ON DELETE CASCADE
    );
END
GO

CREATE NONCLUSTERED INDEX [IX_EmployeeEducation_EmployeeId]
    ON [dbo].[EmployeeEducation] ([EmployeeId] ASC)
    WHERE NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_EmployeeEducation_EmployeeId'
          AND object_id = OBJECT_ID('dbo.EmployeeEducation'));
GO

/* 2. EmployeeTraining -------------------------------------------------- */
IF OBJECT_ID('dbo.EmployeeTraining', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[EmployeeTraining] (
        [TrainingId]       INT           IDENTITY(1,1) NOT NULL,
        [EmployeeId]       INT           NOT NULL,
        [TrainingTitle]    NVARCHAR(200) NOT NULL,
        [TrainingProvider] NVARCHAR(200) NULL,
        [TrainingType]     NVARCHAR(100) NULL,
        [TrainingLocation] NVARCHAR(200) NULL,
        [JoinedDate]       DATE          NULL,
        [PassoutDate]      DATE          NULL,
        [JoinedDateBS]     NVARCHAR(10)  NULL,
        [PassoutDateBS]    NVARCHAR(10)  NULL,
        [Duration]         NVARCHAR(100) NULL,
        [CertificateNo]    NVARCHAR(100) NULL,
        [Remarks]          NVARCHAR(MAX) NULL,
        [CreatedDate]      DATETIME      NULL,
        [CreatedBy]        NVARCHAR(100) NULL,
        [LastModifiedDate] DATETIME      NULL,
        [LastModifiedBy]   NVARCHAR(100) NULL,
        CONSTRAINT [PK_EmployeeTraining] PRIMARY KEY CLUSTERED ([TrainingId] ASC),
        CONSTRAINT [FK_EmployeeTraining_Employees]
            FOREIGN KEY ([EmployeeId]) REFERENCES [dbo].[Employees] ([EmployeeId]) ON DELETE CASCADE
    );
END
GO

CREATE NONCLUSTERED INDEX [IX_EmployeeTraining_EmployeeId]
    ON [dbo].[EmployeeTraining] ([EmployeeId] ASC)
    WHERE NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_EmployeeTraining_EmployeeId'
          AND object_id = OBJECT_ID('dbo.EmployeeTraining'));
GO

/* 2b. Add B.S. (Nepali calendar) columns when tables already exist from a
 *     earlier run of this script (idempotent).                          */
IF COL_LENGTH('dbo.EmployeeEducation', 'JoinedDateBS') IS NULL
    ALTER TABLE dbo.EmployeeEducation ADD JoinedDateBS NVARCHAR(10) NULL;
GO
IF COL_LENGTH('dbo.EmployeeEducation', 'PassoutDateBS') IS NULL
    ALTER TABLE dbo.EmployeeEducation ADD PassoutDateBS NVARCHAR(10) NULL;
GO
IF COL_LENGTH('dbo.EmployeeTraining', 'JoinedDateBS') IS NULL
    ALTER TABLE dbo.EmployeeTraining ADD JoinedDateBS NVARCHAR(10) NULL;
GO
IF COL_LENGTH('dbo.EmployeeTraining', 'PassoutDateBS') IS NULL
    ALTER TABLE dbo.EmployeeTraining ADD PassoutDateBS NVARCHAR(10) NULL;
GO

/* 3. Seed: one education + one training row for every existing employee.
 *    Idempotent via NOT EXISTS so re-running does not duplicate.          */
INSERT INTO dbo.EmployeeEducation
    (EmployeeId, Qualification, BoardUniversity, Institution, Faculty,
     PassedYear, JoinedDate, PassoutDate, DivisionGrade, Remarks, CreatedDate, CreatedBy)
SELECT
    e.EmployeeId,
    N'Bachelor of Science',
    N'Tribhuvan University',
    N'Example Campus',
    N'Computer Science & Engineering',
    2020,
    CAST('2017-06-01' AS date),
    CAST('2021-06-01' AS date),
    N'First Division',
    N'Seeded sample education record',
    SYSUTCDATETIME(),
    N'Seed'
FROM dbo.Employees e
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.EmployeeEducation ee WHERE ee.EmployeeId = e.EmployeeId);
GO

INSERT INTO dbo.EmployeeTraining
    (EmployeeId, TrainingTitle, TrainingProvider, TrainingType, TrainingLocation,
     JoinedDate, PassoutDate, Duration, CertificateNo, Remarks, CreatedDate, CreatedBy)
SELECT
    e.EmployeeId,
    N'Software Development Bootcamp',
    N'Cotivity Training Center',
    N'In-house',
    N'Kathmandu',
    CAST('2022-01-10' AS date),
    CAST('2022-03-10' AS date),
    N'2 months',
    N'TRN-2022-0001',
    N'Seeded sample training record',
    SYSUTCDATETIME(),
    N'Seed'
FROM dbo.Employees e
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.EmployeeTraining et WHERE et.EmployeeId = e.EmployeeId);
GO

/* 4. Verify
SELECT 'EmployeeEducation' AS TableName, COUNT(*) AS RowCount FROM dbo.EmployeeEducation
UNION ALL
SELECT 'EmployeeTraining', COUNT(*) FROM dbo.EmployeeTraining;
*/
