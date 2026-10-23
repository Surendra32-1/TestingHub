using TeamHub.Application.DTOs.Department;
using TeamHub.Application.DTOs.Designation;
using TeamHub.Application.DTOs.JobType ;
using TeamHub.Application.DTOs.BloodGroup ;
using TeamHub.Application.DTOs.Gender ;
using TeamHub.Application.DTOs.Role ;
using TeamHub.Application.DTOs.Team ;
using TeamHub.Application.DTOs.Skill ;
using TeamHub.Application.DTOs.Recruitment;
using TeamHub.Application.DTOs.Employee ;

namespace TeamHub.Application.DTOs.Employees;

public sealed class EmployeeMastersResponse
{
    public IEnumerable<GetDepartmentDto> Departments { get; set; } = [];
    public IEnumerable<GetDesignationDto> Designations { get; set; } = [];
    public IEnumerable<GetJobTypeDto> JobTypes { get; set; } = [];
    public IEnumerable<GetBloodGroupDto> BloodGroups { get; set; } = [];
    public IEnumerable<GetGenderDto> Genders { get; set; } = [];
    public IEnumerable<GetRoleDto> Roles { get; set; } = [];
    public IEnumerable<GetTeamDto> Teams { get; set; } = [];
    public IEnumerable<GetSkillDto> Skills { get; set; } = [];
    public IEnumerable<GetRecruitmentDto> Recruitments { get; set; } = [];
    public IEnumerable<EmployeeReportingManagerDto> Employees { get; set; } = [];
    public IEnumerable<EmployeeReportingManagerDto> CotivityEmployees { get; set; } = [];
    public IEnumerable<EmployeeDressSizesDto> DressSizes { get; set; } = [];
    public IEnumerable<BusinessUnitDto> Businesses { get; set; } = [];
    public IEnumerable<AddressTypeDto> AddressTypes { get; set; } = [];
}
