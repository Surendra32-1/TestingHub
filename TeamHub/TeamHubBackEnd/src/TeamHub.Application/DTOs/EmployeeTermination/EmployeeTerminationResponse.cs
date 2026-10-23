namespace TeamHub.Application.DTOs.EmployeeTermination;

public sealed record EmployeeTerminationResponse(
    int TerminationId,
    int EmployeeId,
    string TerminationType,
    DateOnly? EffectiveDate,
    bool? InvestigationRequired,
    string? InvestigationSummary,
    int? WarningCount,
    DateOnly? LastWarningDate,
    string? Reason,
    string? Remarks,
    bool? AssetRecovered,
    bool? SystemAccessRevoked,
    bool? ExitInterviewCompleted,
    decimal? SettlementAmount,
    bool? EligibleForRehire,
    bool? TerminationLetterIssued,
    DateOnly? TerminationLetterDate,
    string Status,
    string? ApprovedBy,
    DateTimeOffset? ApprovedDate,
    string? CreatedBy,
    DateTimeOffset? CreatedDate,
    string? LastModifiedBy,
    DateTimeOffset? LastModifiedDate)
{
    public string EmployeeName { get; init; } = string.Empty;
    public int EmployeeCode { get; init; }
    public string? Department { get; init; }
    public string? Designation { get; init; }
    public string? ManagerName { get; init; }
    public DateOnly? JoinedDate { get; init; }
}
