IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.Employees')
      AND name = N'IX_Employees_ReportsToEmployeeId'
)
BEGIN
    CREATE INDEX IX_Employees_ReportsToEmployeeId
        ON dbo.Employees (ReportsToEmployeeId)
        INCLUDE (EmployeeId, FirstName, MiddleName, LastName, DesignationId, DepartmentId, ENumber, CotivityEmployeeId);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.CotivityEmployees')
      AND name = N'IX_CotivityEmployees_ReportsToCotivityEmployeeId'
)
BEGIN
    CREATE INDEX IX_CotivityEmployees_ReportsToCotivityEmployeeId
        ON dbo.CotivityEmployees (ReportsToCotivityEmployeeId)
        INCLUDE (CotivityEmployeeId, CotivityEmployeeName, CotivityDepartmentId, CotivityDepartmentDesignationId);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.EmployeeImages')
      AND name = N'IX_EmployeeImages_EmployeeId_ImageId'
)
BEGIN
    CREATE INDEX IX_EmployeeImages_EmployeeId_ImageId
        ON dbo.EmployeeImages (EmployeeId, ImageId DESC)
        INCLUDE (ImagePath);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.indexes
    WHERE object_id = OBJECT_ID(N'dbo.CotivityEmployeeImages')
      AND name = N'IX_CotivityEmployeeImages_EmployeeId_ImageId'
)
BEGIN
    CREATE INDEX IX_CotivityEmployeeImages_EmployeeId_ImageId
        ON dbo.CotivityEmployeeImages (CotivityEmployeeId, CotivityImageId DESC)
        INCLUDE (CotivityImagePath);
END;
GO

CREATE OR ALTER PROCEDURE dbo.sp_GetEmployeeOrganizationHierarchy
    @EmployeeId INT
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Managers AS
    (
        SELECT
            e.EmployeeId AS EmployeeId,
            e.ReportsToEmployeeId AS ParentId,
            CAST('/' + CAST(e.EmployeeId AS VARCHAR(20)) + '/' AS VARCHAR(MAX)) AS VisitedPath,
            0 AS Level
        FROM dbo.Employees e
        WHERE e.EmployeeId = @EmployeeId

        UNION ALL

        SELECT
            manager.EmployeeId,
            manager.ReportsToEmployeeId,
            CAST(chain.VisitedPath + CAST(manager.EmployeeId AS VARCHAR(20)) + '/' AS VARCHAR(MAX)),
            chain.Level - 1
        FROM dbo.Employees manager
        INNER JOIN Managers chain ON manager.EmployeeId = chain.ParentId
        WHERE chain.VisitedPath NOT LIKE '%/' + CAST(manager.EmployeeId AS VARCHAR(20)) + '/%'
    )
    SELECT
        employee.EmployeeId,
        managerIds.ParentId,
        CONCAT_WS(' ', employee.FirstName, employee.MiddleName, employee.LastName) AS EmployeeName,
        designation.DesignationName,
        department.DepartmentName,
        image.ImagePath,
        employee.ENumber AS EmployeeNumber,
        CAST('Employee' AS VARCHAR(20)) AS Source,
        managerIds.Level
    FROM Managers managerIds
    INNER JOIN dbo.Employees employee ON employee.EmployeeId = managerIds.EmployeeId
    LEFT JOIN dbo.Designations designation ON designation.DesignationId = employee.DesignationId
    LEFT JOIN dbo.Departments department ON department.DepartmentId = employee.DepartmentId
    OUTER APPLY (
        SELECT TOP (1) ei.ImagePath
        FROM dbo.EmployeeImages ei
        WHERE ei.EmployeeId = employee.EmployeeId
        ORDER BY ei.ImageId DESC
    ) image
    WHERE managerIds.Level < 0
    ORDER BY managerIds.Level
    OPTION (MAXRECURSION 32767);

    SELECT
        e.EmployeeId AS EmployeeId,
        e.ReportsToEmployeeId AS ParentId,
        CONCAT_WS(' ', e.FirstName, e.MiddleName, e.LastName) AS EmployeeName,
        dsg.DesignationName AS DesignationName,
        d.DepartmentName AS DepartmentName,
        image.ImagePath,
        e.ENumber AS EmployeeNumber,
        CAST('Employee' AS VARCHAR(20)) AS Source,
        0 AS Level
    FROM dbo.Employees e
    LEFT JOIN dbo.Designations dsg ON dsg.DesignationId = e.DesignationId
    LEFT JOIN dbo.Departments d ON d.DepartmentId = e.DepartmentId
    OUTER APPLY (
        SELECT TOP (1) ei.ImagePath
        FROM dbo.EmployeeImages ei
        WHERE ei.EmployeeId = e.EmployeeId
        ORDER BY ei.ImageId DESC
    ) image
    WHERE e.EmployeeId = @EmployeeId;

    ;WITH CotivityManagers AS
    (
        SELECT
            ce.CotivityEmployeeId AS CotivityEmployeeId,
            ce.ReportsToCotivityEmployeeId AS ParentId,
            CAST('/' + CAST(ce.CotivityEmployeeId AS VARCHAR(20)) + '/' AS VARCHAR(MAX)) AS VisitedPath,
            0 AS Level
        FROM dbo.Employees e
        INNER JOIN dbo.CotivityEmployees ce ON ce.CotivityEmployeeId = e.CotivityEmployeeId
        WHERE e.EmployeeId = @EmployeeId

        UNION ALL

        SELECT
            manager.CotivityEmployeeId,
            manager.ReportsToCotivityEmployeeId,
            CAST(chain.VisitedPath + CAST(manager.CotivityEmployeeId AS VARCHAR(20)) + '/' AS VARCHAR(MAX)),
            chain.Level - 1
        FROM dbo.CotivityEmployees manager
        INNER JOIN CotivityManagers chain ON manager.CotivityEmployeeId = chain.ParentId
        WHERE chain.VisitedPath NOT LIKE '%/' + CAST(manager.CotivityEmployeeId AS VARCHAR(20)) + '/%'
    )
    SELECT
        cotivityEmployee.CotivityEmployeeId,
        managerIds.ParentId,
        cotivityEmployee.CotivityEmployeeName,
        designation.CotivityDesignationName,
        department.CotivityDepartmentName,
        image.CotivityImagePath AS ImagePath,
        CAST('Cotivity' AS VARCHAR(20)) AS Source,
        managerIds.Level
    FROM CotivityManagers managerIds
    INNER JOIN dbo.CotivityEmployees cotivityEmployee
        ON cotivityEmployee.CotivityEmployeeId = managerIds.CotivityEmployeeId
    LEFT JOIN dbo.CotivityDesignations designation
        ON designation.CotivityDesignationId = cotivityEmployee.CotivityDepartmentDesignationId
    LEFT JOIN dbo.CotivityDepartments department
        ON department.CotivityDepartmentId = cotivityEmployee.CotivityDepartmentId
    OUTER APPLY (
        SELECT TOP (1) cei.CotivityImagePath
        FROM dbo.CotivityEmployeeImages cei
        WHERE cei.CotivityEmployeeId = cotivityEmployee.CotivityEmployeeId
        ORDER BY cei.CotivityImageId DESC
    ) image
    ORDER BY managerIds.Level
    OPTION (MAXRECURSION 32767);

    ;WITH Reportees AS
    (
        SELECT
            e.EmployeeId AS EmployeeId,
            e.ReportsToEmployeeId AS ParentId,
            CAST('/' + CAST(e.EmployeeId AS VARCHAR(20)) + '/' AS VARCHAR(MAX)) AS VisitedPath,
            0 AS Level
        FROM dbo.Employees e
        WHERE e.EmployeeId = @EmployeeId

        UNION ALL

        SELECT
            report.EmployeeId,
            report.ReportsToEmployeeId,
            CAST(chain.VisitedPath + CAST(report.EmployeeId AS VARCHAR(20)) + '/' AS VARCHAR(MAX)),
            chain.Level + 1
        FROM dbo.Employees report
        INNER JOIN Reportees chain ON report.ReportsToEmployeeId = chain.EmployeeId
        WHERE chain.VisitedPath NOT LIKE '%/' + CAST(report.EmployeeId AS VARCHAR(20)) + '/%'
    )
    SELECT
        employee.EmployeeId,
        reportIds.ParentId,
        CONCAT_WS(' ', employee.FirstName, employee.MiddleName, employee.LastName) AS EmployeeName,
        designation.DesignationName,
        department.DepartmentName,
        image.ImagePath,
        employee.ENumber AS EmployeeNumber,
        CAST('Employee' AS VARCHAR(20)) AS Source,
        reportIds.Level
    FROM Reportees reportIds
    INNER JOIN dbo.Employees employee ON employee.EmployeeId = reportIds.EmployeeId
    LEFT JOIN dbo.Designations designation ON designation.DesignationId = employee.DesignationId
    LEFT JOIN dbo.Departments department ON department.DepartmentId = employee.DepartmentId
    OUTER APPLY (
        SELECT TOP (1) ei.ImagePath
        FROM dbo.EmployeeImages ei
        WHERE ei.EmployeeId = employee.EmployeeId
        ORDER BY ei.ImageId DESC
    ) image
    ORDER BY reportIds.Level, EmployeeName, employee.EmployeeId
    OPTION (MAXRECURSION 32767);
END;
GO
