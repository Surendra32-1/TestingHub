using Dapper;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.Employee;
using TeamHub.Infrastructure.Database;
using TeamHub.Shared.Common.Services;

namespace TeamHub.Infrastructure.Employees;

public sealed class EmployeeProfileRepository(
    IDbConnectionFactory connectionFactory,
    IEmployeeRepository employeeRepository) : IEmployeeProfileRepository
{
    public async Task<EmployeeProfileDto?> GetByEmployeeIdAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        const string activeEmployeeSql = @"
            SELECT BloodGroupId
            FROM dbo.Employees
            WHERE EmployeeId = @EmployeeId AND IsActive = 1;
            ";
        const string bloodGroupsSql = @"
            SELECT BloodGroupId, BloodGroupName
            FROM dbo.BloodGroups
            WHERE IsActive = 1 OR BloodGroupId = @BloodGroupId
            ORDER BY BloodGroupName;
            ";
        const string usernameSql = @"
            SELECT TOP (1) Username
            FROM dbo.Users
            WHERE EmployeeId = @EmployeeId
            ORDER BY UserId;
            ";
        const string skillOptionsSql = @"
            SELECT SkillId, SkillName, IsActive
            FROM dbo.Skills
            WHERE IsActive = 1
               OR SkillId IN (SELECT SkillId FROM dbo.EmployeeSkills WHERE EmployeeId = @EmployeeId)
            ORDER BY SkillName;
            ";
        const string dressSizeOptionsSql = @"
            SELECT DressId, DressName, [Size], IsActive
            FROM dbo.DressSizes
            WHERE IsActive = 1
               OR DressId IN (SELECT DressId FROM dbo.EmployeeDresses WHERE EmployeeId = @EmployeeId)
            ORDER BY DressName, [Size];
            ";
        const string rolesSql = @"
            SELECT DISTINCT role.RoleId, role.RoleName
            FROM dbo.UserRoles userRole
            INNER JOIN dbo.Users userAccount ON userAccount.UserId = userRole.UserId
            INNER JOIN dbo.Roles role ON role.RoleId = userRole.RoleId
            WHERE userAccount.EmployeeId = @EmployeeId
              AND userAccount.IsActive = 1
            ORDER BY role.RoleName;
            ";

        using var connection = connectionFactory.CreateConnection();
        var bloodGroupId = await connection.QuerySingleOrDefaultAsync<int?>(
            new CommandDefinition(activeEmployeeSql, new { EmployeeId = employeeId }, cancellationToken: cancellationToken));
        if (bloodGroupId is null)
        {
            return null;
        }

        var employee = await employeeRepository.GetByIdAsync(employeeId, cancellationToken);
        if (employee is null)
        {
            return null;
        }

        var bloodGroups = await connection.QueryAsync<EmployeeProfileBloodGroupDto>(
            new CommandDefinition(bloodGroupsSql, new { BloodGroupId = bloodGroupId.Value }, cancellationToken: cancellationToken));
        var username = await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(usernameSql, new { EmployeeId = employeeId }, cancellationToken: cancellationToken));
        var skillOptions = await connection.QueryAsync<EmployeeProfileSkillOptionDto>(
            new CommandDefinition(skillOptionsSql, new { EmployeeId = employeeId }, cancellationToken: cancellationToken));
        var dressSizeOptions = await connection.QueryAsync<EmployeeProfileDressSizeOptionDto>(
            new CommandDefinition(dressSizeOptionsSql, new { EmployeeId = employeeId }, cancellationToken: cancellationToken));
        var roles = await connection.QueryAsync<EmployeeProfileRoleDto>(
            new CommandDefinition(rolesSql, new { EmployeeId = employeeId }, cancellationToken: cancellationToken));

        return new EmployeeProfileDto(
            username,
            employee,
            bloodGroups.AsList(),
            roles.AsList(),
            skillOptions.AsList(),
            dressSizeOptions.AsList());
    }

    public async Task<EmployeeProfileSummaryDto?> GetSummaryAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT e.FirstName, e.MiddleName, e.LastName, image.ImagePath
            FROM dbo.Employees e
            OUTER APPLY (
                SELECT TOP (1) ImagePath
                FROM dbo.EmployeeImages
                WHERE EmployeeId = e.EmployeeId
                ORDER BY ImageId DESC
            ) image
            WHERE e.EmployeeId = @EmployeeId AND e.IsActive = 1 AND e.IsDeleted = 0;
            ";

        using var connection = connectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<EmployeeSummaryRow>(
            new CommandDefinition(sql, new { EmployeeId = employeeId }, cancellationToken: cancellationToken));
        if (row is null)
        {
            return null;
        }

        var employeeName = string.Join(' ', new[]
        {
            row.FirstName,
            row.MiddleName,
            row.LastName
        }.Where(name => !string.IsNullOrWhiteSpace(name)));

        return new EmployeeProfileSummaryDto(employeeName, ToBase64DataUrl(row.ImagePath));
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
            var mimeType = Path.GetExtension(relativePath).ToLowerInvariant() switch
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

    private sealed class EmployeeSummaryRow
    {
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public string? ImagePath { get; set; }
    }

    public async Task<bool> IsActiveBloodGroupAsync(
        int bloodGroupId,
        CancellationToken cancellationToken = default)
    {
        const string sql = "SELECT CAST(CASE WHEN EXISTS (SELECT 1 FROM dbo.BloodGroups WHERE BloodGroupId = @BloodGroupId AND IsActive = 1) THEN 1 ELSE 0 END AS BIT);";
        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(
            new CommandDefinition(sql, new { BloodGroupId = bloodGroupId }, cancellationToken: cancellationToken));
    }

    public async Task<bool> AreSelectableSkillsAsync(
        int employeeId,
        IReadOnlyCollection<int> skillIds,
        CancellationToken cancellationToken = default)
    {
        if (skillIds.Count == 0)
        {
            return true;
        }

        const string sql = @"
            SELECT COUNT(DISTINCT SkillId)
            FROM dbo.Skills
            WHERE SkillId IN @SkillIds
              AND (IsActive = 1 OR SkillId IN (
                  SELECT SkillId FROM dbo.EmployeeSkills WHERE EmployeeId = @EmployeeId
              ));
            ";
        using var connection = connectionFactory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { EmployeeId = employeeId, SkillIds = skillIds }, cancellationToken: cancellationToken));
        return count == skillIds.Count;
    }

    public async Task<bool> AreSelectableDressSizesAsync(
        int employeeId,
        IReadOnlyCollection<int> dressIds,
        CancellationToken cancellationToken = default)
    {
        if (dressIds.Count == 0)
        {
            return true;
        }

        const string sql = @"
            SELECT COUNT(DISTINCT DressId)
            FROM dbo.DressSizes
            WHERE DressId IN @DressIds
              AND (IsActive = 1 OR DressId IN (
                  SELECT DressId FROM dbo.EmployeeDresses WHERE EmployeeId = @EmployeeId
              ));
            ";
        using var connection = connectionFactory.CreateConnection();
        var count = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition(sql, new { EmployeeId = employeeId, DressIds = dressIds }, cancellationToken: cancellationToken));
        return count == dressIds.Count;
    }

    public async Task AddImageAsync(
        int employeeId,
        string imagePath,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO dbo.EmployeeImages (EmployeeId, ImagePath, CreatedDate, CreatedBy)
            SELECT @EmployeeId, @ImagePath, SYSUTCDATETIME(), 'SelfService'
            WHERE EXISTS (SELECT 1 FROM dbo.Employees WHERE EmployeeId = @EmployeeId AND IsActive = 1);
            ";

        using var connection = connectionFactory.CreateConnection();
        var affectedRows = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { EmployeeId = employeeId, ImagePath = imagePath },
            cancellationToken: cancellationToken));
        if (affectedRows == 0)
        {
            throw new InvalidOperationException("The authenticated employee profile is no longer active.");
        }
    }

    public async Task<bool> UpdateAsync(
        int employeeId,
        EmployeeProfilePatch patch,
        CancellationToken cancellationToken = default)
    {
        const string employeeSql = @"
            UPDATE dbo.Employees
            SET AlternateEmail = CASE WHEN @AlternateEmailSpecified = 1 THEN @AlternateEmail ELSE AlternateEmail END,
                BloodGroupId = CASE WHEN @BloodGroupIdSpecified = 1 THEN @BloodGroupId ELSE BloodGroupId END,
                MobileNumber = CASE WHEN @MobileNumberSpecified = 1 THEN @MobileNumber ELSE MobileNumber END,
                EmergencyContactNumber = CASE WHEN @EmergencyContactNumberSpecified = 1 THEN @EmergencyContactNumber ELSE EmergencyContactNumber END,
                MotherName = CASE WHEN @MotherNameSpecified = 1 THEN @MotherName ELSE MotherName END,
                SpouseName = CASE WHEN @SpouseNameSpecified = 1 THEN @SpouseName ELSE SpouseName END,
                LastModifiedBy = 'SelfService',
                LastModifiedDate = SYSUTCDATETIME()
            WHERE EmployeeId = @EmployeeId AND IsActive = 1;
            ";

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var affectedRows = await connection.ExecuteAsync(new CommandDefinition(
                employeeSql,
                new
                {
                    EmployeeId = employeeId,
                    patch.AlternateEmailSpecified,
                    patch.AlternateEmail,
                    patch.BloodGroupIdSpecified,
                    patch.BloodGroupId,
                    patch.MobileNumberSpecified,
                    patch.MobileNumber,
                    patch.EmergencyContactNumberSpecified,
                    patch.EmergencyContactNumber,
                    patch.MotherNameSpecified,
                    patch.MotherName,
                    patch.SpouseNameSpecified,
                    patch.SpouseName
                },
                transaction,
                cancellationToken: cancellationToken));

            if (affectedRows == 0)
            {
                transaction.Rollback();
                return false;
            }

            if (patch.LatitudeSpecified || patch.LongitudeSpecified)
            {
                await UpdateCoordinatesAsync(connection, transaction, employeeId, patch, cancellationToken);
            }

            if (patch.SkillsSpecified)
            {
                const string deleteSkillsSql = @"
                    DELETE FROM dbo.EmployeeSkills
                    WHERE EmployeeId = @EmployeeId AND SkillId NOT IN @SkillIds;
                    ";
                const string deleteAllSkillsSql = "DELETE FROM dbo.EmployeeSkills WHERE EmployeeId = @EmployeeId;";
                const string upsertSkillSql = @"
                    UPDATE dbo.EmployeeSkills
                    SET SkillLevel = @SkillLevel, LastModifiedBy = 'SelfService',
                        LastModifiedDate = SYSUTCDATETIME()
                    WHERE EmployeeId = @EmployeeId AND SkillId = @SkillId;
                    IF @@ROWCOUNT = 0
                        INSERT INTO dbo.EmployeeSkills
                            (EmployeeId, SkillId, SkillLevel, IsPrimary, CreatedBy, CreatedDate)
                        VALUES (@EmployeeId, @SkillId, @SkillLevel, 0, 'SelfService', SYSUTCDATETIME());
                    ";
                if (patch.Skills.Count == 0)
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        deleteAllSkillsSql,
                        new { EmployeeId = employeeId },
                        transaction,
                        cancellationToken: cancellationToken));
                }
                else
                {
                    var skillIds = patch.Skills.Select(skill => skill.SkillId).ToArray();
                    await connection.ExecuteAsync(new CommandDefinition(
                        deleteSkillsSql,
                        new { EmployeeId = employeeId, SkillIds = skillIds },
                        transaction,
                        cancellationToken: cancellationToken));
                }

                foreach (var skill in patch.Skills)
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        upsertSkillSql,
                        new
                        {
                            EmployeeId = employeeId,
                            skill.SkillId,
                            skill.SkillLevel
                        },
                        transaction,
                        cancellationToken: cancellationToken));
                }
            }

            if (patch.DressIdsSpecified)
            {
                const string deleteDressSizesSql = "DELETE FROM dbo.EmployeeDresses WHERE EmployeeId = @EmployeeId;";
                const string insertDressSizeSql = @"
                    INSERT INTO dbo.EmployeeDresses (EmployeeId, DressId, CreatedBy, CreatedDate)
                    VALUES (@EmployeeId, @DressId, 'SelfService', SYSUTCDATETIME());
                    ";
                await connection.ExecuteAsync(new CommandDefinition(
                    deleteDressSizesSql,
                    new { EmployeeId = employeeId },
                    transaction,
                    cancellationToken: cancellationToken));
                foreach (var dressId in patch.DressIds)
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        insertDressSizeSql,
                        new { EmployeeId = employeeId, DressId = dressId },
                        transaction,
                        cancellationToken: cancellationToken));
                }
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    private static async Task UpdateCoordinatesAsync(
        System.Data.IDbConnection connection,
        System.Data.IDbTransaction transaction,
        int employeeId,
        EmployeeProfilePatch patch,
        CancellationToken cancellationToken)
    {
        const string currentAddressSql = @"
            SELECT TOP (1) AddressId, Latitude, Longitude
            FROM dbo.EmployeeAddresses
            WHERE EmployeeId = @EmployeeId
            ORDER BY IsPrimary DESC, AddressId DESC;
            ";
        var address = await connection.QuerySingleOrDefaultAsync<EmployeeAddressCoordinateRow>(
            new CommandDefinition(currentAddressSql, new { EmployeeId = employeeId }, transaction, cancellationToken: cancellationToken));
        var latitude = patch.LatitudeSpecified ? patch.Latitude : address?.Latitude ?? 0m;
        var longitude = patch.LongitudeSpecified ? patch.Longitude : address?.Longitude ?? 0m;

        if (address is not null)
        {
            const string updateSql = @"
                UPDATE dbo.EmployeeAddresses
                SET Latitude = @Latitude, Longitude = @Longitude,
                    LastModifiedBy = 'SelfService', LastModifiedDate = SYSUTCDATETIME()
                WHERE AddressId = @AddressId;
                ";
            await connection.ExecuteAsync(new CommandDefinition(
                updateSql,
                new { address.AddressId, Latitude = latitude, Longitude = longitude },
                transaction,
                cancellationToken: cancellationToken));
            return;
        }

        const string insertSql = @"
            DECLARE @AddressTypeId INT = (
                SELECT TOP (1) AddressTypeId
                FROM dbo.AddressType
                WHERE IsActive = 1
                ORDER BY CASE WHEN AddressTypeName = 'Permanent' THEN 0 ELSE 1 END, AddressTypeId
            );
            IF @AddressTypeId IS NULL
                THROW 51000, 'No active address type is available.', 1;

            INSERT INTO dbo.EmployeeAddresses
                (EmployeeId, AddressTypeId, AddressLine1, City, [State], Latitude, Longitude,
                 IsPrimary, CreatedBy, CreatedDate)
            VALUES
                (@EmployeeId, @AddressTypeId, '', '', '', @Latitude, @Longitude,
                 1, 'SelfService', SYSUTCDATETIME());
            ";
        await connection.ExecuteAsync(new CommandDefinition(
            insertSql,
            new { EmployeeId = employeeId, Latitude = latitude, Longitude = longitude },
            transaction,
            cancellationToken: cancellationToken));
    }

    private sealed class EmployeeAddressCoordinateRow
    {
        public int AddressId { get; set; }
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
    }
}
