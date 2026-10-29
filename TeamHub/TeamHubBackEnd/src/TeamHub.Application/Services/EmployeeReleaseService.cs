using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.EmployeeRelease;
using TeamHub.Application.Workflows;
using TeamHub.Shared.Responses;

namespace TeamHub.Application.Services;

public sealed class EmployeeReleaseService(IEmployeeReleaseRepository repository) : IEmployeeReleaseService
{
    private const string ActorName = "System";

    public Task<IReadOnlyList<EmployeeReleaseResponse>> GetAllAsync(CancellationToken cancellationToken) =>
        repository.GetAllAsync(cancellationToken);

    public Task<PagedResult<EmployeeReleaseResponse>> GetPagedAsync(
        int page, int pageSize, string? search, string? department, string? status, string? releaseType, CancellationToken cancellationToken) =>
        repository.GetPagedAsync(
            Math.Max(page, 1),
            Math.Clamp(pageSize, 1, 100),
            string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            string.IsNullOrWhiteSpace(department) ? null : department.Trim(),
            string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
            string.IsNullOrWhiteSpace(releaseType) ? null : releaseType.Trim(),
            cancellationToken);
    public Task<IReadOnlyList<EmployeeReleaseResponse>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken) =>
        repository.GetByEmployeeIdAsync(employeeId, cancellationToken);

    public Task<EmployeeReleaseResponse?> GetByIdAsync(int releasedId, CancellationToken cancellationToken) =>
        repository.GetByIdAsync(releasedId, cancellationToken);

    public Task<EmployeeReleaseResponse?> CreateAsync(
        int employeeId,
        EmployeeReleaseRequest request,
        CancellationToken cancellationToken)
    {
        if (!ReleaseStatusWorkflow.AllowedInitial.Contains(request.Status))
        {
            throw new ReleaseWorkflowException(
                $"A new resignation can only be saved as '{ReleaseStatusWorkflow.Draft}' or " +
                $"'{ReleaseStatusWorkflow.Submitted}'. Use the status workflow to move it further.");
        }

        return repository.CreateAsync(employeeId, request, ActorName, cancellationToken);
    }

    public Task<bool> UpdateAsync(
        int releasedId,
        EmployeeReleaseRequest request,
        CancellationToken cancellationToken) =>
        repository.UpdateAsync(releasedId, request, ActorName, cancellationToken);

    public async Task<EmployeeReleaseResponse?> TransitionStatusAsync(
        int releasedId,
        UpdateReleaseStatusRequest request,
        CancellationToken cancellationToken)
    {
        var newStatus = request.Status;
        var current = await repository.GetByIdAsync(releasedId, cancellationToken);
        if (current is null)
        {
            return null;
        }

        ReleaseStatusWorkflow.EnsureTransition(current.Status, newStatus);

        if (ReleaseStatusWorkflow.RequiresClearance(newStatus) && !IsFullyCleared(current))
        {
            throw new ReleaseWorkflowException(
                "Cannot release: knowledge transfer, asset return, exit interview, relieving letter " +
                "and experience letter must all be completed first.");
        }

        return await repository.TransitionStatusAsync(releasedId, request, ActorName, cancellationToken);
    }

    public Task<EmployeeReleaseResponse?> UpdateClearanceAsync(
        int releasedId,
        UpdateReleaseClearanceRequest request,
        CancellationToken cancellationToken) =>
        repository.UpdateClearanceAsync(releasedId, request, ActorName, cancellationToken);

    private static bool IsFullyCleared(EmployeeReleaseResponse record) =>
        record.KnowledgeTransferCompleted == true &&
        record.AssetsReturned == true &&
        record.ExitInterviewCompleted == true &&
        record.RelievingLetterIssued == true &&
        record.ExperienceLetterIssued == true;
}
