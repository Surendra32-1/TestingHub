namespace TeamHub.Application.DTOs.Recruitment;

public class GetRecruitmentDto
{
    public int RecruitmentId { get; set; }

    public string RecruitmentName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}