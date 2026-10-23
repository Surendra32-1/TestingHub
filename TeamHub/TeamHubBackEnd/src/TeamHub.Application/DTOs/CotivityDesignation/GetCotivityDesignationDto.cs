namespace TeamHub.Application.DTOs.CotivityDesignation;

public class GetCotivityDesignationDto
{
    public int CotivityDesignationId { get; set; }

    public string CotivityDesignationName { get; set; } = string.Empty;

    public int CotivityRank { get; set; }

    public bool IsActive { get; set; }
}