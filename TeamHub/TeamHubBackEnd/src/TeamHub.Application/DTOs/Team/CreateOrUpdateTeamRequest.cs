namespace TeamHub.Application.DTOs.Team;

public class CreateOrUpdateTeamRequest
{
    public string TeamName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}