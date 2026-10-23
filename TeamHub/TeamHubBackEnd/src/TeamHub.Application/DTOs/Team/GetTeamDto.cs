namespace TeamHub.Application.DTOs.Team;

public class GetTeamDto
{
    public int TeamId { get; set; }

    public string TeamName { get; set; } = string.Empty;

    public bool IsActive { get; set; }
}