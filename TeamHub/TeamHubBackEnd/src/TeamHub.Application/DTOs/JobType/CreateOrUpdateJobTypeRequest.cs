namespace TeamHub.Application.DTOs.JobType;

public class CreateOrUpdateJobTypeRequest
{
    public string JobTypeName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}