using System.ComponentModel.DataAnnotations;

namespace TeamHub.Application.DTOs.EmployeeTermination;

public sealed record EmployeeTerminationRequest : IValidatableObject
{
    [Required, RegularExpression("^(Performance|Policy Violation|PolicyViolation|Misconduct|Layoff|Contract End|Attendance|Other|Fraud|Absenteeism|Restructuring)$")]
    public required string TerminationType { get; init; }
    public DateOnly? EffectiveDate { get; init; }
    public bool? InvestigationRequired { get; init; }
    public string? InvestigationSummary { get; init; }
    [Range(0, int.MaxValue)]
    public int? WarningCount { get; init; }
    public DateOnly? LastWarningDate { get; init; }
    [MaxLength(500)]
    public string? Reason { get; init; }
    public string? Remarks { get; init; }
    public bool? AssetRecovered { get; init; }
    public bool? SystemAccessRevoked { get; init; }
    public bool? ExitInterviewCompleted { get; init; }
    [Range(typeof(decimal), "0", "9999999999999999.99")]
    public decimal? SettlementAmount { get; init; }
    public bool? EligibleForRehire { get; init; }
    public bool? TerminationLetterIssued { get; init; }
    public DateOnly? TerminationLetterDate { get; init; }
    [Required, RegularExpression("^(Draft|Submitted|PendingApproval|Pending Approval|UnderInvestigation|Approved|Completed|Terminated|Rejected|Cancelled)$")]
    public string Status { get; init; } = "Draft";

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (EffectiveDate is { } effectiveDate && effectiveDate > DateOnly.FromDateTime(DateTime.Today))
        {
            yield return new ValidationResult(
                "Termination Date cannot be in the future.",
                new[] { nameof(EffectiveDate) });
        }
    }
}
