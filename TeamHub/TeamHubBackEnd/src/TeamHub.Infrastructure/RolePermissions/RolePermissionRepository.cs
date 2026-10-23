using Dapper;
using TeamHub.Application.Common;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.Rbac;
using TeamHub.Infrastructure.Database;

namespace TeamHub.Infrastructure.RolePermissions;

public sealed class RolePermissionRepository(IDbConnectionFactory connectionFactory) : IRolePermissionRepository
{
    public async Task<RolePermissionMatrixDto> GetMatrixAsync(int pageNumber, int pageSize, string? search, CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var searchTerm = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        const string rolesSql = @"
            SELECT RoleId, RoleName
            FROM dbo.Roles
            WHERE IsActive = 1
            ORDER BY RoleName;";

        const string countSql = @"
            SELECT COUNT(1)
            FROM dbo.Permissions
            WHERE IsActive = 1
              AND (@Search IS NULL OR LOWER(PermissionName) LIKE '%' + LOWER(@Search) + '%');";

        const string pageSql = @"
            SELECT p.PermissionId,
                   p.PermissionCode,
                   p.PermissionName,
                   p.Module
            FROM dbo.Permissions p
            WHERE p.IsActive = 1
              AND (@Search IS NULL OR LOWER(p.PermissionName) LIKE '%' + LOWER(@Search) + '%')
            ORDER BY p.PermissionName
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        const string rolesForPageSql = @"
            SELECT rp.PermissionId, rp.RoleId
            FROM dbo.RolePermissions rp
            INNER JOIN dbo.Roles r ON r.RoleId = rp.RoleId AND r.IsActive = 1
            WHERE rp.PermissionId IN @PermissionIds;";

        using var connection = connectionFactory.CreateConnection();
        var roles = (await connection.QueryAsync<RoleOptionDto>(
            new CommandDefinition(rolesSql, cancellationToken: cancellationToken))).AsList();

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, new { Search = searchTerm }, cancellationToken: cancellationToken));
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        pageNumber = totalPages == 0 ? 1 : Math.Min(pageNumber, totalPages);
        var offset = (pageNumber - 1) * pageSize;

        var permissions = (await connection.QueryAsync<RolePermissionRow>(
            new CommandDefinition(pageSql, new { Offset = offset, PageSize = pageSize, Search = searchTerm }, cancellationToken: cancellationToken))).AsList();

        var permissionIds = permissions.Select(permission => permission.PermissionId).ToList();
        var assignments = new List<RolePermissionRow>();
        if (permissionIds.Count > 0)
        {
            assignments = (await connection.QueryAsync<RolePermissionRow>(
                new CommandDefinition(rolesForPageSql, new { PermissionIds = permissionIds }, cancellationToken: cancellationToken))).AsList();
        }
        var roleIdsByPermission = assignments
            .Where(assignment => assignment.RoleId.HasValue)
            .GroupBy(assignment => assignment.PermissionId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(assignment => assignment.RoleId!.Value).Distinct().ToList());

        var rows = permissions.Select(permission => new RolePermissionMatrixRowDto
        {
            PermissionId = permission.PermissionId,
            PermissionCode = permission.PermissionCode,
            PermissionName = permission.PermissionName,
            Module = permission.Module,
            RoleIds = roleIdsByPermission.TryGetValue(permission.PermissionId, out var assigned) ? assigned : []
        }).ToList();

        return new RolePermissionMatrixDto
        {
            Roles = roles,
            Rows = rows,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task SaveAsync(IReadOnlyList<RolePermissionAssignmentDto> assignments, string modifiedBy, CancellationToken cancellationToken = default)
    {
        if (assignments is null || assignments.Count == 0)
        {
            return;
        }

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        foreach (var assignment in assignments)
        {
            var permissionExists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                @"SELECT COUNT(1) FROM dbo.Permissions WHERE PermissionId = @PermissionId AND IsActive = 1;",
                new { assignment.PermissionId }, transaction, cancellationToken: cancellationToken));
            if (permissionExists != 1)
            {
                throw new RbacValidationException($"Permission id {assignment.PermissionId} does not exist or is inactive.");
            }

            var distinctRoleIds = assignment.RoleIds.Distinct().ToList();
            if (distinctRoleIds.Count > 0)
            {
                var validRoleCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                    @"SELECT COUNT(1) FROM dbo.Roles WHERE IsActive = 1 AND RoleId IN @RoleIds;",
                    new { RoleIds = distinctRoleIds }, transaction, cancellationToken: cancellationToken));
                if (validRoleCount != distinctRoleIds.Count)
                {
                    throw new RbacValidationException($"One or more role ids for permission {assignment.PermissionId} do not exist or are inactive.");
                }
            }

            await connection.ExecuteAsync(new CommandDefinition(
                @"DELETE FROM dbo.RolePermissions WHERE PermissionId = @PermissionId;",
                new { assignment.PermissionId }, transaction, cancellationToken: cancellationToken));

            foreach (var roleId in distinctRoleIds)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    @"INSERT INTO dbo.RolePermissions (RoleId, PermissionId, CreatedBy, CreatedDate)
                      VALUES (@RoleId, @PermissionId, @ModifiedBy, SYSUTCDATETIME());",
                    new { RoleId = roleId, assignment.PermissionId, ModifiedBy = modifiedBy }, transaction, cancellationToken: cancellationToken));
            }
        }

        transaction.Commit();
    }

    private sealed class RolePermissionRow
    {
        public int PermissionId { get; set; }
        public string PermissionCode { get; set; } = string.Empty;
        public string PermissionName { get; set; } = string.Empty;
        public string? Module { get; set; }
        public int? RoleId { get; set; }
    }
}
