using Dapper;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.Employee;
using TeamHub.Infrastructure.Database;
using TeamHub.Shared.Common.Services;

namespace TeamHub.Infrastructure.Employees;

public sealed class EmployeeSearchRepository(IDbConnectionFactory connectionFactory) : IEmployeeSearchRepository
{
    public async Task<IReadOnlyList<EmployeeSearchCardDto>> SearchActiveAsync(
        EmployeeSearchFilter filter,
        CancellationToken cancellationToken = default)
    {
        var name = Normalize(filter.Name);
        var designation = Normalize(filter.Designation);
        var contact = Normalize(filter.Contact);

        const string sql = @"
            SELECT
                e.EmployeeId AS Id,
                e.ENumber,
                CONCAT_WS(' ', e.FirstName, e.MiddleName, e.LastName) AS Name,
                dsg.DesignationName AS Designation,
                d.DepartmentName AS Department,
                t.TeamName,
                bu.BusinessUnitName,
                ISNULL(e.Email, '') AS Email,
                e.PhoneNumber,
                ISNULL(e.ExtensionNumber, 0) AS ExtensionNumber,
                e.MobileNumber,
                e.StationNumber,
                CONCAT_WS(' ', reports.FirstName, reports.MiddleName, reports.LastName) AS ReportsToEmployee,
                image.ImagePath
            FROM Employees e
            LEFT JOIN Departments d ON d.DepartmentId = e.DepartmentId
            LEFT JOIN Designations dsg ON dsg.DesignationId = e.DesignationId
            LEFT JOIN Teams t ON t.TeamId = e.TeamId
            LEFT JOIN BusinessUnits bu ON bu.BusinessUnitId = e.BusinessUnitId
            LEFT JOIN Employees reports ON reports.EmployeeId = e.ReportsToEmployeeId AND reports.IsDeleted = 0
            OUTER APPLY (
                SELECT TOP (1) ImagePath
                FROM EmployeeImages
                WHERE EmployeeId = e.EmployeeId
                ORDER BY ImageId DESC
            ) image
            WHERE e.IsActive = 1 AND e.IsDeleted = 0
                AND (@Name IS NULL
                    OR LOWER(CONCAT_WS(' ', e.FirstName, e.MiddleName, e.LastName)) LIKE '%' + LOWER(@Name) + '%')
                AND (@Designation IS NULL OR LOWER(dsg.DesignationName) LIKE '%' + LOWER(@Designation) + '%')
                AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
                AND (@Contact IS NULL
                    OR LOWER(ISNULL(e.Email, '')) LIKE '%' + LOWER(@Contact) + '%'
                    OR LOWER(ISNULL(e.MobileNumber, '')) LIKE '%' + LOWER(@Contact) + '%'
                    OR LOWER(ISNULL(e.PhoneNumber, '')) LIKE '%' + LOWER(@Contact) + '%'
                    OR CAST(e.ENumber AS nvarchar(20)) LIKE '%' + LOWER(@Contact) + '%'
                    OR CAST(ISNULL(e.ExtensionNumber, 0) AS nvarchar(20)) LIKE '%' + LOWER(@Contact) + '%')
            ORDER BY e.FirstName, e.LastName, e.EmployeeId;
            ";

        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<EmployeeSearchRow>(new CommandDefinition(
            sql,
            new { Name = name, Designation = designation, filter.DepartmentId, Contact = contact },
            cancellationToken: cancellationToken));

        return rows
            .Select(row => new EmployeeSearchCardDto
            {
                Id = row.Id,
                ENumber = row.ENumber,
                Name = row.Name,
                Designation = row.Designation,
                Department = row.Department,
                TeamName = row.TeamName,
                BusinessUnitName = row.BusinessUnitName,
                Email = row.Email,
                PhoneNumber = row.PhoneNumber,
                ExtensionNumber = row.ExtensionNumber,
                MobileNumber = row.MobileNumber,
                StationNumber = row.StationNumber,
                ReportsToEmployee = string.IsNullOrWhiteSpace(row.ReportsToEmployee) ? null : row.ReportsToEmployee,
                ImageBase64 = ToBase64DataUrl(row.ImagePath)
            })
            .ToList();
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private sealed class EmployeeSearchRow
    {
        public int Id { get; set; }
        public int ENumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Designation { get; set; }
        public string? Department { get; set; }
        public string? TeamName { get; set; }
        public string? BusinessUnitName { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public int ExtensionNumber { get; set; }
        public string? MobileNumber { get; set; }
        public string? StationNumber { get; set; }
        public string? ReportsToEmployee { get; set; }
        public string? ImagePath { get; set; }
    }

    private static string? ToBase64DataUrl(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return null;
        }

        try
        {
            var relativePath = imagePath.Replace('\\', '/').TrimStart('/');
            var base64 = FileHelper.GetBase64StringFromFile(relativePath);
            var extension = Path.GetExtension(relativePath);
            var mimeType = extension.ToLowerInvariant() switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };

            return $"data:{mimeType};base64,{base64}";
        }
        catch
        {
            return null;
        }
    }
}
