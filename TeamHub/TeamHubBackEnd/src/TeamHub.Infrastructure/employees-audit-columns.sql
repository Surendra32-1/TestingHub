/*
    Employees audit-column alignment for TeamHub.

    The Employees table's audit columns drifted from every other table (and from the shared
    AuditableEntity<string> contract): they were CreatedBy INT / ModifiedBy INT / ModifiedDate,
    whereas the rest of the schema (and the application) use
        CreatedBy VARCHAR(100), LastModifiedBy VARCHAR(100), LastModifiedDate DATETIME2.

    Idempotent and safe to re-run:
      1. Widen Employees.CreatedBy from INT to VARCHAR(100) (dropping any default first).
      2. Rename ModifiedBy -> LastModifiedBy and widen it to VARCHAR(100).
      3. Rename ModifiedDate -> LastModifiedDate.

    Apply with a runner that executes each GO batch separately (SqlClient cannot parse GO).
*/

/* ---- 1. CreatedBy INT -> VARCHAR(100) ---- */
IF EXISTS (
    SELECT 1 FROM sys.columns c
    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.Employees') AND c.name = N'CreatedBy' AND t.name = N'int')
BEGIN
    DECLARE @dfCreatedBy NVARCHAR(258);
    SELECT @dfCreatedBy = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Employees') AND c.name = N'CreatedBy';
    IF @dfCreatedBy IS NOT NULL
        EXEC(N'ALTER TABLE dbo.Employees DROP CONSTRAINT [' + @dfCreatedBy + N']');
    ALTER TABLE dbo.Employees ALTER COLUMN CreatedBy VARCHAR(100) NULL;
END
GO

/* ---- 2a. Rename ModifiedBy -> LastModifiedBy ---- */
IF COL_LENGTH(N'dbo.Employees', N'ModifiedBy') IS NOT NULL AND COL_LENGTH(N'dbo.Employees', N'LastModifiedBy') IS NULL
    EXEC(N'sp_rename ''dbo.Employees.ModifiedBy'', ''LastModifiedBy'', ''COLUMN''');
GO

/* ---- 2b. LastModifiedBy INT -> VARCHAR(100) ---- */
IF EXISTS (
    SELECT 1 FROM sys.columns c
    INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
    WHERE c.object_id = OBJECT_ID(N'dbo.Employees') AND c.name = N'LastModifiedBy' AND t.name = N'int')
BEGIN
    DECLARE @dfModifiedBy NVARCHAR(258);
    SELECT @dfModifiedBy = dc.name
    FROM sys.default_constraints dc
    INNER JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
    WHERE dc.parent_object_id = OBJECT_ID(N'dbo.Employees') AND c.name = N'LastModifiedBy';
    IF @dfModifiedBy IS NOT NULL
        EXEC(N'ALTER TABLE dbo.Employees DROP CONSTRAINT [' + @dfModifiedBy + N']');
    ALTER TABLE dbo.Employees ALTER COLUMN LastModifiedBy VARCHAR(100) NULL;
END
GO

/* ---- 3. Rename ModifiedDate -> LastModifiedDate ---- */
IF COL_LENGTH(N'dbo.Employees', N'ModifiedDate') IS NOT NULL AND COL_LENGTH(N'dbo.Employees', N'LastModifiedDate') IS NULL
    EXEC(N'sp_rename ''dbo.Employees.ModifiedDate'', ''LastModifiedDate'', ''COLUMN''');
GO

/* ---- Validation (run after applying): expect CreatedBy/LastModifiedBy varchar(100), LastModifiedDate datetime2 ---- */
SELECT c.name AS column_name, t.name AS type_name, c.max_length
FROM sys.columns c
INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID(N'dbo.Employees')
  AND c.name IN (N'CreatedBy', N'CreatedDate', N'LastModifiedBy', N'LastModifiedDate')
ORDER BY c.name;
GO
