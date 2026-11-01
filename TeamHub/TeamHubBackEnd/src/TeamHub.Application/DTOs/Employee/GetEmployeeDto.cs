namespace TeamHub.Application.DTOs.Employee;

public class GetEmployeeDto
{
    public int EmployeeId { get; set; }
    public int ENumber { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public DateOnly JoinedDate { get; set; }
    public string JoinedDateBS { get; set; } = string.Empty;
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
    public DateOnly? DateOfBirth { get; set; }
    public string? DateOfBirthBS { get; set; }
    public string? PhoneNumber { get; set; }
    public string? MobileNumber { get; set; }
    public string? EmergencyContactNumber { get; set; }
    public string EmergencyContactPerson { get; set; } = string.Empty;
    public string FatherName { get; set; } = string.Empty;
    public string? MotherName { get; set; }
    public string? SpouseName { get; set; }
    public string PANNumber { get; set; } = string.Empty;
    public string? LockerNumber { get; set; }
    public List<int> RoleIds { get; set; } = [];
    public string? RoleName { get; set; }
    public int TeamId { get; set; }
    public string? TeamName { get; set; }
    public int? BusinessUnitId { get; set; }
    public string? BusinessUnitName { get; set; }
    public int? CotivityEmployeeId { get; set; }
    public string? CotivityEmployeeName { get; set; }
    public bool IsActive { get; set; }
    public string? ImageBase64 { get; set; }
    public IReadOnlyList<EmployeeAddressDto> Addresses { get; set; } = [];
    public IReadOnlyList<EmployeeSkillDto> Skills { get; set; } = [];
    public IReadOnlyList<EmployeeDressSizesDto> DressSizes { get; set; } = [];
    public IReadOnlyList<EmployeeEducationDto> Educations { get; set; } = [];
    public IReadOnlyList<EmployeeTrainingDto> Trainings { get; set; } = [];
}