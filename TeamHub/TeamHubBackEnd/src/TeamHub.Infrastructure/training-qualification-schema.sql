IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Training')
BEGIN
    CREATE TABLE [dbo].[Training]
    (
        TrainingId       INT IDENTITY(1,1) PRIMARY KEY,
        TrainingName     NVARCHAR(200) NOT NULL UNIQUE,
        CreatedBy        NVARCHAR(100) NULL,
        CreatedDate      DATETIME2     NULL,
        LastModifiedBy   NVARCHAR(100) NULL,
        LastModifiedDate DATETIME2     NULL,
        IsActive         BIT NOT NULL DEFAULT(1)
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'EducationalQualification')
BEGIN
    CREATE TABLE [dbo].[EducationalQualification]
    (
        QualificationId    INT IDENTITY(1,1) PRIMARY KEY,
        QualificationName  NVARCHAR(200) NOT NULL UNIQUE,
        CreatedBy          NVARCHAR(100) NULL,
        CreatedDate        DATETIME2     NULL,
        LastModifiedBy     NVARCHAR(100) NULL,
        LastModifiedDate   DATETIME2     NULL,
        IsActive           BIT NOT NULL DEFAULT(1)
    );
END
GO

INSERT INTO dbo.EducationalQualification (QualificationName, IsActive, CreatedBy, CreatedDate)
SELECT v.QualificationName, 1, 'seed', SYSUTCDATETIME()
FROM (VALUES
    ('SLC / SEE'), ('+2 / Intermediate'), ('Diploma'),
    ('Bachelor'), ('Postgraduate Diploma'), ('Master'), ('MPhil'), ('PhD')
) v(QualificationName)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.EducationalQualification q WHERE q.QualificationName = v.QualificationName);
GO

INSERT INTO dbo.Training (TrainingName, IsActive, CreatedBy, CreatedDate)
SELECT v.TrainingName, 1, 'seed', SYSUTCDATETIME()
FROM (VALUES
    ('Onboarding / Induction'), ('Workplace Safety'), ('First Aid'),
    ('Communication Skills'), ('Leadership Development'), ('Project Management'),
    ('Technical Workshop'), ('Compliance & Ethics')
) v(TrainingName)
WHERE NOT EXISTS (
    SELECT 1 FROM dbo.Training t WHERE t.TrainingName = v.TrainingName);
GO
