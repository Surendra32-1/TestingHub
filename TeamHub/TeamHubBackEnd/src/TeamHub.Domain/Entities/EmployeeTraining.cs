namespace TeamHub.Domain.Entities;

public class EmployeeTraining : AuditableEntity<int>
{
    public int TrainingId { get; set; }

    public override int Id
    {
        get => TrainingId;
        set => TrainingId = value;
    }

    public int EmployeeId { get; set; }
    public string TrainingTitle { get; set; } = string.Empty;
    public string? TrainingProvider { get; set; }
    public string? TrainingType { get; set; }
    public string? TrainingLocation { get; set; }
    public DateOnly? JoinedDate { get; set; }
    public DateOnly? PassoutDate { get; set; }
    public string? JoinedDateBS { get; set; }
    public string? PassoutDateBS { get; set; }
    public string? Duration { get; set; }
    public string? CertificateNo { get; set; }
    public string? Remarks { get; set; }
    public Employee? Employee { get; set; }
}
