using System.ComponentModel.DataAnnotations;

namespace TeamHub.Application.DTOs.EmployeeRelease;

public sealed record EmployeeReleaseRequest : IValidatableObject
{
    [Required]
    [RegularExpression(
        "^(Resignation|Retirement|Contract End|Internal Transfer|Mutual Separation)$",
        ErrorMessage = "Invalid release type.")]
    public required string ReleaseType { get; init; }

    [Required(ErrorMessage = "Resignation Date is required.")]
    public DateOnly? ResignationDate { get; init; }

    public DateOnly? NoticeDate { get; init; }

    [Required(ErrorMessage = "Last Working Day is required.")]
    public DateOnly? LastWorkingDate { get; init; }

    [Required(ErrorMessage = "Notice Period Days is required so early release can be detected.")]
    [Range(0, 180, ErrorMessage = "Notice Period Days must be between 0 and 180.")]
    public int? NoticePeriodDays { get; init; }

    [MaxLength(500, ErrorMessage = "Reason cannot exceed 500 characters.")]
    public string? Reason { get; init; }

    [MaxLength(500, ErrorMessage = "Early Release Reason cannot exceed 500 characters.")]
    public string? EarlyReleaseReason { get; init; }

    [MaxLength(2000, ErrorMessage = "Remarks cannot exceed 2000 characters.")]
    public string? Remarks { get; init; }

    public bool? KnowledgeTransferCompleted { get; init; }

    public bool? AssetsReturned { get; init; }

    public bool? ExitInterviewCompleted { get; init; }

    [Range(typeof(decimal), "0", "9999999999999999.99",
        ErrorMessage = "Settlement Amount must be a positive value.")]
    public decimal? SettlementAmount { get; init; }

    public bool? RelievingLetterIssued { get; init; }

    public bool? ExperienceLetterIssued { get; init; }

    [Required]
    [RegularExpression(
        "^(Draft|Submitted|PendingApproval|Approved|Rejected|Released|Cancelled)$",
        ErrorMessage = "Invalid status.")]
    public string Status { get; init; } = "Draft";

    public IEnumerable<ValidationResult> Validate(
        ValidationContext validationContext)
    {
        if (ResignationDate is { } resignationDate &&
            LastWorkingDate is { } lastWorkingDate)
        {
            if (lastWorkingDate < resignationDate)
            {
                yield return new ValidationResult(
                    "Last Working Day must be greater than or equal to Resignation Date.",
                    new[] { nameof(LastWorkingDate) });
            }
        }

        if (NoticeDate is { } noticeDate &&
            ResignationDate is { } resignationDate2 &&
            noticeDate < resignationDate2)
        {
            yield return new ValidationResult(
                "Notice Date cannot be earlier than Resignation Date.",
                new[] { nameof(NoticeDate) });
        }

        var hasReason = !string.IsNullOrWhiteSpace(Reason) && Reason.Trim().Length >= 10;
        var hasEarlyReleaseReason = !string.IsNullOrWhiteSpace(EarlyReleaseReason);
        if (!hasReason && !hasEarlyReleaseReason)
        {
            yield return new ValidationResult(
                "Provide either a Reason (at least 10 characters) or an Early Release Reason.",
                new[] { nameof(Reason), nameof(EarlyReleaseReason) });
        }
    }
}