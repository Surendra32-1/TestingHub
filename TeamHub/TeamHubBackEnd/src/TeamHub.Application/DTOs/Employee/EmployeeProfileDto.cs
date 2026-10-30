namespace TeamHub.Application.DTOs.Employee;

public sealed record EmployeeProfileBloodGroupDto(int BloodGroupId, string BloodGroupName);

public sealed record EmployeeProfileSkillOptionDto(int SkillId, string SkillName, bool IsActive);

public sealed record EmployeeProfileDressSizeOptionDto(int DressId, string DressName, string Size, bool IsActive);

public sealed record EmployeeProfileRoleDto(int RoleId, string RoleName);

public sealed record EmployeeProfileSkillUpdateDto(int SkillId, int SkillLevel);

public sealed record EmployeeProfileDto(
    string? Username,
    GetEmployeeDto Employee,
    IReadOnlyList<EmployeeProfileBloodGroupDto> BloodGroups,
    IReadOnlyList<EmployeeProfileRoleDto> Roles,
    IReadOnlyList<EmployeeProfileSkillOptionDto> SkillOptions,
    IReadOnlyList<EmployeeProfileDressSizeOptionDto> DressSizeOptions);

public sealed record EmployeeProfileSummaryDto(
    string EmployeeName,
    string? ImageBase64);

public sealed record EmployeeProfilePatch(
    bool AlternateEmailSpecified,
    string? AlternateEmail,
    bool BloodGroupIdSpecified,
    int BloodGroupId,
    bool MobileNumberSpecified,
    string? MobileNumber,
    bool EmergencyContactNumberSpecified,
    string? EmergencyContactNumber,
    bool MotherNameSpecified,
    string? MotherName,
    bool SpouseNameSpecified,
    string? SpouseName,
    bool LatitudeSpecified,
    decimal Latitude,
    bool LongitudeSpecified,
    decimal Longitude,
    bool ImageBase64Specified,
    string? ImageBase64,
    bool SkillsSpecified,
    IReadOnlyList<EmployeeProfileSkillUpdateDto> Skills,
    bool DressIdsSpecified,
    IReadOnlyList<int> DressIds,
    bool JoinedDateSpecified,
    DateOnly? JoinedDate,
    bool JoinedDateBSSpecified,
    string? JoinedDateBS,
    bool DateOfBirthSpecified,
    DateOnly? DateOfBirth,
    bool DateOfBirthBSSpecified,
    string? DateOfBirthBS);

public sealed record EmployeeProfileUpdateResult(
    EmployeeProfileDto? Profile,
    IReadOnlyDictionary<string, string[]> Errors);
