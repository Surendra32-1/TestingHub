using Dapper;
using TeamHub.Application.Common;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.Rbac;
using TeamHub.Infrastructure.Database;

namespace TeamHub.Infrastructure.UserRoles;

public sealed class UserRoleRepository(IDbConnectionFactory connectionFactory) : IUserRoleRepository
{
    public async Task<EmployeeRoleMatrixDto> GetMatrixAsync(int pageNumber, int pageSize, string? search, CancellationToken cancellationToken = default)
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
            FROM dbo.Users u
            INNER JOIN dbo.Employees e ON e.EmployeeId = u.EmployeeId
            WHERE u.IsActive = 1 AND e.IsActive = 1
              AND (@Search IS NULL OR LOWER(CONCAT_WS(' ', e.FirstName, e.MiddleName, e.LastName)) LIKE '%' + LOWER(@Search) + '%');";

        const string pageSql = @"
            SELECT u.UserId,
                   e.EmployeeId,
                   u.Username,
                   e.FirstName,
                   e.MiddleName,
                   e.LastName
            FROM dbo.Users u
            INNER JOIN dbo.Employees e ON e.EmployeeId = u.EmployeeId
            WHERE u.IsActive = 1 AND e.IsActive = 1
              AND (@Search IS NULL OR LOWER(CONCAT_WS(' ', e.FirstName, e.MiddleName, e.LastName)) LIKE '%' + LOWER(@Search) + '%')
            ORDER BY e.FirstName, e.LastName, u.Username
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

        const string rolesForPageSql = @"
            SELECT ur.UserId, ur.RoleId
            FROM dbo.UserRoles ur
            INNER JOIN dbo.Roles r ON r.RoleId = ur.RoleId AND r.IsActive = 1
            WHERE ur.UserId IN @UserIds;";

        using var connection = connectionFactory.CreateConnection();
        var roles = (await connection.QueryAsync<RoleOptionDto>(
            new CommandDefinition(rolesSql, cancellationToken: cancellationToken))).AsList();

        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(countSql, new { Search = searchTerm }, cancellationToken: cancellationToken));
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        pageNumber = totalPages == 0 ? 1 : Math.Min(pageNumber, totalPages);
        var offset = (pageNumber - 1) * pageSize;

        var users = (await connection.QueryAsync<UserRoleRow>(
            new CommandDefinition(pageSql, new { Offset = offset, PageSize = pageSize, Search = searchTerm }, cancellationToken: cancellationToken))).AsList();

        var userIds = users.Select(user => user.UserId).ToList();
        var assignments = new List<UserRoleRow>();
        if (userIds.Count > 0)
        {
            assignments = (await connection.QueryAsync<UserRoleRow>(
                new CommandDefinition(rolesForPageSql, new { UserIds = userIds }, cancellationToken: cancellationToken))).AsList();
        }
        var roleIdsByUser = assignments
            .Where(assignment => assignment.RoleId.HasValue)
            .GroupBy(assignment => assignment.UserId)
            .ToDictionary(
                group => group.Key,
                group => group.Select(assignment => assignment.RoleId!.Value).Distinct().ToList());

        var rows = users.Select(user => new EmployeeRoleMatrixRowDto
        {
            UserId = user.UserId,
            EmployeeId = user.EmployeeId,
            Username = user.Username,
            FullName = BuildFullName(user.FirstName, user.MiddleName, user.LastName),
            RoleIds = roleIdsByUser.TryGetValue(user.UserId, out var assigned) ? assigned : []
        }).ToList();

        return new EmployeeRoleMatrixDto
        {
            Roles = roles,
            Rows = rows,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task SaveAsync(IReadOnlyList<UserRoleAssignmentDto> assignments, string modifiedBy, CancellationToken cancellationToken = default)
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
            var userExists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                @"SELECT COUNT(1) FROM dbo.Users WHERE UserId = @UserId AND IsActive = 1;",
                new { assignment.UserId }, transaction, cancellationToken: cancellationToken));
            if (userExists != 1)
            {
                throw new RbacValidationException($"User id {assignment.UserId} does not exist or is inactive.");
            }

            var distinctRoleIds = assignment.RoleIds.Distinct().ToList();
            if (distinctRoleIds.Count > 0)
            {
                var validRoleCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                    @"SELECT COUNT(1) FROM dbo.Roles WHERE IsActive = 1 AND RoleId IN @RoleIds;",
                    new { RoleIds = distinctRoleIds }, transaction, cancellationToken: cancellationToken));
                if (validRoleCount != distinctRoleIds.Count)
                {
                    throw new RbacValidationException($"One or more role ids for user {assignment.UserId} do not exist or are inactive.");
                }
            }

            await connection.ExecuteAsync(new CommandDefinition(
                @"DELETE FROM dbo.UserRoles WHERE UserId = @UserId;",
                new { assignment.UserId }, transaction, cancellationToken: cancellationToken));

            foreach (var roleId in distinctRoleIds)
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    @"INSERT INTO dbo.UserRoles (UserId, RoleId, CreatedBy, CreatedDate)
                      VALUES (@UserId, @RoleId, @ModifiedBy, SYSUTCDATETIME());",
                    new { assignment.UserId, RoleId = roleId, ModifiedBy = modifiedBy }, transaction, cancellationToken: cancellationToken));
            }
        }

        transaction.Commit();
    }

    private static string BuildFullName(string firstName, string? middleName, string lastName)
    {
        var parts = new[] { firstName, middleName, lastName }
            .Where(part => !string.IsNullOrWhiteSpace(part));
        return string.Join(' ', parts);
    }

    private sealed class UserRoleRow
    {
        public int UserId { get; set; }
        public int EmployeeId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public int? RoleId { get; set; }
    }
}
