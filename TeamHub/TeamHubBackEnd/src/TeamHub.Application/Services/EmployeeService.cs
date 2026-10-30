using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Employee;
using TeamHub.Domain.Entities;
using TeamHub.Shared.Common.Services;
using TeamHub.Application.DTOs.Employees;
using TeamHub.Shared.Responses;

namespace TeamHub.Application.Services;

public sealed class EmployeeService(IEmployeeRepository repository) : IEmployeeService
{
    public Task<PagedResult<GetGridEmployeeDto>> GetAllAsync(
        CancellationToken cancellationToken = default,
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string searchField = "all",
        string? departmentName = null,
        string status = "active") =>
        repository.GetAllAsync(cancellationToken, pageNumber, pageSize, searchTerm, searchField, departmentName, status);

    public Task<PagedResult<GetGridEmployeeDto>> GetActiveByDepartmentIdAsync(
        int departmentId,
        CancellationToken cancellationToken = default,
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string searchField = "all") =>
        repository.GetActiveByDepartmentIdAsync(departmentId, cancellationToken, pageNumber, pageSize, searchTerm, searchField);

    public Task<PagedResult<GetActiveInactiveEmployeeDto>> GetAllActiveInactiveAsync(
        CancellationToken cancellationToken = default,
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string searchField = "all",
        string? departmentName = null,
        string status = "all") =>
        repository.GetAllActiveInactiveAsync(cancellationToken, pageNumber, pageSize, searchTerm, searchField, departmentName, status);

    public Task<GetEmployeeDto?> GetByIdAsync(int employeeId, CancellationToken cancellationToken = default) =>
        repository.GetByIdAsync(employeeId, cancellationToken);

    public Task<GetEmployeeDto?> GetActiveInactiveByIdAsync(int employeeId, CancellationToken cancellationToken = default) =>
        repository.GetActiveInactiveByIdAsync(employeeId, cancellationToken);

    public Task<bool> DeleteAsync(int employeeId, string deletedBy, CancellationToken cancellationToken = default) =>
        repository.DeleteAsync(employeeId, deletedBy, cancellationToken);

    public Task<IReadOnlyList<DepartmentTreeDto>> GetOrganizationTreeAsync(CancellationToken cancellationToken = default) =>
        repository.GetOrganizationTreeAsync(cancellationToken);

    public async Task<GetEmployeeDto> CreateAsync(CreateOrUpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        var employee = new Employee
        {
            ENumber = request.ENumber,
            FirstName = request.FirstName,
            MiddleName = request.MiddleName,
            LastName = request.LastName,
            JoinedDate = request.JoinedDate,
            JoinedDateBS = request.JoinedDateBS,
            Email = request.Email,
            AlternateEmail = request.AlternateEmail,
            DesignationId = request.DesignationId,
            DepartmentId = request.DepartmentId,
            JobTypeId = request.JobTypeId,
            BloodGroupId = request.BloodGroupId,
            GenderId = request.GenderId,
            ReportsToEmployeeId = request.ReportsToEmployeeId,
            LeadEmployeeId = request.LeadEmployeeId,
            StationNumber = request.StationNumber,
            RecruitmentId = request.RecruitmentId,
            ExtensionNumber = request.ExtensionNumber,
            CitizenshipNumber = request.CitizenshipNumber,
            DateOfBirth = request.DateOfBirth,
            DateOfBirthBS = request.DateOfBirthBS,
            PhoneNumber = request.PhoneNumber,
            MobileNumber = request.MobileNumber,
            EmergencyContactNumber = request.EmergencyContactNumber,
            EmergencyContactPerson = request.EmergencyContactPerson,
            FatherName = request.FatherName,
            MotherName = request.MotherName,
            SpouseName = request.SpouseName,
            PANNumber = request.PANNumber,
            LockerNumber = request.LockerNumber,
            TeamId = request.TeamId,
            BusinessUnitId = request.BusinessUnitId,
            CotivityEmployeeId = request.CotivityEmployeeId,
            IsActive = request.IsActive,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "System",
            Addresses = request.Addresses.Select(address => new EmployeeAddress
            {
                AddressTypeId = address.AddressTypeId,
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                City = address.City,
                State = address.State,
                Latitude = address.Latitude,
                Longitude = address.Longitude,
                IsPrimary = address.IsPrimary,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "System"
            }).ToList(),
            Skills = request.Skills.Select(skill => new EmployeeSkill
            {
                SkillId = skill.SkillId,
                SkillLevel = skill.SkillLevel,
                IsPrimary = skill.IsPrimary,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "System"
            }).ToList(),
            DressSizes = request.DressSizes.Select(dress => new EmployeeDress
            {
                DressId = dress.DressId,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "System"
            }).ToList()
        };

        employee.EmployeeId = await repository.CreateAsync(employee, cancellationToken);
        await repository.AssignEmployeeRolesAsync(employee.EmployeeId, request.RoleIds, request.Email, "System", cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            var imageFileName = $"employee-{employee.EmployeeId}-{Guid.NewGuid():N}";
            var imagePath = request.ImageBase64.ToFileUrl(FolderName.Infinite_Employee_FOLDER, imageFileName);

            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                await repository.CreateImageAsync(new EmployeeImage
                {
                    EmployeeId = employee.EmployeeId,
                    ImagePath = imagePath,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                }, cancellationToken);
            }
        }

        return new GetEmployeeDto
        {
            EmployeeId = employee.EmployeeId,
            ENumber = employee.ENumber,
            FirstName = employee.FirstName,
            MiddleName = employee.MiddleName,
            LastName = employee.LastName,
            JoinedDate = employee.JoinedDate,
            JoinedDateBS = employee.JoinedDateBS,
            Email = employee.Email,
            AlternateEmail = employee.AlternateEmail,
            DesignationId = employee.DesignationId,
            DepartmentId = employee.DepartmentId,
            JobTypeId = employee.JobTypeId,
            BloodGroupId = employee.BloodGroupId,
            GenderId = employee.GenderId,
            ReportsToEmployeeId = employee.ReportsToEmployeeId,
            LeadEmployeeId = employee.LeadEmployeeId,
            StationNumber = employee.StationNumber,
            RecruitmentId = employee.RecruitmentId,
            ExtensionNumber = employee.ExtensionNumber,
            CitizenshipNumber = employee.CitizenshipNumber,
            DateOfBirth = employee.DateOfBirth,
            DateOfBirthBS = employee.DateOfBirthBS,
            PhoneNumber = employee.PhoneNumber,
            MobileNumber = employee.MobileNumber,
            EmergencyContactNumber = employee.EmergencyContactNumber,
            EmergencyContactPerson = employee.EmergencyContactPerson,
            FatherName = employee.FatherName,
            MotherName = employee.MotherName,
            SpouseName = employee.SpouseName,
            PANNumber = employee.PANNumber,
            LockerNumber = employee.LockerNumber,
            RoleIds = request.RoleIds,
            TeamId = employee.TeamId,
            BusinessUnitId = employee.BusinessUnitId,
            CotivityEmployeeId = employee.CotivityEmployeeId,
            IsActive = employee.IsActive,
            ImageBase64 = request.ImageBase64,
            Addresses = employee.Addresses.Select(address => new EmployeeAddressDto
            {
                AddressId = address.AddressId,
                AddressTypeId = address.AddressTypeId,
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                City = address.City,
                State = address.State,
                Latitude = address.Latitude,
                Longitude = address.Longitude,
                IsPrimary = address.IsPrimary
            }).ToList(),
            Skills = employee.Skills.Select(skill => new EmployeeSkillDto
            {
                EmployeeSkillId = skill.EmployeeSkillId,
                SkillId = skill.SkillId,
                SkillLevel = skill.SkillLevel,
                IsPrimary = skill.IsPrimary
            }).ToList(),
            DressSizes = request.DressSizes
        };
    }

    public async Task<bool> UpdateAsync(int employeeId, CreateOrUpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        return await PersistUpdateAsync(employeeId, request, includeInactive: false, cancellationToken);
    }

    public async Task<bool> UpdateActiveInactiveAsync(int employeeId, CreateOrUpdateEmployeeRequest request, CancellationToken cancellationToken = default)
    {
        return await PersistUpdateAsync(employeeId, request, includeInactive: true, cancellationToken);
    }

    private async Task<bool> PersistUpdateAsync(
        int employeeId,
        CreateOrUpdateEmployeeRequest request,
        bool includeInactive,
        CancellationToken cancellationToken)
    {
        var employee = new Employee
        {
            EmployeeId = employeeId,
            ENumber = request.ENumber,
            FirstName = request.FirstName,
            MiddleName = request.MiddleName,
            LastName = request.LastName,
            JoinedDate = request.JoinedDate,
            JoinedDateBS = request.JoinedDateBS,
            Email = request.Email,
            AlternateEmail = request.AlternateEmail,
            DesignationId = request.DesignationId,
            DepartmentId = request.DepartmentId,
            JobTypeId = request.JobTypeId,
            BloodGroupId = request.BloodGroupId,
            GenderId = request.GenderId,
            ReportsToEmployeeId = request.ReportsToEmployeeId,
            LeadEmployeeId = request.LeadEmployeeId,
            StationNumber = request.StationNumber,
            RecruitmentId = request.RecruitmentId,
            ExtensionNumber = request.ExtensionNumber,
            CitizenshipNumber = request.CitizenshipNumber,
            DateOfBirth = request.DateOfBirth,
            DateOfBirthBS = request.DateOfBirthBS,
            PhoneNumber = request.PhoneNumber,
            MobileNumber = request.MobileNumber,
            EmergencyContactNumber = request.EmergencyContactNumber,
            EmergencyContactPerson = request.EmergencyContactPerson,
            FatherName = request.FatherName,
            MotherName = request.MotherName,
            SpouseName = request.SpouseName,
            PANNumber = request.PANNumber,
            LockerNumber = request.LockerNumber,
            TeamId = request.TeamId,
            BusinessUnitId = request.BusinessUnitId,
            CotivityEmployeeId = request.CotivityEmployeeId,
            IsActive = request.IsActive,
            LastModifiedDate = DateTime.UtcNow,
            LastModifiedBy = "System",
            Addresses = request.Addresses.Select(address => new EmployeeAddress
            {
                AddressTypeId = address.AddressTypeId,
                AddressLine1 = address.AddressLine1,
                AddressLine2 = address.AddressLine2,
                City = address.City,
                State = address.State,
                Latitude = address.Latitude,
                Longitude = address.Longitude,
                IsPrimary = address.IsPrimary,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "System"
            }).ToList(),
            Skills = request.Skills.Select(skill => new EmployeeSkill
            {
                SkillId = skill.SkillId,
                SkillLevel = skill.SkillLevel,
                IsPrimary = skill.IsPrimary,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "System"
            }).ToList(),
            DressSizes = request.DressSizes.Select(dress => new EmployeeDress
            {
                DressId = dress.DressId,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "System"
            }).ToList()
        };

        var updated = includeInactive
            ? await repository.UpdateActiveInactiveAsync(employee, cancellationToken)
            : await repository.UpdateAsync(employee, cancellationToken);
        if (updated)
        {
            await repository.AssignEmployeeRolesAsync(employeeId, request.RoleIds, request.Email, "System", cancellationToken);
        }

        if (updated && !string.IsNullOrWhiteSpace(request.ImageBase64))
        {
            var imageFileName = $"employee-{employeeId}-{Guid.NewGuid():N}";
            var imagePath = request.ImageBase64.ToFileUrl(FolderName.Infinite_Employee_FOLDER, imageFileName);
            if (!string.IsNullOrWhiteSpace(imagePath))
            {
                await repository.CreateImageAsync(new EmployeeImage
                {
                    EmployeeId = employeeId,
                    ImagePath = imagePath,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = "System"
                }, cancellationToken);
            }
        }

        return updated;
    }
    public async Task<EmployeeMastersResponse> GetEmployeeMastersAsync(CancellationToken cancellationToken = default)
    {
      return await repository.GetEmployeeMastersAsync();
    }
}
