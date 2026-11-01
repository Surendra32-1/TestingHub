namespace TeamHub.Domain.Entities;

public class EmployeeEducation : AuditableEntity<int>
{
    public int EducationId { get; set; }

    public override int Id
    {
        get => EducationId;
        set => EducationId = value;
    }

    public int EmployeeId { get; set; }
    public string Qualification { get; set; } = string.Empty;
    public string? BoardUniversity { get; set; }
    public string? Institution { get; set; }
    public string? Faculty { get; set; }
    public int? PassedYear { get; set; }
    public DateOnly? JoinedDate { get; set; }
    public DateOnly? PassoutDate { get; set; }
    public string? JoinedDateBS { get; set; }
    public string? PassoutDateBS { get; set; }
    public string? DivisionGrade { get; set; }
    public string? Remarks { get; set; }
    public Employee? Employee { get; set; }
}
