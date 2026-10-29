/*
    Employees.BusinessUnitId for TeamHub.

    Adds the missing business-unit link on an employee, in three idempotent steps:
      1. the nullable column itself,
      2. its foreign key to dbo.BusinessUnits,
      3. a lookup index covering the soft-delete/active flags used by every listing query.

    The column is nullable so the script can run against a populated Employees table;
    a business unit is optional on an employee, exactly like RecruitmentId.

    Idempotent and safe to re-run. Apply with a runner that executes each GO batch
    separately (SqlClient cannot parse GO), e.g.
        sqlcmd -S ".\SQLEXPRESS" -d TeamHub -E -C -b -i employees-businessunit-column.sql
*/

/* ---- 1. the column ---- */
IF COL_LENGTH(N'dbo.Employees', N'BusinessUnitId') IS NULL
    ALTER TABLE dbo.Employees ADD BusinessUnitId INT NULL;
GO

/* ---- 2. the foreign key ---- */
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_Employees_BusinessUnit')
BEGIN
    ALTER TABLE dbo.Employees
        ADD CONSTRAINT FK_Employees_BusinessUnit
            FOREIGN KEY (BusinessUnitId)
            REFERENCES dbo.BusinessUnits(BusinessUnitId);
END
GO

/* ---- 3. the lookup index ---- */
IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE name = N'IX_Employees_BusinessUnitId'
        AND object_id = OBJECT_ID(N'dbo.Employees')
)
    CREATE INDEX IX_Employees_BusinessUnitId
        ON dbo.Employees (BusinessUnitId)
        INCLUDE (IsActive, IsDeleted);
GO
