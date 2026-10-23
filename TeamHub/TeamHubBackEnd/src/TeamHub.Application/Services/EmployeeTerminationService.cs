using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.EmployeeTermination;
using TeamHub.Application.Workflows;
using TeamHub.Shared.Responses;

namespace TeamHub.Application.Services;

public sealed class EmployeeTerminationService(IEmployeeTerminationRepository repository) : IEmployeeTerminationService
{
    private const string ActorName = "System";

    public Task<IReadOnlyList<EmployeeTerminationResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);

    public Task<PagedResult<EmployeeTerminationResponse>> GetPagedAsync(
        int page, int pageSize, string? search, string? department, string? status, CancellationToken cancellationToken) =>
        repository.GetPagedAsync(
            Math.Max(page, 1),
            Math.Clamp(pageSize, 1, 100),
            string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            string.IsNullOrWhiteSpace(department) ? null : department.Trim(),
            string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
            cancellationToken);
    public Task<IReadOnlyList<EmployeeTerminationResponse>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken) =>
        repository.GetByEmployeeIdAsync(employeeId, cancellationToken);

    public Task<EmployeeTerminationResponse?> GetByIdAsync(int terminationId, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(terminationId, cancellationToken);

    public Task<EmployeeTerminationResponse?> CreateAsync(int employeeId, EmployeeTerminationRequest request, CancellationToken cancellationToken)
    {
        if (!TerminationStatusWorkflow.AllowedInitial.Contains(request.Status))
        {
            throw new ReleaseWorkflowException(
                $"A new termination can only be saved as '{TerminationStatusWorkflow.Draft}' or " +
                $"'{TerminationStatusWorkflow.Submitted}'. Use the status workflow to move it further.");
        }

        return repository.CreateAsync(employeeId, request, ActorName, cancellationToken);
    }

    public Task<bool> UpdateAsync(int terminationId, EmployeeTerminationRequest request, CancellationToken cancellationToken) =>
        repository.UpdateAsync(terminationId, request, ActorName, cancellationToken);

    public async Task<EmployeeTerminationResponse?> TransitionStatusAsync(
        int terminationId,
        string newStatus,
        CancellationToken cancellationToken)
    {
        var current = await repository.GetByIdAsync(terminationId, cancellationToken);
        if (current is null)
        {
            return null;
        }

        TerminationStatusWorkflow.EnsureTransition(current.Status, newStatus);

        if (TerminationStatusWorkflow.RequiresClearance(newStatus) && !IsFullyCleared(current))
        {
            throw new ReleaseWorkflowException(
                "Cannot terminate: asset recovery, system access revocation, exit interview " +
                "and termination letter must all be completed first.");
        }

        return await repository.TransitionStatusAsync(terminationId, newStatus, ActorName, cancellationToken);
    }

    public Task<EmployeeTerminationResponse?> UpdateClearanceAsync(
        int terminationId,
        UpdateTerminationClearanceRequest request,
        CancellationToken cancellationToken) =>
        repository.UpdateClearanceAsync(terminationId, request, ActorName, cancellationToken);

    private static bool IsFullyCleared(EmployeeTerminationResponse record) =>
        record.AssetRecovered == true &&
        record.SystemAccessRevoked == true &&
        record.ExitInterviewCompleted == true &&
        record.TerminationLetterIssued == true;
}
