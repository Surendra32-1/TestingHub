using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.AddressType;
using TeamHub.Application.DTOs.Gender;
using TeamHub.Application.DTOs.JobType;
using TeamHub.Application.DTOs.Recruitment;
using TeamHub.Application.DTOs.Role;
using TeamHub.Application.DTOs.Skill;
using TeamHub.Application.DTOs.Team;
using TeamHub.Domain.Entities;

namespace TeamHub.Application.Services;

public sealed class GenderService(IGenderRepository repository) : IGenderService
{
    public Task<IReadOnlyList<GetGenderDto>> GetAllAsync(CancellationToken cancellationToken = default) => repository.GetAllAsync(cancellationToken);
    public Task<GetGenderDto?> GetByIdAsync(int genderId, CancellationToken cancellationToken = default) => repository.GetByIdAsync(genderId, cancellationToken);
    public async Task<GetGenderDto> CreateAsync(CreateOrUpdateGenderRequest request, CancellationToken cancellationToken = default)
    {
        var gender = new Gender { GenderName = request.GenderName, IsActive = request.IsActive, CreatedDate = DateTime.UtcNow, CreatedBy = "System" };
        gender.GenderId = await repository.CreateAsync(gender, cancellationToken);
        return new GetGenderDto { GenderId = gender.GenderId, GenderName = gender.GenderName, IsActive = gender.IsActive };
    }
    public Task<bool> UpdateAsync(int genderId, CreateOrUpdateGenderRequest request, CancellationToken cancellationToken = default) =>
        repository.UpdateAsync(new Gender { GenderId = genderId, GenderName = request.GenderName, IsActive = request.IsActive, LastModifiedDate = DateTime.UtcNow, LastModifiedBy = "System" }, cancellationToken);
    public Task<bool> DeleteAsync(int genderId, CancellationToken cancellationToken = default) => repository.DeleteAsync(genderId, cancellationToken);
}

public sealed class AddressTypeService(IAddressTypeRepository repository) : IAddressTypeService
{
    public Task<IReadOnlyList<GetAddressTypeDto>> GetAllAsync(CancellationToken cancellationToken = default) => repository.GetAllAsync(cancellationToken);
    public Task<GetAddressTypeDto?> GetByIdAsync(int addressTypeId, CancellationToken cancellationToken = default) => repository.GetByIdAsync(addressTypeId, cancellationToken);
    public async Task<GetAddressTypeDto> CreateAsync(CreateOrUpdateAddressTypeRequest request, CancellationToken cancellationToken = default)
    {
        var addressType = new AddressType { AddressTypeName = request.AddressTypeName, IsActive = request.IsActive, CreatedDate = DateTime.UtcNow, CreatedBy = "System" };
        addressType.AddressTypeId = await repository.CreateAsync(addressType, cancellationToken);
        return new GetAddressTypeDto { AddressTypeId = addressType.AddressTypeId, AddressTypeName = addressType.AddressTypeName, IsActive = addressType.IsActive };
    }
    public Task<bool> UpdateAsync(int addressTypeId, CreateOrUpdateAddressTypeRequest request, CancellationToken cancellationToken = default) =>
        repository.UpdateAsync(new AddressType { AddressTypeId = addressTypeId, AddressTypeName = request.AddressTypeName, IsActive = request.IsActive, LastModifiedDate = DateTime.UtcNow, LastModifiedBy = "System" }, cancellationToken);
    public Task<bool> DeleteAsync(int addressTypeId, CancellationToken cancellationToken = default) => repository.DeleteAsync(addressTypeId, cancellationToken);
}

public sealed class RoleService(IRoleRepository repository) : IRoleService
{
    public Task<IReadOnlyList<GetRoleDto>> GetAllAsync(CancellationToken cancellationToken = default) => repository.GetAllAsync(cancellationToken);
    public Task<GetRoleDto?> GetByIdAsync(int roleId, CancellationToken cancellationToken = default) => repository.GetByIdAsync(roleId, cancellationToken);
    public async Task<GetRoleDto> CreateAsync(CreateOrUpdateRoleRequest request, CancellationToken cancellationToken = default)
    {
        var role = new Role { RoleName = request.RoleName, IsActive = request.IsActive, CreatedDate = DateTime.UtcNow, CreatedBy = "System" };
        role.RoleId = await repository.CreateAsync(role, cancellationToken);
        return new GetRoleDto { RoleId = role.RoleId, RoleName = role.RoleName, IsActive = role.IsActive };
    }
    public Task<bool> UpdateAsync(int roleId, CreateOrUpdateRoleRequest request, CancellationToken cancellationToken = default) =>
        repository.UpdateAsync(new Role { RoleId = roleId, RoleName = request.RoleName, IsActive = request.IsActive, LastModifiedDate = DateTime.UtcNow, LastModifiedBy = "System" }, cancellationToken);
    public Task<bool> DeleteAsync(int roleId, CancellationToken cancellationToken = default) => repository.DeleteAsync(roleId, cancellationToken);
}

public sealed class TeamService(ITeamRepository repository) : ITeamService
{
    public Task<IReadOnlyList<GetTeamDto>> GetAllAsync(CancellationToken cancellationToken = default) => repository.GetAllAsync(cancellationToken);
    public Task<GetTeamDto?> GetByIdAsync(int teamId, CancellationToken cancellationToken = default) => repository.GetByIdAsync(teamId, cancellationToken);
    public async Task<GetTeamDto> CreateAsync(CreateOrUpdateTeamRequest request, CancellationToken cancellationToken = default)
    {
        var team = new Team { TeamName = request.TeamName, IsActive = request.IsActive, CreatedDate = DateTime.UtcNow, CreatedBy = "System" };
        team.TeamId = await repository.CreateAsync(team, cancellationToken);
        return new GetTeamDto { TeamId = team.TeamId, TeamName = team.TeamName, IsActive = team.IsActive };
    }
    public Task<bool> UpdateAsync(int teamId, CreateOrUpdateTeamRequest request, CancellationToken cancellationToken = default) =>
        repository.UpdateAsync(new Team { TeamId = teamId, TeamName = request.TeamName, IsActive = request.IsActive, LastModifiedDate = DateTime.UtcNow, LastModifiedBy = "System" }, cancellationToken);
    public Task<bool> DeleteAsync(int teamId, CancellationToken cancellationToken = default) => repository.DeleteAsync(teamId, cancellationToken);
}

public sealed class SkillService(ISkillRepository repository) : ISkillService
{
    public Task<IReadOnlyList<GetSkillDto>> GetAllAsync(CancellationToken cancellationToken = default) => repository.GetAllAsync(cancellationToken);
    public Task<GetSkillDto?> GetByIdAsync(int skillId, CancellationToken cancellationToken = default) => repository.GetByIdAsync(skillId, cancellationToken);
    public async Task<GetSkillDto> CreateAsync(CreateOrUpdateSkillRequest request, CancellationToken cancellationToken = default)
    {
        var skill = new Skill { SkillName = request.SkillName, IsActive = request.IsActive, CreatedDate = DateTime.UtcNow, CreatedBy = "System" };
        skill.SkillId = await repository.CreateAsync(skill, cancellationToken);
        return new GetSkillDto { SkillId = skill.SkillId, SkillName = skill.SkillName, IsActive = skill.IsActive };
    }
    public Task<bool> UpdateAsync(int skillId, CreateOrUpdateSkillRequest request, CancellationToken cancellationToken = default) =>
        repository.UpdateAsync(new Skill { SkillId = skillId, SkillName = request.SkillName, IsActive = request.IsActive, LastModifiedDate = DateTime.UtcNow, LastModifiedBy = "System" }, cancellationToken);
    public Task<bool> DeleteAsync(int skillId, CancellationToken cancellationToken = default) => repository.DeleteAsync(skillId, cancellationToken);
}

public sealed class RecruitmentService(IRecruitmentRepository repository) : IRecruitmentService
{
    public Task<IReadOnlyList<GetRecruitmentDto>> GetAllAsync(CancellationToken cancellationToken = default) => repository.GetAllAsync(cancellationToken);
    public Task<GetRecruitmentDto?> GetByIdAsync(int recruitmentId, CancellationToken cancellationToken = default) => repository.GetByIdAsync(recruitmentId, cancellationToken);
    public async Task<GetRecruitmentDto> CreateAsync(CreateOrUpdateRecruitmentRequest request, CancellationToken cancellationToken = default)
    {
        var recruitment = new Recruitment { RecruitmentName = request.RecruitmentName, IsActive = request.IsActive, CreatedDate = DateTime.UtcNow, CreatedBy = "System" };
        recruitment.RecruitmentId = await repository.CreateAsync(recruitment, cancellationToken);
        return new GetRecruitmentDto { RecruitmentId = recruitment.RecruitmentId, RecruitmentName = recruitment.RecruitmentName, IsActive = recruitment.IsActive };
    }
    public Task<bool> UpdateAsync(int recruitmentId, CreateOrUpdateRecruitmentRequest request, CancellationToken cancellationToken = default) =>
        repository.UpdateAsync(new Recruitment { RecruitmentId = recruitmentId, RecruitmentName = request.RecruitmentName, IsActive = request.IsActive, LastModifiedDate = DateTime.UtcNow, LastModifiedBy = "System" }, cancellationToken);
    public Task<bool> DeleteAsync(int recruitmentId, CancellationToken cancellationToken = default) => repository.DeleteAsync(recruitmentId, cancellationToken);
}

public sealed class JobTypeService(IJobTypeRepository repository) : IJobTypeService
{
    public Task<IReadOnlyList<GetJobTypeDto>> GetAllAsync(CancellationToken cancellationToken = default) => repository.GetAllAsync(cancellationToken);
    public Task<GetJobTypeDto?> GetByIdAsync(int jobTypeId, CancellationToken cancellationToken = default) => repository.GetByIdAsync(jobTypeId, cancellationToken);
    public async Task<GetJobTypeDto> CreateAsync(CreateOrUpdateJobTypeRequest request, CancellationToken cancellationToken = default)
    {
        var jobType = new JobType { JobTypeName = request.JobTypeName, IsActive = request.IsActive, CreatedDate = DateTime.UtcNow, CreatedBy = "System" };
        jobType.JobTypeId = await repository.CreateAsync(jobType, cancellationToken);
        return new GetJobTypeDto { JobTypeId = jobType.JobTypeId, JobTypeName = jobType.JobTypeName, IsActive = jobType.IsActive };
    }
    public Task<bool> UpdateAsync(int jobTypeId, CreateOrUpdateJobTypeRequest request, CancellationToken cancellationToken = default) =>
        repository.UpdateAsync(new JobType { JobTypeId = jobTypeId, JobTypeName = request.JobTypeName, IsActive = request.IsActive, LastModifiedDate = DateTime.UtcNow, LastModifiedBy = "System" }, cancellationToken);
    public Task<bool> DeleteAsync(int jobTypeId, CancellationToken cancellationToken = default) => repository.DeleteAsync(jobTypeId, cancellationToken);
}