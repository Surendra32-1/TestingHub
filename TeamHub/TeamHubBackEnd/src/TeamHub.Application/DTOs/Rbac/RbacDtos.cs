namespace TeamHub.Application.DTOs.Rbac;

public class RoleOptionDto
{
    public int RoleId { get; set; }

    public string RoleName { get; set; } = string.Empty;
}

public class EmployeeRoleMatrixRowDto
{
    public int UserId { get; set; }

    public int EmployeeId { get; set; }

    public string Username { get; set; } = string.Empty;

    public string FullName { get; set; } = string.Empty;

    public List<int> RoleIds { get; set; } = [];
}

public class EmployeeRoleMatrixDto
{
    public List<RoleOptionDto> Roles { get; set; } = [];

    public List<EmployeeRoleMatrixRowDto> Rows { get; set; } = [];

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }
}

public class UserRoleAssignmentDto
{
    public int UserId { get; set; }

    public List<int> RoleIds { get; set; } = [];
}

public class SaveUserRolesRequest
{
    public List<UserRoleAssignmentDto> Assignments { get; set; } = [];
}

public class RolePermissionMatrixRowDto
{
    public int PermissionId { get; set; }

    public string PermissionCode { get; set; } = string.Empty;

    public string PermissionName { get; set; } = string.Empty;

    public string? Module { get; set; }

    public List<int> RoleIds { get; set; } = [];
}

public class RolePermissionMatrixDto
{
    public List<RoleOptionDto> Roles { get; set; } = [];

    public List<RolePermissionMatrixRowDto> Rows { get; set; } = [];

    public int PageNumber { get; set; }

    public int PageSize { get; set; }

    public int TotalCount { get; set; }

    public int TotalPages { get; set; }
}

public class RolePermissionAssignmentDto
{
    public int PermissionId { get; set; }

    public List<int> RoleIds { get; set; } = [];
}

public class SaveRolePermissionsRequest
{
    public List<RolePermissionAssignmentDto> Assignments { get; set; } = [];
}
