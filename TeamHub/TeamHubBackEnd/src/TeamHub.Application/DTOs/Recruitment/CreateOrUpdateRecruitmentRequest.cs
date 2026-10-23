namespace TeamHub.Application.DTOs.Recruitment;

public class CreateOrUpdateRecruitmentRequest
{
    public string RecruitmentName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}