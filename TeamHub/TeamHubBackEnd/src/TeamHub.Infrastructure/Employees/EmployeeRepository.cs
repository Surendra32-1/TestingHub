using Dapper;
using System.Data;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.Employee;
using TeamHub.Domain.Entities;
using TeamHub.Infrastructure.Database;
using TeamHub.Shared.Common.Services;
using TeamHub.Application.DTOs.Employees;
using TeamHub.Application.DTOs.Department;
using TeamHub.Application.DTOs.Designation;
using TeamHub.Application.DTOs.JobType ;
using TeamHub.Application.DTOs.BloodGroup ;
using TeamHub.Application.DTOs.Gender ;
using TeamHub.Application.DTOs.Role ;
using TeamHub.Application.DTOs.Team ;
using TeamHub.Application.DTOs.Skill ;
using TeamHub.Application.DTOs.Recruitment;
using TeamHub.Shared.Responses;

namespace TeamHub.Infrastructure.Employees;

public sealed class EmployeeRepository(IDbConnectionFactory connectionFactory) : IEmployeeRepository
{
    public async Task<IReadOnlyList<DepartmentTreeDto>> GetOrganizationTreeAsync(CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                d.DepartmentName,
                dsg.DesignationName AS Designation,
                e.EmployeeId AS Id,
                CONCAT_WS(' ', e.FirstName, e.MiddleName, e.LastName) AS Name
            FROM Employees e
            INNER JOIN Departments d ON d.DepartmentId = e.DepartmentId
            INNER JOIN Designations dsg ON dsg.DesignationId = e.DesignationId
            WHERE e.IsActive = 1 AND e.IsDeleted = 0
            ORDER BY d.DepartmentName, dsg.DesignationName, e.FirstName, e.LastName, e.EmployeeId;
            ";

        using var connection = connectionFactory.CreateConnection();
        var rows = await connection.QueryAsync<OrganizationTreeRow>(
            new CommandDefinition(sql, cancellationToken: cancellationToken));

        return rows
            .GroupBy(row => row.DepartmentName)
            .Select(department => new DepartmentTreeDto
            {
                DepartmentName = department.Key,
                Designations = department
                    .GroupBy(row => row.Designation)
                    .Select(designation => new DesignationTreeDto
                    {
                        Designation = designation.Key,
                        Employees = designation
                            .Select(row => new EmployeeNodeDto
                            {
                                Id = row.Id,
                                Name = row.Name
                            })
                            .ToList()
                    })
                    .ToList()
            })
            .ToList();
    }

    public async Task<EmployeeHierarchyResponse?> GetHierarchyAsync(
        int employeeId,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.CreateConnection();
        using var results = await connection.QueryMultipleAsync(new CommandDefinition(
            "dbo.sp_GetEmployeeOrganizationHierarchy",
            new { EmployeeId = employeeId },
            commandType: CommandType.StoredProcedure,
            cancellationToken: cancellationToken));

        var managerRows = (await results.ReadAsync<EmployeeHierarchyEmployeeRow>()).AsList();
        var selectedRow = await results.ReadSingleOrDefaultAsync<EmployeeHierarchyEmployeeRow>();
        if (selectedRow is null)
        {
            return null;
        }

        var cotivityRows = (await results.ReadAsync<EmployeeHierarchyCotivityRow>()).AsList();
        var reportRows = (await results.ReadAsync<EmployeeHierarchyEmployeeRow>()).AsList();

        var selectedEmployee = MapSelectedEmployee(selectedRow);
        var managers = managerRows.Select(MapEmployeeHierarchyNode).ToList();
        var cotivity = cotivityRows.Select(row => new CotivityHierarchyNodeDto
        {
            CotivityEmployeeId = row.CotivityEmployeeId,
            ParentId = row.ParentId,
            CotivityEmployeeName = row.CotivityEmployeeName,
            CotivityDesignationName = row.CotivityDesignationName,
            CotivityDepartmentName = row.CotivityDepartmentName,
            ImagePath = row.ImagePath,
            Source = row.Source,
            Level = row.Level
        }).ToList();

        return new EmployeeHierarchyResponse
        {
            EmployeeManagers = managers,
            SelectedEmployee = selectedEmployee,
            CotivityHierarchy = cotivity,
            ReportingHierarchy = reportRows.Select(MapEmployeeHierarchyNode).ToList()
        };
    }

    private static EmployeeHierarchyNodeDto MapEmployeeHierarchyNode(EmployeeHierarchyEmployeeRow row) =>
        new()
        {
            EmployeeId = row.EmployeeId,
            ParentId = row.ParentId,
            EmployeeName = row.EmployeeName,
            DesignationName = row.DesignationName,
            DepartmentName = row.DepartmentName,
            ImagePath = row.ImagePath,
            EmployeeNumber = row.EmployeeNumber,
            Source = row.Source,
            Level = row.Level
        };

    private static SelectedEmployeeDto MapSelectedEmployee(EmployeeHierarchyEmployeeRow row) =>
        new()
        {
            EmployeeId = row.EmployeeId,
            ParentId = row.ParentId,
            EmployeeName = row.EmployeeName,
            DesignationName = row.DesignationName,
            DepartmentName = row.DepartmentName,
            ImagePath = row.ImagePath,
            EmployeeNumber = row.EmployeeNumber,
            Source = row.Source,
            Level = row.Level
        };

    public Task<PagedResult<GetGridEmployeeDto>> GetAllAsync(
        CancellationToken cancellationToken = default,
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string searchField = "all",
        string? departmentName = null,
        string status = "active") =>
        GetEmployeesAsync(cancellationToken, pageNumber, pageSize, searchTerm, searchField, null, departmentName, status);

    public Task<PagedResult<GetGridEmployeeDto>> GetActiveByDepartmentIdAsync(
        int departmentId,
        CancellationToken cancellationToken = default,
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string searchField = "all") =>
        GetEmployeesAsync(cancellationToken, pageNumber, pageSize, searchTerm, searchField, departmentId, null, "active");

    public async Task<PagedResult<GetActiveInactiveEmployeeDto>> GetAllActiveInactiveAsync(
        CancellationToken cancellationToken = default,
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string searchField = "all",
        string? departmentName = null,
        string status = "all")
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        searchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        searchField = searchField is "name" or "designation" or "department" ? searchField : "all";
        departmentName = string.IsNullOrWhiteSpace(departmentName) ? null : departmentName.Trim();
        status = status is "active" or "inactive" or "all" ? status : "all";

        const string fromSql = @"
            FROM Employees e
            LEFT JOIN Departments d ON d.DepartmentId = e.DepartmentId
            LEFT JOIN Designations dsg ON dsg.DesignationId = e.DesignationId
            LEFT JOIN Employees reports ON reports.EmployeeId = e.ReportsToEmployeeId
            LEFT JOIN Teams t ON t.TeamId = e.TeamId
            LEFT JOIN BusinessUnits bu ON bu.BusinessUnitId = e.BusinessUnitId";
        const string whereSql = @"
            WHERE (@Status = 'all' OR e.IsActive = CASE WHEN @Status = 'active' THEN 1 ELSE 0 END)
                AND (@DepartmentName IS NULL OR d.DepartmentName = @DepartmentName)
                AND (
                        @SearchTerm IS NULL
                        OR (@SearchField IN ('all', 'name') AND LOWER(CONCAT_WS(' ', e.FirstName, e.MiddleName, e.LastName)) LIKE '%' + LOWER(@SearchTerm) + '%')
                        OR (@SearchField IN ('all', 'designation') AND LOWER(dsg.DesignationName) LIKE '%' + LOWER(@SearchTerm) + '%')
                        OR (@SearchField IN ('all', 'department') AND LOWER(d.DepartmentName) LIKE '%' + LOWER(@SearchTerm) + '%')
                        OR (@SearchField = 'all' AND LOWER(t.TeamName) LIKE '%' + LOWER(@SearchTerm) + '%')
                )";
        using var connection = connectionFactory.CreateConnection();
        var filters = new { SearchTerm = searchTerm, SearchField = searchField, DepartmentName = departmentName, Status = status };
        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition("SELECT COUNT(1)" + fromSql + whereSql, filters, cancellationToken: cancellationToken));
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        pageNumber = totalPages == 0 ? 1 : Math.Min(pageNumber, totalPages);

        const string selectSql = @"
            SELECT
                e.EmployeeId AS Id,
                e.ENumber,
                CONCAT_WS(' ', e.FirstName, e.MiddleName, e.LastName) AS Name,
                dsg.DesignationName AS Designation,
                d.DepartmentName AS Department,
                CONCAT_WS(' ', reports.FirstName, reports.MiddleName, reports.LastName) AS ReportsToEmployee,
                t.TeamName,
                bu.BusinessUnitName,
                e.IsActive,
                e.IsDeleted";
        var sql = selectSql + fromSql + whereSql + @"
            ORDER BY
                CASE WHEN e.IsActive = 1 THEN 0 ELSE 1 END,
                e.FirstName, e.LastName, e.EmployeeId
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";
        var rows = await connection.QueryAsync<ActiveInactiveEmployeeRow>(
            new CommandDefinition(
                sql,
                new
                {
                    filters.SearchTerm,
                    filters.SearchField,
                    filters.DepartmentName,
                    filters.Status,
                    Offset = (long)(pageNumber - 1) * pageSize,
                    PageSize = pageSize
                },
                cancellationToken: cancellationToken));
        var items = rows
            .Select(row => new GetActiveInactiveEmployeeDto
            {
                Id = row.Id,
                ENumber = row.ENumber,
                Name = row.Name,
                Designation = row.Designation,
                Department = row.Department,
                ReportsToEmployee = string.IsNullOrWhiteSpace(row.ReportsToEmployee) ? null : row.ReportsToEmployee,
                TeamName = row.TeamName,
                BusinessUnitName = row.BusinessUnitName,
                IsActive = row.IsActive,
                IsDeleted = row.IsDeleted
            })
            .ToArray();

        return new PagedResult<GetActiveInactiveEmployeeDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    private sealed class ActiveInactiveEmployeeRow
    {
        public int Id { get; set; }
        public int ENumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Designation { get; set; }
        public string? Department { get; set; }
        public string? ReportsToEmployee { get; set; }
        public string? TeamName { get; set; }
        public string? BusinessUnitName { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }

    private async Task<PagedResult<GetGridEmployeeDto>> GetEmployeesAsync(
        CancellationToken cancellationToken,
        int pageNumber,
        int pageSize,
        string? searchTerm,
        string searchField,
        int? departmentId,
        string? departmentName,
        string status)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        searchTerm = string.IsNullOrWhiteSpace(searchTerm) ? null : searchTerm.Trim();
        searchField = searchField is "name" or "designation" or "department" ? searchField : "all";
        departmentName = string.IsNullOrWhiteSpace(departmentName) ? null : departmentName.Trim();
        status = status is "active" or "inactive" or "all" ? status : "active";

        const string filterFromSql = @"
            FROM Employees e
            LEFT JOIN Departments d ON d.DepartmentId = e.DepartmentId
            LEFT JOIN Designations dsg ON dsg.DesignationId = e.DesignationId
            LEFT JOIN Teams t ON t.TeamId = e.TeamId";
        const string whereSql = @"
            WHERE e.IsDeleted = 0
                AND (@Status = 'all' OR e.IsActive = CASE WHEN @Status = 'active' THEN 1 ELSE 0 END)
                AND (@DepartmentId IS NULL OR e.DepartmentId = @DepartmentId)
                AND (@DepartmentName IS NULL OR d.DepartmentName = @DepartmentName)
                AND (
                    @SearchTerm IS NULL
                    OR (@SearchField IN ('all', 'name') AND LOWER(CONCAT_WS(' ', e.FirstName, e.MiddleName, e.LastName)) LIKE '%' + LOWER(@SearchTerm) + '%')
                    OR (@SearchField IN ('all', 'designation') AND LOWER(dsg.DesignationName) LIKE '%' + LOWER(@SearchTerm) + '%')
                    OR (@SearchField IN ('all', 'department') AND LOWER(d.DepartmentName) LIKE '%' + LOWER(@SearchTerm) + '%')
                    OR (@SearchField = 'all' AND LOWER(t.TeamName) LIKE '%' + LOWER(@SearchTerm) + '%')
                )";
        var filters = new
        {
            SearchTerm = searchTerm,
            SearchField = searchField,
            DepartmentId = departmentId,
            DepartmentName = departmentName,
            Status = status
        };

        using var connection = connectionFactory.CreateConnection();
        var totalCount = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition("SELECT COUNT(1)" + filterFromSql + whereSql, filters, cancellationToken: cancellationToken));
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        pageNumber = totalPages == 0 ? 1 : Math.Min(pageNumber, totalPages);

        const string sql = @"
            WITH PagedEmployees AS (
                SELECT e.EmployeeId,
                       ROW_NUMBER() OVER (
                           ORDER BY
                               CASE WHEN @DepartmentId IS NOT NULL THEN dsg.[Rank] END ASC,
                               e.FirstName, e.LastName, e.EmployeeId
                       ) AS PageOrder
            " + filterFromSql + whereSql + @"
                ORDER BY
                    CASE WHEN @DepartmentId IS NOT NULL THEN dsg.[Rank] END ASC,
                    e.FirstName, e.LastName, e.EmployeeId
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY
            )
            SELECT
                e.EmployeeId AS Id,
                e.ENumber,
                CONCAT_WS(' ', e.FirstName, e.MiddleName, e.LastName) AS Name,
                e.Email,
                dsg.DesignationName AS Designation,
                dsg.[Rank] AS DesignationRank,
                d.DepartmentName AS Department,
                e.JoinedDate,
                e.IsActive,
                g.GenderName AS Gender,
                e.ReportsToEmployeeId,
                CONCAT_WS(' ', reports.FirstName, reports.MiddleName, reports.LastName) AS ReportsToEmployee,
                CONCAT_WS(' ', leads.FirstName, leads.MiddleName, leads.LastName) AS LeadEmployee,
                e.StationNumber,
                e.RecruitmentId,
                e.ExtensionNumber,
                e.CitizenshipNumber,
                e.DateOfBirth,
                e.PANNumber,
                e.LockerNumber,
                (SELECT STRING_AGG(role.RoleName, ', ') FROM dbo.UserRoles ur INNER JOIN dbo.Users u ON u.UserId = ur.UserId INNER JOIN dbo.Roles role ON role.RoleId = ur.RoleId AND role.IsActive = 1 WHERE u.EmployeeId = e.EmployeeId) AS RoleName,
                t.TeamName,
                e.BusinessUnitId,
                bu.BusinessUnitName,
                ce.CotivityEmployeeName AS ReportToCotivityEmployee,
                CONCAT_WS(', ', address.AddressLine1, address.AddressLine2, address.City, address.State) AS Address,
                skill.SkillName AS SkillName,
                employeeSkill.IsPrimary AS SkillIsPrimary,
                ei.ImagePath
            FROM PagedEmployees page
            INNER JOIN Employees e ON e.EmployeeId = page.EmployeeId
            LEFT JOIN Departments d ON d.DepartmentId = e.DepartmentId
            LEFT JOIN Designations dsg ON dsg.DesignationId = e.DesignationId
            LEFT JOIN Gender g ON g.GenderId = e.GenderId
            LEFT JOIN Employees reports ON reports.EmployeeId = e.ReportsToEmployeeId AND reports.IsDeleted = 0
            LEFT JOIN Employees leads ON leads.EmployeeId = e.LeadEmployeeId AND leads.IsDeleted = 0
            LEFT JOIN Teams t ON t.TeamId = e.TeamId
            LEFT JOIN BusinessUnits bu ON bu.BusinessUnitId = e.BusinessUnitId
            LEFT JOIN CotivityEmployees ce ON ce.CotivityEmployeeId = e.CotivityEmployeeId
            LEFT JOIN EmployeeAddresses address ON address.EmployeeId = e.EmployeeId
            LEFT JOIN EmployeeSkills employeeSkill ON employeeSkill.EmployeeId = e.EmployeeId
            LEFT JOIN Skills skill ON skill.SkillId = employeeSkill.SkillId
            LEFT JOIN EmployeeImages ei ON ei.EmployeeId = e.EmployeeId
            ORDER BY page.PageOrder;";
        var rows = await connection.QueryAsync<EmployeeGridRow>(
            new CommandDefinition(
                sql,
                new
                {
                    filters.SearchTerm,
                    filters.SearchField,
                    filters.DepartmentId,
                    filters.DepartmentName,
                    filters.Status,
                    Offset = (long)(pageNumber - 1) * pageSize,
                    PageSize = pageSize
                },
                cancellationToken: cancellationToken));

        var items = rows
            .GroupBy(row => row.Id)
            .Select(group =>
            {
                var first = group.First();
                return new GetGridEmployeeDto
                {
                    Id = first.Id,
                    ENumber = first.ENumber,
                    Name = first.Name,
                    Email = first.Email,
                    Designation = first.Designation,
                    DesignationRank = first.DesignationRank,
                    Department = first.Department,
                    JoinedDate = DateOnly.FromDateTime(first.JoinedDate),
                    PrimarySkill = group.Where(row => row.SkillIsPrimary && row.SkillName is not null)
                        .Select(row => row.SkillName)
                        .FirstOrDefault(),
                    IsActive = first.IsActive,
                    Gender = first.Gender,
                    ReportsToEmployeeId = first.ReportsToEmployeeId,
                    ReportsToEmployee = first.ReportsToEmployee,
                    LeadEmployee = first.LeadEmployee,
                    StationNumber = first.StationNumber,
                    RecruitmentId = first.RecruitmentId,
                    ExtensionNumber = first.ExtensionNumber,
                    CitizenshipNumber = first.CitizenshipNumber,
                    DateOfBirth = first.DateOfBirth.HasValue
                        ? DateOnly.FromDateTime(first.DateOfBirth.Value)
                        : null,
                    PANNumber = first.PANNumber,
                    LockerNumber = first.LockerNumber,
                    RoleName = first.RoleName,
                    TeamName = first.TeamName,
                    BusinessUnitId = first.BusinessUnitId,
                    BusinessUnitName = first.BusinessUnitName,
                    ReportToCotivityEmployee = first.ReportToCotivityEmployee,
                    ImageBase64 = !string.IsNullOrWhiteSpace(first.ImagePath)
                        ? ToBase64DataUrl(first.ImagePath)
                        : null,
                    Addresses = group.Where(row => row.Address is not null)
                        .Select(row => row.Address!)
                        .Distinct()
                        .ToList(),
                    Skills = group.Where(row => row.SkillName is not null)
                        .Select(row => row.SkillName!)
                        .Distinct()
                        .ToList()
                };
            })
            .ToArray();

        return new PagedResult<GetGridEmployeeDto>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public Task<GetEmployeeDto?> GetByIdAsync(int employeeId, CancellationToken cancellationToken = default) =>
        GetEmployeeAsync(employeeId, includeDeleted: false, cancellationToken);

    public Task<GetEmployeeDto?> GetActiveInactiveByIdAsync(int employeeId, CancellationToken cancellationToken = default) =>
        GetEmployeeAsync(employeeId, includeDeleted: true, cancellationToken);

    private async Task<GetEmployeeDto?> GetEmployeeAsync(int employeeId, bool includeDeleted, CancellationToken cancellationToken)
    {
        const string employeeSql = @"
            SELECT e.EmployeeId, e.ENumber, e.FirstName, e.MiddleName, e.LastName, e.JoinedDate,
                   e.Email, e.AlternateEmail, e.DesignationId, designation.DesignationName,
                   e.DepartmentId, department.DepartmentName, e.JobTypeId, jobType.JobTypeName,
                   e.BloodGroupId, bloodGroup.BloodGroupName, e.GenderId, gender.GenderName,
                   e.ReportsToEmployeeId, CONCAT_WS(' ', reports.FirstName, reports.MiddleName, reports.LastName) AS ReportsToEmployeeName,
                   e.LeadEmployeeId, CONCAT_WS(' ', lead.FirstName, lead.MiddleName, lead.LastName) AS LeadEmployeeName,
                   e.StationNumber, e.RecruitmentId, recruitment.RecruitmentName,
                   e.ExtensionNumber, e.CitizenshipNumber, e.DateOfBirth, e.PANNumber, e.LockerNumber,
                   (SELECT STRING_AGG(role.RoleName, ', ') FROM dbo.UserRoles ur INNER JOIN dbo.Users u ON u.UserId = ur.UserId INNER JOIN dbo.Roles role ON role.RoleId = ur.RoleId AND role.IsActive = 1 WHERE u.EmployeeId = e.EmployeeId) AS RoleName,
                   e.TeamId, team.TeamName,
                   e.BusinessUnitId, businessUnit.BusinessUnitName,
                   e.CotivityEmployeeId, cotivityEmployee.CotivityEmployeeName,
                   e.IsActive, e.JoinedDateBS, e.DateOfBirthBS, e.PhoneNumber, e.MobileNumber,
                   e.EmergencyContactNumber, e.EmergencyContactPerson, e.FatherName, e.MotherName,
                   e.SpouseName, image.ImagePath
            FROM Employees e
            LEFT JOIN Designations designation ON designation.DesignationId = e.DesignationId
            LEFT JOIN Departments department ON department.DepartmentId = e.DepartmentId
            LEFT JOIN JobTypes jobType ON jobType.JobTypeId = e.JobTypeId
            LEFT JOIN BloodGroups bloodGroup ON bloodGroup.BloodGroupId = e.BloodGroupId
            LEFT JOIN Gender gender ON gender.GenderId = e.GenderId
            LEFT JOIN Employees reports ON reports.EmployeeId = e.ReportsToEmployeeId
            LEFT JOIN Employees lead ON lead.EmployeeId = e.LeadEmployeeId
            LEFT JOIN Recruitments recruitment ON recruitment.RecruitmentId = e.RecruitmentId
            LEFT JOIN Teams team ON team.TeamId = e.TeamId
            LEFT JOIN BusinessUnits businessUnit ON businessUnit.BusinessUnitId = e.BusinessUnitId
            LEFT JOIN CotivityEmployees cotivityEmployee ON cotivityEmployee.CotivityEmployeeId = e.CotivityEmployeeId
            OUTER APPLY (
                SELECT TOP (1) ImagePath
                FROM EmployeeImages
                WHERE EmployeeId = e.EmployeeId
                ORDER BY ImageId DESC
            ) image
            WHERE e.EmployeeId = @EmployeeId AND (@IncludeDeleted = 1 OR e.IsDeleted = 0);
            ";

        using var connection = connectionFactory.CreateConnection();
        var row = await connection.QuerySingleOrDefaultAsync<EmployeeDetailRow>(
            new CommandDefinition(employeeSql, new { EmployeeId = employeeId, IncludeDeleted = includeDeleted }, cancellationToken: cancellationToken));
        if (row is null)
        {
            return null;
        }

        const string addressSql = @"
            WITH DeduplicatedAddresses AS
            (
                SELECT address.AddressId, address.AddressTypeId, addressType.AddressTypeName,
                       address.AddressLine1, address.AddressLine2, address.City, address.State,
                       address.Latitude, address.Longitude, address.IsPrimary,
                       ROW_NUMBER() OVER (
                           PARTITION BY address.AddressTypeId,
                                        UPPER(LTRIM(RTRIM(ISNULL(address.AddressLine1, '')))),
                                        UPPER(LTRIM(RTRIM(ISNULL(address.AddressLine2, '')))),
                                        UPPER(LTRIM(RTRIM(ISNULL(address.City, '')))),
                                        UPPER(LTRIM(RTRIM(ISNULL(address.State, ''))))
                           ORDER BY address.IsPrimary DESC, address.AddressId DESC
                       ) AS DuplicateRank
                FROM EmployeeAddresses address
                INNER JOIN AddressType addressType ON addressType.AddressTypeId = address.AddressTypeId
                WHERE address.EmployeeId = @EmployeeId
            )
            SELECT AddressId, AddressTypeId, AddressTypeName, AddressLine1, AddressLine2,
                   City, State, Latitude, Longitude, IsPrimary
            FROM DeduplicatedAddresses
            WHERE DuplicateRank = 1
            ORDER BY IsPrimary DESC, AddressTypeName, AddressId;
            ";
        const string skillSql = @"
            SELECT employeeSkill.EmployeeSkillId, employeeSkill.SkillId, skill.SkillName,
                   employeeSkill.SkillLevel, employeeSkill.IsPrimary
            FROM EmployeeSkills employeeSkill
            INNER JOIN Skills skill ON skill.SkillId = employeeSkill.SkillId
            WHERE employeeSkill.EmployeeId = @EmployeeId
            ORDER BY employeeSkill.IsPrimary DESC, skill.SkillName;
            ";
        const string dressSql = @"
            SELECT dress.DressId, dress.DressName, dress.Size, dress.IsActive
            FROM EmployeeDresses employeeDress
            INNER JOIN DressSizes dress ON dress.DressId = employeeDress.DressId
            WHERE employeeDress.EmployeeId = @EmployeeId
            ORDER BY dress.DressName, dress.Size;
            ";
        const string educationSql = @"
            SELECT EducationId, Qualification, BoardUniversity, Institution, Faculty,
                   PassedYear, JoinedDate, PassoutDate, JoinedDateBS, PassoutDateBS, DivisionGrade, Remarks
            FROM EmployeeEducation
            WHERE EmployeeId = @EmployeeId
            ORDER BY PassedYear DESC, EducationId DESC;
            ";
        const string trainingSql = @"
            SELECT TrainingId, TrainingTitle, TrainingProvider, TrainingType, TrainingLocation,
                   JoinedDate, PassoutDate, JoinedDateBS, PassoutDateBS, Duration, CertificateNo, Remarks
            FROM EmployeeTraining
            WHERE EmployeeId = @EmployeeId
            ORDER BY JoinedDate DESC, TrainingId DESC;
            ";
        const string roleIdSql = @"
            SELECT ur.RoleId
            FROM dbo.UserRoles ur
            INNER JOIN dbo.Users u ON u.UserId = ur.UserId
            INNER JOIN dbo.Roles role ON role.RoleId = ur.RoleId AND role.IsActive = 1
            WHERE u.EmployeeId = @EmployeeId
            ORDER BY role.RoleName;
            ";
        var parameters = new { EmployeeId = employeeId };
        var addresses = await connection.QueryAsync<EmployeeAddressDto>(new CommandDefinition(addressSql, parameters, cancellationToken: cancellationToken));
        var skills = await connection.QueryAsync<EmployeeSkillDto>(new CommandDefinition(skillSql, parameters, cancellationToken: cancellationToken));
        var dressSizes = await connection.QueryAsync<EmployeeDressSizesDto>(new CommandDefinition(dressSql, parameters, cancellationToken: cancellationToken));
        var educations = await connection.QueryAsync<EmployeeEducationDto>(new CommandDefinition(educationSql, parameters, cancellationToken: cancellationToken));
        var trainings = await connection.QueryAsync<EmployeeTrainingDto>(new CommandDefinition(trainingSql, parameters, cancellationToken: cancellationToken));
        var roleIds = await connection.QueryAsync<int>(new CommandDefinition(roleIdSql, parameters, cancellationToken: cancellationToken));

        return new GetEmployeeDto
        {
            EmployeeId = row.EmployeeId,
            ENumber = row.ENumber,
            FirstName = row.FirstName,
            MiddleName = row.MiddleName,
            LastName = row.LastName,
            JoinedDate = DateOnly.FromDateTime(row.JoinedDate),
            JoinedDateBS = row.JoinedDateBS ?? string.Empty,
            Email = row.Email,
            AlternateEmail = row.AlternateEmail,
            DesignationId = row.DesignationId,
            DesignationName = row.DesignationName,
            DepartmentId = row.DepartmentId,
            DepartmentName = row.DepartmentName,
            JobTypeId = row.JobTypeId,
            JobTypeName = row.JobTypeName,
            BloodGroupId = row.BloodGroupId,
            BloodGroupName = row.BloodGroupName,
            GenderId = row.GenderId,
            GenderName = row.GenderName,
            ReportsToEmployeeId = row.ReportsToEmployeeId,
            ReportsToEmployeeName = row.ReportsToEmployeeName,
            LeadEmployeeId = row.LeadEmployeeId,
            LeadEmployeeName = row.LeadEmployeeName,
            StationNumber = row.StationNumber,
            RecruitmentId = row.RecruitmentId,
            RecruitmentName = row.RecruitmentName,
            ExtensionNumber = row.ExtensionNumber,
            CitizenshipNumber = row.CitizenshipNumber,
            DateOfBirth = row.DateOfBirth.HasValue ? DateOnly.FromDateTime(row.DateOfBirth.Value) : null,
            DateOfBirthBS = row.DateOfBirthBS,
            PhoneNumber = row.PhoneNumber,
            MobileNumber = row.MobileNumber,
            EmergencyContactNumber = row.EmergencyContactNumber,
            EmergencyContactPerson = row.EmergencyContactPerson ?? string.Empty,
            FatherName = row.FatherName ?? string.Empty,
            MotherName = row.MotherName,
            SpouseName = row.SpouseName,
            PANNumber = row.PANNumber,
            LockerNumber = row.LockerNumber,
            RoleIds = roleIds.AsList(),
            RoleName = row.RoleName,
            TeamId = row.TeamId,
            TeamName = row.TeamName,
            BusinessUnitId = row.BusinessUnitId,
            BusinessUnitName = row.BusinessUnitName,
            CotivityEmployeeId = row.CotivityEmployeeId,
            CotivityEmployeeName = row.CotivityEmployeeName,
            IsActive = row.IsActive,
            ImageBase64 = ToBase64DataUrl(row.ImagePath),
            Addresses = addresses.AsList(),
            Skills = skills.AsList(),
            DressSizes = dressSizes.AsList(),
            Educations = educations.AsList(),
            Trainings = trainings.AsList()
        };
    }

    public async Task<int> CreateAsync(Employee employee, CancellationToken cancellationToken = default)
    {
        const string employeeSql = @"
            INSERT INTO Employees
                (ENumber, FirstName, MiddleName, LastName, JoinedDate, JoinedDateBS, Email, AlternateEmail,
                 DesignationId, DepartmentId, JobTypeId, BloodGroupId, GenderId, ReportsToEmployeeId,
                 LeadEmployeeId, StationNumber, RecruitmentId, ExtensionNumber, CitizenshipNumber,
                 DateOfBirth, DateOfBirthBS, PhoneNumber, MobileNumber, EmergencyContactNumber,
                 EmergencyContactPerson, FatherName, MotherName, SpouseName,
                 PANNumber, LockerNumber, TeamId, BusinessUnitId, CotivityEmployeeId, IsActive,
                 CreatedDate, CreatedBy)
            OUTPUT INSERTED.EmployeeId
            VALUES
                (@ENumber, @FirstName, @MiddleName, @LastName, @JoinedDate, @JoinedDateBS, @Email, @AlternateEmail,
                 @DesignationId, @DepartmentId, @JobTypeId, @BloodGroupId, @GenderId, @ReportsToEmployeeId,
                 @LeadEmployeeId, @StationNumber, @RecruitmentId, @ExtensionNumber, @CitizenshipNumber,
                 @DateOfBirth, @DateOfBirthBS, @PhoneNumber, @MobileNumber, @EmergencyContactNumber,
                 @EmergencyContactPerson, @FatherName, @MotherName, @SpouseName,
                 @PANNumber, @LockerNumber, @TeamId, @BusinessUnitId, @CotivityEmployeeId, @IsActive,
                 @CreatedDate, @CreatedBy);
            ";
        const string addressSql = @"
            INSERT INTO EmployeeAddresses
                (EmployeeId, AddressTypeId, AddressLine1, AddressLine2, City, State, Latitude, Longitude,
                 IsPrimary, CreatedDate, CreatedBy)
            OUTPUT INSERTED.AddressId
            VALUES
                (@EmployeeId, @AddressTypeId, @AddressLine1, @AddressLine2, @City, @State, @Latitude,
                 @Longitude, @IsPrimary, @CreatedDate, @CreatedBy);
            ";
        const string skillSql = @"
            INSERT INTO EmployeeSkills
                (EmployeeId, SkillId, SkillLevel, IsPrimary, CreatedDate, CreatedBy)
            OUTPUT INSERTED.EmployeeSkillId
            VALUES (@EmployeeId, @SkillId, @SkillLevel, @IsPrimary, @CreatedDate, @CreatedBy);
            ";
        const string dressSql = @"
            INSERT INTO EmployeeDresses
                (EmployeeId, DressId, CreatedDate, CreatedBy)
            OUTPUT INSERTED.EmployeeDressId
            VALUES (@EmployeeId, @DressId, @CreatedDate, @CreatedBy);
            ";
        const string educationSql = @"
            INSERT INTO EmployeeEducation
                (EmployeeId, Qualification, BoardUniversity, Institution, Faculty, PassedYear,
                 JoinedDate, PassoutDate, JoinedDateBS, PassoutDateBS, DivisionGrade, Remarks, CreatedDate, CreatedBy)
            OUTPUT INSERTED.EducationId
            VALUES (@EmployeeId, @Qualification, @BoardUniversity, @Institution, @Faculty, @PassedYear,
                    @JoinedDate, @PassoutDate, @JoinedDateBS, @PassoutDateBS, @DivisionGrade, @Remarks, @CreatedDate, @CreatedBy);
            ";
        const string trainingSql = @"
            INSERT INTO EmployeeTraining
                (EmployeeId, TrainingTitle, TrainingProvider, TrainingType, TrainingLocation,
                 JoinedDate, PassoutDate, JoinedDateBS, PassoutDateBS, Duration, CertificateNo, Remarks, CreatedDate, CreatedBy)
            OUTPUT INSERTED.TrainingId
            VALUES (@EmployeeId, @TrainingTitle, @TrainingProvider, @TrainingType, @TrainingLocation,
                    @JoinedDate, @PassoutDate, @JoinedDateBS, @PassoutDateBS, @Duration, @CertificateNo, @Remarks, @CreatedDate, @CreatedBy);
            ";


        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();

        try
        {
            var employeeParams = new
            {
                employee.ENumber,
                employee.FirstName,
                employee.MiddleName,
                employee.LastName,
                JoinedDate = employee.JoinedDate.ToDateTime(TimeOnly.MinValue),
                employee.JoinedDateBS,
                employee.Email,
                employee.AlternateEmail,
                employee.DesignationId,
                employee.DepartmentId,
                employee.JobTypeId,
                employee.BloodGroupId,
                employee.GenderId,
                employee.ReportsToEmployeeId,
                employee.LeadEmployeeId,
                employee.StationNumber,
                employee.RecruitmentId,
                employee.ExtensionNumber,
                employee.CitizenshipNumber,
                DateOfBirth = employee.DateOfBirth.HasValue
                    ? (DateTime?)employee.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue)
                    : null,
                employee.DateOfBirthBS,
                employee.PhoneNumber,
                employee.MobileNumber,
                employee.EmergencyContactNumber,
                employee.EmergencyContactPerson,
                employee.FatherName,
                employee.MotherName,
                employee.SpouseName,
                employee.PANNumber,
                employee.LockerNumber,
                employee.TeamId,
                employee.BusinessUnitId,
                employee.CotivityEmployeeId,
                employee.IsActive,
                employee.CreatedDate,
                employee.CreatedBy
            };

            employee.EmployeeId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                employeeSql, employeeParams, transaction, cancellationToken: cancellationToken));

            foreach (var address in employee.Addresses)
            {
                address.EmployeeId = employee.EmployeeId;
                var addressParams = new
                {
                    address.EmployeeId,
                    address.AddressTypeId,
                    address.AddressLine1,
                    address.AddressLine2,
                    address.City,
                    address.State,
                    address.Latitude,
                    address.Longitude,
                    address.IsPrimary,
                    address.CreatedDate,
                    address.CreatedBy
                };

                address.AddressId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                    addressSql, addressParams, transaction, cancellationToken: cancellationToken));
            }

            foreach (var skill in employee.Skills)
            {
                skill.EmployeeId = employee.EmployeeId;
                var skillParams = new
                {
                    skill.EmployeeId,
                    skill.SkillId,
                    skill.SkillLevel,
                    skill.IsPrimary,
                    skill.CreatedDate,
                    skill.CreatedBy
                };

                skill.EmployeeSkillId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                    skillSql, skillParams, transaction, cancellationToken: cancellationToken));
            }

            foreach (var dress in employee.DressSizes)
            {
                dress.EmployeeId = employee.EmployeeId;
                var dressParams = new
                {
                    dress.EmployeeId,
                    dress.DressId,
                    dress.CreatedDate,
                    dress.CreatedBy
                };

                dress.EmployeeDressId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                    dressSql, dressParams, transaction, cancellationToken: cancellationToken));
            }

            foreach (var education in employee.Educations)
            {
                education.EmployeeId = employee.EmployeeId;
                var educationParams = new
                {
                    education.EmployeeId,
                    education.Qualification,
                    education.BoardUniversity,
                    education.Institution,
                    education.Faculty,
                    education.PassedYear,
                    JoinedDate = education.JoinedDate?.ToDateTime(TimeOnly.MinValue),
                    PassoutDate = education.PassoutDate?.ToDateTime(TimeOnly.MinValue),
                    education.JoinedDateBS,
                    education.PassoutDateBS,
                    education.DivisionGrade,
                    education.Remarks,
                    education.CreatedDate,
                    education.CreatedBy
                };

                education.EducationId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                    educationSql, educationParams, transaction, cancellationToken: cancellationToken));
            }

            foreach (var training in employee.Trainings)
            {
                training.EmployeeId = employee.EmployeeId;
                var trainingParams = new
                {
                    training.EmployeeId,
                    training.TrainingTitle,
                    training.TrainingProvider,
                    training.TrainingType,
                    training.TrainingLocation,
                    JoinedDate = training.JoinedDate?.ToDateTime(TimeOnly.MinValue),
                    PassoutDate = training.PassoutDate?.ToDateTime(TimeOnly.MinValue),
                    training.JoinedDateBS,
                    training.PassoutDateBS,
                    training.Duration,
                    training.CertificateNo,
                    training.Remarks,
                    training.CreatedDate,
                    training.CreatedBy
                };

                training.TrainingId = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                    trainingSql, trainingParams, transaction, cancellationToken: cancellationToken));
            }

            transaction.Commit();
            return employee.EmployeeId;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public Task<bool> UpdateAsync(Employee employee, CancellationToken cancellationToken = default) =>
        UpdateEmployeeAsync(employee, includeDeleted: false, cancellationToken);

    public Task<bool> UpdateActiveInactiveAsync(Employee employee, CancellationToken cancellationToken = default) =>
        UpdateEmployeeAsync(employee, includeDeleted: true, cancellationToken);

    private async Task<bool> UpdateEmployeeAsync(Employee employee, bool includeDeleted, CancellationToken cancellationToken)
    {
        const string employeeSql = @"
            UPDATE Employees
            SET ENumber = @ENumber,
                FirstName = @FirstName,
                MiddleName = @MiddleName,
                LastName = @LastName,
                JoinedDate = @JoinedDate,
                JoinedDateBS = @JoinedDateBS,
                Email = @Email,
                AlternateEmail = @AlternateEmail,
                DesignationId = @DesignationId,
                DepartmentId = @DepartmentId,
                JobTypeId = @JobTypeId,
                BloodGroupId = @BloodGroupId,
                GenderId = @GenderId,
                ReportsToEmployeeId = @ReportsToEmployeeId,
                LeadEmployeeId = @LeadEmployeeId,
                StationNumber = @StationNumber,
                RecruitmentId = @RecruitmentId,
                ExtensionNumber = @ExtensionNumber,
                CitizenshipNumber = @CitizenshipNumber,
                DateOfBirth = @DateOfBirth,
                DateOfBirthBS = @DateOfBirthBS,
                PhoneNumber = @PhoneNumber,
                MobileNumber = @MobileNumber,
                EmergencyContactNumber = @EmergencyContactNumber,
                EmergencyContactPerson = @EmergencyContactPerson,
                FatherName = @FatherName,
                MotherName = @MotherName,
                SpouseName = @SpouseName,
                PANNumber = @PANNumber,
                LockerNumber = @LockerNumber,
                TeamId = @TeamId,
                BusinessUnitId = @BusinessUnitId,
                CotivityEmployeeId = @CotivityEmployeeId,
                IsActive = @IsActive,
                IsDeleted = CASE
                    WHEN @IncludeDeleted = 0 THEN IsDeleted
                    WHEN @IsActive = 1 THEN 0
                    ELSE 1
                END,
                DeletedDate = CASE
                    WHEN @IncludeDeleted = 0 THEN DeletedDate
                    WHEN @IsActive = 1 THEN NULL
                    ELSE COALESCE(DeletedDate, SYSUTCDATETIME())
                END,
                DeletedBy = CASE
                    WHEN @IncludeDeleted = 0 THEN DeletedBy
                    WHEN @IsActive = 1 THEN NULL
                    ELSE COALESCE(DeletedBy, @LastModifiedBy)
                END,
                LastModifiedDate = @LastModifiedDate,
                LastModifiedBy = @LastModifiedBy
            WHERE EmployeeId = @EmployeeId AND (@IncludeDeleted = 1 OR IsDeleted = 0);
            ";
        const string deleteAddressesSql = "DELETE FROM EmployeeAddresses WHERE EmployeeId = @EmployeeId;";
        const string deleteSkillsSql = "DELETE FROM EmployeeSkills WHERE EmployeeId = @EmployeeId;";
        const string deleteUnselectedSkillsSql = @"
            DELETE FROM EmployeeSkills
            WHERE EmployeeId = @EmployeeId AND SkillId NOT IN @SkillIds;
            ";
        const string updateSkillSql = @"
            UPDATE EmployeeSkills
            SET SkillLevel = @SkillLevel, IsPrimary = @IsPrimary,
                LastModifiedDate = @LastModifiedDate, LastModifiedBy = @LastModifiedBy
            WHERE EmployeeId = @EmployeeId AND SkillId = @SkillId;
            ";
        const string deleteDressesSql = "DELETE FROM EmployeeDresses WHERE EmployeeId = @EmployeeId;";
        const string addressSql = @"
            INSERT INTO EmployeeAddresses
                (EmployeeId, AddressTypeId, AddressLine1, AddressLine2, City, State,
                 Latitude, Longitude, IsPrimary, CreatedDate, CreatedBy)
            VALUES
                (@EmployeeId, @AddressTypeId, @AddressLine1, @AddressLine2, @City, @State,
                 @Latitude, @Longitude, @IsPrimary, @CreatedDate, @CreatedBy);
            ";
        const string insertSkillSql = @"
            INSERT INTO EmployeeSkills
                (EmployeeId, SkillId, SkillLevel, IsPrimary, CreatedDate, CreatedBy)
            VALUES (@EmployeeId, @SkillId, @SkillLevel, @IsPrimary, @CreatedDate, @CreatedBy);
            ";
        const string dressSql = @"
            INSERT INTO EmployeeDresses (EmployeeId, DressId, CreatedDate, CreatedBy)
            VALUES (@EmployeeId, @DressId, @CreatedDate, @CreatedBy);
            ";
        const string deleteEducationsSql = "DELETE FROM EmployeeEducation WHERE EmployeeId = @EmployeeId;";
        const string deleteTrainingsSql = "DELETE FROM EmployeeTraining WHERE EmployeeId = @EmployeeId;";
        const string educationSql = @"
            INSERT INTO EmployeeEducation
                (EmployeeId, Qualification, BoardUniversity, Institution, Faculty, PassedYear,
                 JoinedDate, PassoutDate, JoinedDateBS, PassoutDateBS, DivisionGrade, Remarks, CreatedDate, CreatedBy)
            VALUES (@EmployeeId, @Qualification, @BoardUniversity, @Institution, @Faculty, @PassedYear,
                    @JoinedDate, @PassoutDate, @JoinedDateBS, @PassoutDateBS, @DivisionGrade, @Remarks, @CreatedDate, @CreatedBy);
            ";
        const string trainingSql = @"
            INSERT INTO EmployeeTraining
                (EmployeeId, TrainingTitle, TrainingProvider, TrainingType, TrainingLocation,
                 JoinedDate, PassoutDate, JoinedDateBS, PassoutDateBS, Duration, CertificateNo, Remarks, CreatedDate, CreatedBy)
            VALUES (@EmployeeId, @TrainingTitle, @TrainingProvider, @TrainingType, @TrainingLocation,
                    @JoinedDate, @PassoutDate, @JoinedDateBS, @PassoutDateBS, @Duration, @CertificateNo, @Remarks, @CreatedDate, @CreatedBy);
            ";

        var parameters = new
        {
            employee.EmployeeId,
            employee.ENumber,
            employee.FirstName,
            employee.MiddleName,
            employee.LastName,
            JoinedDate = employee.JoinedDate.ToDateTime(TimeOnly.MinValue),
            employee.JoinedDateBS,
            employee.Email,
            employee.AlternateEmail,
            employee.DesignationId,
            employee.DepartmentId,
            employee.JobTypeId,
            employee.BloodGroupId,
            employee.GenderId,
            employee.ReportsToEmployeeId,
            employee.LeadEmployeeId,
            employee.StationNumber,
            employee.RecruitmentId,
            employee.ExtensionNumber,
            employee.CitizenshipNumber,
            DateOfBirth = employee.DateOfBirth?.ToDateTime(TimeOnly.MinValue),
            employee.DateOfBirthBS,
            employee.PhoneNumber,
            employee.MobileNumber,
            employee.EmergencyContactNumber,
            employee.EmergencyContactPerson,
            employee.FatherName,
            employee.MotherName,
            employee.SpouseName,
            employee.PANNumber,
            employee.LockerNumber,
            employee.TeamId,
            employee.BusinessUnitId,
            employee.CotivityEmployeeId,
            employee.IsActive,
            employee.LastModifiedDate,
            employee.LastModifiedBy,
            IncludeDeleted = includeDeleted
        };

        using var connection = connectionFactory.CreateConnection();
        connection.Open();
        using var transaction = connection.BeginTransaction();
        try
        {
            var affectedRows = await connection.ExecuteAsync(new CommandDefinition(
                employeeSql, parameters, transaction, cancellationToken: cancellationToken));
            if (affectedRows == 0)
            {
                transaction.Rollback();
                return false;
            }

            var employeeParameter = new { employee.EmployeeId };
            await connection.ExecuteAsync(new CommandDefinition(deleteAddressesSql, employeeParameter, transaction, cancellationToken: cancellationToken));
            if (employee.Skills.Count == 0)
            {
                await connection.ExecuteAsync(new CommandDefinition(deleteSkillsSql, employeeParameter, transaction, cancellationToken: cancellationToken));
            }
            else
            {
                await connection.ExecuteAsync(new CommandDefinition(
                    deleteUnselectedSkillsSql,
                    new { employee.EmployeeId, SkillIds = employee.Skills.Select(skill => skill.SkillId).ToArray() },
                    transaction,
                    cancellationToken: cancellationToken));
            }
            await connection.ExecuteAsync(new CommandDefinition(deleteDressesSql, employeeParameter, transaction, cancellationToken: cancellationToken));
            await connection.ExecuteAsync(new CommandDefinition(deleteEducationsSql, employeeParameter, transaction, cancellationToken: cancellationToken));
            await connection.ExecuteAsync(new CommandDefinition(deleteTrainingsSql, employeeParameter, transaction, cancellationToken: cancellationToken));

            foreach (var address in employee.Addresses)
            {
                await connection.ExecuteAsync(new CommandDefinition(addressSql, new
                {
                    employee.EmployeeId,
                    address.AddressTypeId,
                    address.AddressLine1,
                    address.AddressLine2,
                    address.City,
                    address.State,
                    address.Latitude,
                    address.Longitude,
                    address.IsPrimary,
                    address.CreatedDate,
                    address.CreatedBy
                }, transaction, cancellationToken: cancellationToken));
            }

            foreach (var skill in employee.Skills)
            {
                var skillParameters = new
                {
                    employee.EmployeeId,
                    skill.SkillId,
                    skill.SkillLevel,
                    skill.IsPrimary,
                    LastModifiedDate = employee.LastModifiedDate,
                    LastModifiedBy = employee.LastModifiedBy,
                    skill.CreatedDate,
                    skill.CreatedBy
                };
                var updatedSkillRows = await connection.ExecuteAsync(new CommandDefinition(
                    updateSkillSql, skillParameters, transaction, cancellationToken: cancellationToken));
                if (updatedSkillRows == 0)
                {
                    await connection.ExecuteAsync(new CommandDefinition(
                        insertSkillSql, skillParameters, transaction, cancellationToken: cancellationToken));
                }
            }

            foreach (var dress in employee.DressSizes)
            {
                await connection.ExecuteAsync(new CommandDefinition(dressSql, new
                {
                    employee.EmployeeId,
                    dress.DressId,
                    dress.CreatedDate,
                    dress.CreatedBy
                }, transaction, cancellationToken: cancellationToken));
            }

            foreach (var education in employee.Educations)
            {
                await connection.ExecuteAsync(new CommandDefinition(educationSql, new
                {
                    employee.EmployeeId,
                    education.Qualification,
                    education.BoardUniversity,
                    education.Institution,
                    education.Faculty,
                    education.PassedYear,
                    JoinedDate = education.JoinedDate?.ToDateTime(TimeOnly.MinValue),
                    PassoutDate = education.PassoutDate?.ToDateTime(TimeOnly.MinValue),
                    education.JoinedDateBS,
                    education.PassoutDateBS,
                    education.DivisionGrade,
                    education.Remarks,
                    education.CreatedDate,
                    education.CreatedBy
                }, transaction, cancellationToken: cancellationToken));
            }

            foreach (var training in employee.Trainings)
            {
                await connection.ExecuteAsync(new CommandDefinition(trainingSql, new
                {
                    employee.EmployeeId,
                    training.TrainingTitle,
                    training.TrainingProvider,
                    training.TrainingType,
                    training.TrainingLocation,
                    JoinedDate = training.JoinedDate?.ToDateTime(TimeOnly.MinValue),
                    PassoutDate = training.PassoutDate?.ToDateTime(TimeOnly.MinValue),
                    training.JoinedDateBS,
                    training.PassoutDateBS,
                    training.Duration,
                    training.CertificateNo,
                    training.Remarks,
                    training.CreatedDate,
                    training.CreatedBy
                }, transaction, cancellationToken: cancellationToken));
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

public async Task<EmployeeMastersResponse> GetEmployeeMastersAsync(CancellationToken cancellationToken)
{
    const string sql = @"
        SELECT
            DepartmentId,
            DepartmentName,IsActive
        FROM Departments
        WHERE IsActive = 1
        ORDER BY DepartmentName;

        SELECT
            DesignationId,
            DesignationName,
            Rank,IsActive
        FROM Designations
        WHERE IsActive = 1
        ORDER BY Rank;

        SELECT
            JobTypeId,
            JobTypeName,IsActive
        FROM JobTypes
        WHERE IsActive = 1
        ORDER BY JobTypeName;

        SELECT
            BloodGroupId,
            BloodGroupName,IsActive
        FROM BloodGroups
        WHERE IsActive = 1
        ORDER BY BloodGroupName;

        SELECT
            GenderId,
            GenderName,IsActive
        FROM Gender
        WHERE IsActive = 1
        ORDER BY GenderName;

        SELECT
            RoleId,
            RoleName,IsActive
        FROM Roles
        WHERE IsActive = 1
        ORDER BY RoleName;

        SELECT
            TeamId,
            TeamName,IsActive
        FROM Teams
        WHERE IsActive = 1
        ORDER BY TeamName;

        SELECT
            SkillId,
            SkillName,IsActive
        FROM Skills
        WHERE IsActive = 1
        ORDER BY SkillName;

        SELECT
            RecruitmentId,
            RecruitmentName,IsActive
        FROM Recruitments
        WHERE IsActive = 1
        ORDER BY RecruitmentName;

        SELECT
            EmployeeId as ReportingManagerId,
            CONCAT_WS(' ', FirstName, MiddleName, LastName) AS ReportingManagerName
        FROM Employees
        WHERE IsActive = 1
        ORDER BY FirstName, LastName;

        SELECT
            CotivityEmployeeId as ReportingManagerId,
            CotivityEmployeeName AS ReportingManagerName
        FROM CotivityEmployees
        ORDER BY CotivityEmployeeName;

        SELECT
            BusinessUnitId ,
            BusinessUnitName,
            COALESCE(IsActive, 1) AS IsActive
        FROM BusinessUnits
        WHERE COALESCE(IsActive, 1) = 1
        ORDER BY BusinessUnitName;

        SELECT
            DressId,
            DressName,
            Size,
            IsActive
        FROM DressSizes
        WHERE IsActive = 1
        ORDER BY DressName, Size;

                           SELECT
            AddressTypeId ,
          AddressTypeName
            FROM AddressType
                WHERE IsActive = 1
           ORDER BY AddressTypeName;
        ";



    using var connection = connectionFactory.CreateConnection();

    using var multi = await connection.QueryMultipleAsync(
        new CommandDefinition(
            sql,
            cancellationToken: cancellationToken));

    return new EmployeeMastersResponse
    {
        Departments = await multi.ReadAsync<GetDepartmentDto>(),

        Designations = await multi.ReadAsync<GetDesignationDto>(),

        JobTypes = await multi.ReadAsync<GetJobTypeDto>(),

        BloodGroups = await multi.ReadAsync<GetBloodGroupDto>(),

        Genders = await multi.ReadAsync<GetGenderDto>(),

        Roles = await multi.ReadAsync<GetRoleDto>(),

        Teams = await multi.ReadAsync<GetTeamDto>(),

        Skills = await multi.ReadAsync<GetSkillDto>(),

        Recruitments = await multi.ReadAsync<GetRecruitmentDto>(),

        Employees = await multi.ReadAsync<EmployeeReportingManagerDto>(),

        CotivityEmployees=await multi.ReadAsync<EmployeeReportingManagerDto>(),
        Businesses=await multi.ReadAsync<BusinessUnitDto>(),
        DressSizes=await multi.ReadAsync<EmployeeDressSizesDto>(),
        AddressTypes=await multi.ReadAsync<AddressTypeDto>(),
    };
}
    public async Task<bool> DeleteAsync(
        int employeeId,
        string deletedBy,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            UPDATE dbo.Employees
            SET IsDeleted = 1,
                IsActive = 0,
                DeletedDate = SYSUTCDATETIME(),
                DeletedBy = @DeletedBy,
                LastModifiedDate = SYSUTCDATETIME(),
                LastModifiedBy = @DeletedBy
            WHERE EmployeeId = @EmployeeId AND IsDeleted = 0;
            ";

        using var connection = connectionFactory.CreateConnection();
        var affectedRows = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { EmployeeId = employeeId, DeletedBy = deletedBy },
            cancellationToken: cancellationToken));

        return affectedRows > 0;
    }

    public async Task AssignEmployeeRolesAsync(int employeeId, IReadOnlyCollection<int> roleIds, string? email, string modifiedBy, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        var distinctRoleIds = roleIds
            .Where(roleId => roleId > 0)
            .Distinct()
            .ToArray();

        using var connection = connectionFactory.CreateConnection();

        const string ensureAndClearSql = @"
            DECLARE @UserId INT;
            SELECT @UserId = UserId FROM dbo.Users WHERE EmployeeId = @EmployeeId;

            IF @UserId IS NULL AND NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Username = @Username)
            BEGIN
                INSERT INTO dbo.Users (EmployeeId, Username, Email, IsActive, CreatedDate)
                VALUES (@EmployeeId, @Username, @Email, 1, SYSUTCDATETIME());
                SET @UserId = SCOPE_IDENTITY();
            END

            IF @UserId IS NOT NULL
                DELETE FROM dbo.UserRoles WHERE UserId = @UserId;";
        await connection.ExecuteAsync(new CommandDefinition(
            ensureAndClearSql,
            new { EmployeeId = employeeId, Username = email, Email = email },
            cancellationToken: cancellationToken));

        if (distinctRoleIds.Length == 0)
        {
            return;
        }

        const string insertSql = @"
            DECLARE @UserId INT;
            SELECT @UserId = UserId FROM dbo.Users WHERE EmployeeId = @EmployeeId;

            IF @UserId IS NOT NULL
                INSERT INTO dbo.UserRoles (UserId, RoleId, CreatedBy, CreatedDate)
                SELECT @UserId, r.RoleId, @ModifiedBy, SYSUTCDATETIME()
                FROM dbo.Roles r
                WHERE r.IsActive = 1 AND r.RoleId IN @RoleIds;";
        await connection.ExecuteAsync(new CommandDefinition(
            insertSql,
            new { EmployeeId = employeeId, ModifiedBy = modifiedBy, RoleIds = distinctRoleIds },
            cancellationToken: cancellationToken));
    }

    public async Task<int> CreateImageAsync(EmployeeImage image, CancellationToken cancellationToken = default)
    {
        const string sql = @"
            INSERT INTO EmployeeImages
                (EmployeeId, ImagePath, CreatedDate, CreatedBy)
            OUTPUT INSERTED.ImageId
            VALUES (@EmployeeId, @ImagePath, @CreatedDate, @CreatedBy);
            ";

        using var connection = connectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<int>(new CommandDefinition(sql, image, cancellationToken: cancellationToken));
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

    private sealed class EmployeeDetailRow
    {
        public int EmployeeId { get; set; }
        public int ENumber { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string? MiddleName { get; set; }
        public string LastName { get; set; } = string.Empty;
        public DateTime JoinedDate { get; set; }
        public string? JoinedDateBS { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? AlternateEmail { get; set; }
        public int DesignationId { get; set; }
        public string? DesignationName { get; set; }
        public int DepartmentId { get; set; }
        public string? DepartmentName { get; set; }
        public int JobTypeId { get; set; }
        public string? JobTypeName { get; set; }
        public int BloodGroupId { get; set; }
        public string? BloodGroupName { get; set; }
        public int GenderId { get; set; }
        public string? GenderName { get; set; }
        public int? ReportsToEmployeeId { get; set; }
        public string? ReportsToEmployeeName { get; set; }
        public int? LeadEmployeeId { get; set; }
        public string? LeadEmployeeName { get; set; }
        public string? StationNumber { get; set; }
        public int? RecruitmentId { get; set; }
        public string? RecruitmentName { get; set; }
        public int ExtensionNumber { get; set; }
        public string CitizenshipNumber { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string? DateOfBirthBS { get; set; }
        public string? PhoneNumber { get; set; }
        public string? MobileNumber { get; set; }
        public string? EmergencyContactNumber { get; set; }
        public string? EmergencyContactPerson { get; set; }
        public string? FatherName { get; set; }
        public string? MotherName { get; set; }
        public string? SpouseName { get; set; }
        public string PANNumber { get; set; } = string.Empty;
        public string? LockerNumber { get; set; }
        public string? RoleName { get; set; }
        public int TeamId { get; set; }
        public string? TeamName { get; set; }
        public int? BusinessUnitId { get; set; }
        public string? BusinessUnitName { get; set; }
        public int? CotivityEmployeeId { get; set; }
        public string? CotivityEmployeeName { get; set; }
        public bool IsActive { get; set; }
        public string? ImagePath { get; set; }
    }

    private sealed class EmployeeGridRow
    {
        public int Id { get; set; }
        public int ENumber { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Designation { get; set; }
        public int DesignationRank { get; set; }
        public string? Department { get; set; }
        public DateTime JoinedDate { get; set; }
        public bool IsActive { get; set; }
        public string? Gender { get; set; }
        public int? ReportsToEmployeeId { get; set; }
        public string? ReportsToEmployee { get; set; }
        public string? LeadEmployee { get; set; }
        public string? StationNumber { get; set; }
        public int? RecruitmentId { get; set; }
        public int ExtensionNumber { get; set; }
        public string CitizenshipNumber { get; set; } = string.Empty;
        public DateTime? DateOfBirth { get; set; }
        public string PANNumber { get; set; } = string.Empty;
        public string? LockerNumber { get; set; }
        public string? RoleName { get; set; }
        public string? TeamName { get; set; }
        public int? BusinessUnitId { get; set; }
        public string? BusinessUnitName { get; set; }
        public string? ReportToCotivityEmployee { get; set; }
        public string? Address { get; set; }
        public string? SkillName { get; set; }
        public bool SkillIsPrimary { get; set; }
        public string? ImagePath { get; set; }
    }

    private sealed class OrganizationTreeRow
    {
        public string DepartmentName { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class EmployeeHierarchyEmployeeRow
    {
        public int EmployeeId { get; set; }
        public int? ParentId { get; set; }
        public string EmployeeName { get; set; } = string.Empty;
        public string? DesignationName { get; set; }
        public string? DepartmentName { get; set; }
        public string? ImagePath { get; set; }
        public int EmployeeNumber { get; set; }
        public string Source { get; set; } = "Employee";
        public int Level { get; set; }
    }

    private sealed class EmployeeHierarchyCotivityRow
    {
        public int CotivityEmployeeId { get; set; }
        public int? ParentId { get; set; }
        public string CotivityEmployeeName { get; set; } = string.Empty;
        public string? CotivityDesignationName { get; set; }
        public string? CotivityDepartmentName { get; set; }
        public string? ImagePath { get; set; }
        public string Source { get; set; } = "Cotivity";
        public int Level { get; set; }
    }

}
