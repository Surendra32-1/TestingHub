namespace TeamHub.Application.DTOs.JobType;

public class GetJobTypeDto
{
    public int JobTypeId { get; set; }

    public string JobTypeName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}