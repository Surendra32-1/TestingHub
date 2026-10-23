namespace TeamHub.Application.Workflows;

public static class ReleaseStatusWorkflow
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string PendingApproval = "PendingApproval";
    public const string Approved = "Approved";
    public const string Released = "Released";
    public const string Rejected = "Rejected";
    public const string Cancelled = "Cancelled";

    public const string ResignationType = "Resignation";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Draft, Submitted, PendingApproval, Approved, Released, Rejected, Cancelled
    };

    public static readonly IReadOnlySet<string> AllowedInitial = new HashSet<string>(StringComparer.Ordinal)
    {
        Draft, Submitted
    };

    private static readonly Dictionary<string, string[]> Graph = new(StringComparer.Ordinal)
    {
        [Draft] = [Submitted, Cancelled],
        [Submitted] = [PendingApproval, Approved, Rejected, Cancelled],
        [PendingApproval] = [Approved, Rejected, Cancelled],
        [Approved] = [Released, Cancelled],
        [Released] = [],
        [Rejected] = [],
        [Cancelled] = [],
    };

    public static bool IsKnown(string? status) => status is not null && All.Contains(status);

    public static bool IsTerminal(string? status) =>
        status is not null && Graph.TryGetValue(status, out var next) && next.Length == 0;

    public static IReadOnlyList<string> NextStates(string status) =>
        Graph.TryGetValue(status, out var next) ? next : Array.Empty<string>();

    public static bool CanTransition(string from, string to) =>
        Graph.TryGetValue(from, out var next) && Array.IndexOf(next, to) >= 0;

    public static bool RequiresClearance(string status) => status == Released;

    public static void EnsureTransition(string from, string to)
    {
        if (!IsKnown(from))
        {
            throw new ReleaseWorkflowException($"Unknown current resignation status '{from}'.");
        }

        if (!IsKnown(to))
        {
            throw new ReleaseWorkflowException($"Unknown target resignation status '{to}'.");
        }

        if (string.Equals(from, to, StringComparison.Ordinal))
        {
            throw new ReleaseWorkflowException($"The resignation is already in status '{to}'.");
        }

        if (!CanTransition(from, to))
        {
            var allowed = NextStates(from);
            var hint = allowed.Count == 0
                ? "it is a final state and can no longer change"
                : $"allowed next: {string.Join(", ", allowed)}";
            throw new ReleaseWorkflowException(
                $"Cannot move a resignation from '{from}' to '{to}' — {hint}.");
        }
    }
}
