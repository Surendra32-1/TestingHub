using Dapper;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.CotivityEmployee;
using TeamHub.Domain.Entities;
using TeamHub.Infrastructure.Database;
using TeamHub.Shared.Common.Services;

namespace TeamHub.Infrastructure.CotivityEmployees;

public sealed class CotivityEmployeeRepository(IDbConnectionFactory connectionFactory) : ICotivityEmployeeRepository
{
    public async Task<IReadOnlyList<GetCotivityEmployeeDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        const string sql =@"
            SELECT ce.CotivityEmployeeId, ce.CotivityEmployeeName, ce.CotivityDepartmentId,
                   cd.CotivityDepartmentName, ce.CotivityDepartmentDesignationId,
                   cdes.CotivityDesignationName AS CotivityDepartmentDesignationName,
                     ce.ReportsToCotivityEmployeeId, reporting.CotivityEmployeeName AS ReportsToCotivityEmployeeName,
                     ce.IsActive, image.CotivityImagePath AS ImagePath
            FROM CotivityEmployees ce
            LEFT JOIN CotivityDepartments cd ON cd.CotivityDepartmentId = ce.CotivityDepartmentId
            LEFT JOIN CotivityDesignations cdes ON cdes.CotivityDesignationId = ce.CotivityDepartmentDesignationId
                 LEFT JOIN CotivityEmployees reporting ON reporting.CotivityEmployeeId = ce.ReportsToCotivityEmployeeId
            OUTER APPLY (
                SELECT TOP (1) CotivityImagePath
                FROM CotivityEmployeeImages
                WHERE CotivityEmployeeId = ce.CotivityEmployeeId
                ORDER BY CotivityImageId DESC
            ) image
            ORDER BY ce.CotivityEmployeeName;
            ";
        using var connection = connectionFactory.CreateConnection();
        var employees = await connection.QueryAsync<CotivityEmployeeRow>(new CommandDefinition(sql, cancellationToken: cancellationToken));
        return employees.Select(Map).ToList();
    }

    public async Task<GetCotivityEmployeeDto?> GetByIdAsync(int cotivityEmployeeId, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            SELECT ce.CotivityEmployeeId, ce.CotivityEmployeeName, ce.CotivityDepartmentId,
                   cd.CotivityDepartmentName, ce.CotivityDepartmentDesignationId,
                   cdes.CotivityDesignationName AS CotivityDepartmentDesignationName,
                     ce.ReportsToCotivityEmployeeId, reporting.CotivityEmployeeName AS ReportsToCotivityEmployeeName,
                     ce.IsActive, image.CotivityImagePath AS ImagePath
            FROM CotivityEmployees ce
            LEFT JOIN CotivityDepartments cd ON cd.CotivityDepartmentId = ce.CotivityDepartmentId
            LEFT JOIN CotivityDesignations cdes ON cdes.CotivityDesignationId = ce.CotivityDepartmentDesignationId
                 LEFT JOIN CotivityEmployees reporting ON reporting.CotivityEmployeeId = ce.ReportsToCotivityEmployeeId
            OUTER APPLY (
                SELECT TOP (1) CotivityImagePath
                FROM CotivityEmployeeImages
                WHERE CotivityEmployeeId = ce.CotivityEmployeeId
                ORDER BY CotivityImageId DESC
            ) image
            WHERE ce.CotivityEmployeeId = @CotivityEmployeeId;
            ";
        using var connection = connectionFactory.CreateConnection();
        var employee = await connection.QuerySingleOrDefaultAsync<CotivityEmployeeRow>(
            new CommandDefinition(sql, new { CotivityEmployeeId = cotivityEmployeeId }, cancellationToken: cancellationToken));
        return employee is null ? null : Map(employee);
    }

    public async Task<int> CreateAsync(CotivityEmployee employee, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            INSERT INTO CotivityEmployees
                (CotivityEmployeeName, CotivityDepartmentId, CotivityDepartmentDesignationId,
                 ReportsToCotivityEmployeeId, IsActive, CreatedDate, CreatedBy)
            OUTPUT INSERTED.CotivityEmployeeId
            VALUES (@CotivityEmployeeName, @CotivityDepartmentId, @CotivityDepartmentDesignationId,
                    @ReportsToCotivityEmployeeId, @IsActive, @CreatedDate, @CreatedBy);
            ";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, employee, cancellationToken: cancellationToken));
    }

    public async Task<int> CreateImageAsync(CotivityEmployeeImage image, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            INSERT INTO CotivityEmployeeImages
                (CotivityEmployeeId, CotivityImagePath, CreatedDate, CreatedBy)
            OUTPUT INSERTED.CotivityImageId
            VALUES (@CotivityEmployeeId, @CotivityImagePath, @CreatedDate, @CreatedBy);
            ";

        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, image, cancellationToken: cancellationToken));
    }

    public async Task<bool> UpdateAsync(CotivityEmployee employee, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            UPDATE CotivityEmployees
            SET CotivityEmployeeName = @CotivityEmployeeName,
                CotivityDepartmentId = @CotivityDepartmentId,
                CotivityDepartmentDesignationId = @CotivityDepartmentDesignationId,
                ReportsToCotivityEmployeeId = @ReportsToCotivityEmployeeId,
                IsActive = @IsActive,
                LastModifiedDate = @LastModifiedDate,
                LastModifiedBy = @LastModifiedBy
            WHERE CotivityEmployeeId = @CotivityEmployeeId;
            ";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, employee, cancellationToken: cancellationToken)) > 0;
    }

    public async Task<bool> DeleteAsync(int cotivityEmployeeId, CancellationToken cancellationToken = default)
    {
        const string sql =@"
            UPDATE CotivityEmployees SET IsActive = 0
            WHERE CotivityEmployeeId = @CotivityEmployeeId;
            ";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteAsync(new CommandDefinition(sql, new { CotivityEmployeeId = cotivityEmployeeId }, cancellationToken: cancellationToken)) > 0;
    }

    private static GetCotivityEmployeeDto Map(CotivityEmployeeRow employee) => new()
    {
        CotivityEmployeeId = employee.CotivityEmployeeId,
        CotivityEmployeeName = employee.CotivityEmployeeName,
        CotivityDepartmentId = employee.CotivityDepartmentId,
        CotivityDepartmentName = employee.CotivityDepartmentName,
        CotivityDepartmentDesignationId = employee.CotivityDepartmentDesignationId,
        CotivityDepartmentDesignationName = employee.CotivityDepartmentDesignationName,
        ReportsToCotivityEmployeeId = employee.ReportsToCotivityEmployeeId,
        ReportsToCotivityEmployeeName = employee.ReportsToCotivityEmployeeName,
        IsActive = employee.IsActive,
        ImageBase64 = ToBase64DataUrl(employee.ImagePath)
    };

    private static string? ToBase64DataUrl(string? imagePath)
    {
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return null;
        }

        var relativePath = imagePath.Replace('\\', '/').TrimStart('/');
        var base64 = FileHelper.GetBase64StringFromFile(relativePath);
        var mimeType = Path.GetExtension(relativePath).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };

        return $"data:{mimeType};base64,{base64}";
    }

    private sealed class CotivityEmployeeRow
    {
        public int CotivityEmployeeId { get; set; }
        public string CotivityEmployeeName { get; set; } = string.Empty;
        public int CotivityDepartmentId { get; set; }
        public string CotivityDepartmentName { get; set; } = string.Empty;
        public int CotivityDepartmentDesignationId { get; set; }
        public string CotivityDepartmentDesignationName { get; set; } = string.Empty;
        public int? ReportsToCotivityEmployeeId { get; set; }
        public string? ReportsToCotivityEmployeeName { get; set; }
        public bool IsActive { get; set; }
        public string? ImagePath { get; set; }
    }
}