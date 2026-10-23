namespace TeamHub.Application.DTOs.EmployeeRelease;

public sealed record EmployeeReleaseResponse(
    int ReleasedId,
    int EmployeeId,
    string ReleaseType,
    DateOnly? ResignationDate,
    DateOnly? NoticeDate,
    DateOnly? LastWorkingDate,
    int? NoticePeriodDays,
    string? Reason,
    string? Remarks,
    bool? KnowledgeTransferCompleted,
    bool? AssetsReturned,
    bool? ExitInterviewCompleted,
    decimal? SettlementAmount,
    bool? RelievingLetterIssued,
    bool? ExperienceLetterIssued,
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
    public int? ServedNoticeDays { get; init; }
    public string? EarlyReleaseReason { get; init; }
}
