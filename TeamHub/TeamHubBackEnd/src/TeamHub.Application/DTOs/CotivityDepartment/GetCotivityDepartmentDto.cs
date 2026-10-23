namespace TeamHub.Application.DTOs.CotivityDepartment;

public class GetCotivityDepartmentDto
{
    public int CotivityDepartmentId { get; set; }

    public string CotivityDepartmentName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}