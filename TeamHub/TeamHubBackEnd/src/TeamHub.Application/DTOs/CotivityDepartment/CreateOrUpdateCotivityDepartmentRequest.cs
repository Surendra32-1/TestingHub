namespace TeamHub.Application.DTOs.CotivityDepartment;

public class CreateOrUpdateCotivityDepartmentRequest
{
    public string CotivityDepartmentName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}