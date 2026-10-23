using System.ComponentModel.DataAnnotations;

namespace TeamHub.Application.DTOs.EmployeeTermination;

public sealed record UpdateTerminationStatusRequest
{
    [Required]
    [RegularExpression(
        "^(Draft|Submitted|PendingApproval|Approved|Terminated|Rejected|Cancelled)$",
        ErrorMessage = "Invalid status.")]
    public required string Status { get; init; }
}

public sealed record UpdateTerminationClearanceRequest
{
    public bool? AssetRecovered { get; init; }
    public bool? SystemAccessRevoked { get; init; }
    public bool? ExitInterviewCompleted { get; init; }
    public bool? TerminationLetterIssued { get; init; }

    [Range(typeof(decimal), "0", "9999999999999999.99",
        ErrorMessage = "Settlement Amount must be a positive value.")]
    public decimal? SettlementAmount { get; init; }
}
