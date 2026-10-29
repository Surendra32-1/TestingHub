using System.ComponentModel.DataAnnotations;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.EmployeeRelease;
using TeamHub.Application.DTOs.EmployeeTermination;
using TeamHub.Application.Services;
using TeamHub.Shared.Responses;

namespace TeamHub.UnitTests.Validation;

public class EmployeeLifecycleRequestValidationTests
{
    [Fact]
    public void ReleaseRequiresResignationDateLastWorkingDateAndNoticePeriod()
    {
        var request = new EmployeeReleaseRequest
        {
            ReleaseType = "Resignation",
            Status = "Draft"
        };

        var results = Validate(request);

        Assert.Contains(results, result => result.ErrorMessage == "Resignation Date is required.");
        Assert.Contains(results, result => result.ErrorMessage == "Last Working Day is required.");
        Assert.Contains(results, result =>
            result.ErrorMessage == "Notice Period Days is required so early release can be detected.");
    }

    [Fact]
    public void ReleaseRejectsLastWorkingDateBeforeResignationDate()
    {
        var request = new EmployeeReleaseRequest
        {
            ReleaseType = "Resignation",
            ResignationDate = new DateOnly(2026, 10, 10),
            LastWorkingDate = new DateOnly(2026, 10, 9),
            NoticePeriodDays = 0,
            Reason = "Employee requested release",
            Status = "Draft"
        };

        var results = Validate(request);

        Assert.Contains(results, result =>
            result.ErrorMessage == "Last Working Day must be greater than or equal to Resignation Date.");
    }

    [Fact]
    public void ReleaseReasonMustHaveAtLeastTenNonWhitespaceCharacters()
    {
        var request = new EmployeeReleaseRequest
        {
            ReleaseType = "Resignation",
            ResignationDate = new DateOnly(2026, 10, 10),
            LastWorkingDate = new DateOnly(2026, 10, 10),
            NoticePeriodDays = 0,
            Reason = "     short     ",
            Status = "Draft"
        };

        var results = Validate(request);

        Assert.Contains(results, result =>
            result.ErrorMessage == "Provide either a Reason (at least 10 characters) or an Early Release Reason.");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(181)]
    public void ReleaseNoticePeriodMustBeBetweenZeroAnd180(int noticePeriodDays)
    {
        var request = new EmployeeReleaseRequest
        {
            ReleaseType = "Resignation",
            ResignationDate = new DateOnly(2026, 10, 10),
            LastWorkingDate = new DateOnly(2026, 10, 10),
            NoticePeriodDays = noticePeriodDays,
            Reason = "Employee requested release",
            Status = "Draft"
        };

        var results = Validate(request);

        Assert.Contains(results, result => result.MemberNames.Contains(nameof(request.NoticePeriodDays)));
    }

    [Fact]
    public void TerminationDateCannotBeInTheFuture()
    {
        var request = new EmployeeTerminationRequest
        {
            TerminationType = "Performance",
            EffectiveDate = DateOnly.FromDateTime(DateTime.Today).AddDays(1),
            Status = "Draft"
        };

        var results = Validate(request);

        Assert.Contains(results, result =>
            result.ErrorMessage == "Termination Date cannot be in the future.");
    }

    [Fact]
    public async Task ReleaseServiceForwardsCreateToRepositoryWithSystemActor()
    {
        var repository = new CapturingEmployeeReleaseRepository();
        var service = new EmployeeReleaseService(repository);
        var request = new EmployeeReleaseRequest
        {
            ReleaseType = "Resignation",
            ResignationDate = new DateOnly(2026, 10, 1),
            LastWorkingDate = new DateOnly(2026, 10, 6),
            NoticePeriodDays = 5,
            Reason = "Employee requested release",
            Status = "Draft"
        };

        await service.CreateAsync(1, request, CancellationToken.None);

        Assert.Same(request, repository.CapturedRequest);
        Assert.Equal("System", repository.CapturedActorName);
    }

    private static List<ValidationResult> Validate(object request)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(request, new ValidationContext(request), results, validateAllProperties: true);
        return results;
    }

    private sealed class CapturingEmployeeReleaseRepository : IEmployeeReleaseRepository
    {
        public EmployeeReleaseRequest? CapturedRequest { get; private set; }
        public string? CapturedActorName { get; private set; }

        public Task<IReadOnlyList<EmployeeReleaseResponse>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<EmployeeReleaseResponse>>(Array.Empty<EmployeeReleaseResponse>());

        public Task<IReadOnlyList<EmployeeReleaseResponse>> GetByEmployeeIdAsync(int employeeId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<EmployeeReleaseResponse>>(Array.Empty<EmployeeReleaseResponse>());

        public Task<EmployeeReleaseResponse?> GetByIdAsync(int releasedId, CancellationToken cancellationToken) =>
            Task.FromResult<EmployeeReleaseResponse?>(null);

        public Task<EmployeeReleaseResponse?> CreateAsync(
            int employeeId, EmployeeReleaseRequest request, string actorName, CancellationToken cancellationToken)
        {
            CapturedRequest = request;
            CapturedActorName = actorName;
            return Task.FromResult<EmployeeReleaseResponse?>(null);
        }

        public Task<bool> UpdateAsync(
            int releasedId, EmployeeReleaseRequest request, string actorName, CancellationToken cancellationToken) =>
            Task.FromResult(false);

        public Task<PagedResult<EmployeeReleaseResponse>> GetPagedAsync(
            int page, int pageSize, string? search, string? department, string? status, string? releaseType, CancellationToken cancellationToken) =>
            Task.FromResult<PagedResult<EmployeeReleaseResponse>>(PagedResult<EmployeeReleaseResponse>.Create(Array.Empty<EmployeeReleaseResponse>(), page, pageSize));

        public Task<EmployeeReleaseResponse?> TransitionStatusAsync(
            int releasedId, UpdateReleaseStatusRequest request, string actorName, CancellationToken cancellationToken) =>
            Task.FromResult<EmployeeReleaseResponse?>(null);

        public Task<EmployeeReleaseResponse?> UpdateClearanceAsync(
            int releasedId, UpdateReleaseClearanceRequest request, string actorName, CancellationToken cancellationToken) =>
            Task.FromResult<EmployeeReleaseResponse?>(null);
    }
}
