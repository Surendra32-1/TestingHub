using System.ComponentModel.DataAnnotations;

namespace TeamHub.Application.DTOs.EmployeeRelease;

public sealed record UpdateReleaseStatusRequest
{
    [Required]
    [RegularExpression(
        "^(Draft|Submitted|PendingApproval|Approved|Rejected|Released|Cancelled)$",
        ErrorMessage = "Invalid status.")]
    public required string Status { get; init; }
}

public sealed record UpdateReleaseClearanceRequest
{
    public bool? KnowledgeTransferCompleted { get; init; }
    public bool? AssetsReturned { get; init; }
    public bool? ExitInterviewCompleted { get; init; }
    public bool? RelievingLetterIssued { get; init; }
    public bool? ExperienceLetterIssued { get; init; }

    [Range(typeof(decimal), "0", "9999999999999999.99",
        ErrorMessage = "Settlement Amount must be a positive value.")]
    public decimal? SettlementAmount { get; init; }
}
