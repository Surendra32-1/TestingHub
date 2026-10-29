using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.Employee;
using TeamHub.Application.Services;

namespace TeamHub.UnitTests.Services;

public sealed class EmployeeProfileServiceTests
{
    [Fact]
    public async Task UpdateAsync_normalizes_email_and_preserves_omitted_fields()
    {
        var repository = new EmployeeProfileRepositoryStub();
        var service = new EmployeeProfileService(repository);

        var result = await service.UpdateAsync(
            17,
            new EmployeeProfilePatch(
                true, "  User.Name@Example.COM  ",
                false, 0,
                false, null,
                false, null,
                false, null,
                false, null,
                false, 0,
                false, 0,
                false, null,
                false, [],
                false, []));

        Assert.Empty(result.Errors);
        Assert.Equal("user.name@example.com", repository.UpdatedPatch?.AlternateEmail);
        Assert.False(repository.UpdatedPatch?.MobileNumberSpecified);
    }

    [Theory]
    [InlineData("not-an-email", null, 0)]
    [InlineData(null, "123", 0)]
    [InlineData(null, null, 91)]
    public async Task UpdateAsync_rejects_invalid_profile_values(
        string? email,
        string? mobileNumber,
        decimal latitude)
    {
        var repository = new EmployeeProfileRepositoryStub();
        var service = new EmployeeProfileService(repository);
        var patch = new EmployeeProfilePatch(
            email is not null, email,
            false, 0,
            mobileNumber is not null, mobileNumber,
            false, null,
            false, null,
            false, null,
            latitude != 0, latitude,
            false, 0,
            false, null,
            false, [],
            false, []);

        var result = await service.UpdateAsync(17, patch);

        Assert.NotEmpty(result.Errors);
        Assert.Null(repository.UpdatedPatch);
    }

    [Fact]
    public async Task UpdateAsync_rejects_unsupported_profile_images()
    {
        var repository = new EmployeeProfileRepositoryStub();
        var service = new EmployeeProfileService(repository);
        var patch = new EmployeeProfilePatch(
            false, null,
            false, 0,
            false, null,
            false, null,
            false, null,
            false, null,
            false, 0,
            false, 0,
            true, "data:image/svg+xml;base64,PHN2Zz4=",
            false, [],
            false, []);

        var result = await service.UpdateAsync(17, patch);

        Assert.Contains("imageBase64", result.Errors.Keys);
        Assert.Null(repository.UpdatedPatch);
    }

    [Fact]
    public async Task UpdateAsync_rejects_skill_levels_outside_the_supported_range()
    {
        var repository = new EmployeeProfileRepositoryStub();
        var service = new EmployeeProfileService(repository);
        var patch = new EmployeeProfilePatch(
            false, null,
            false, 0,
            false, null,
            false, null,
            false, null,
            false, null,
            false, 0,
            false, 0,
            false, null,
            true, [new EmployeeProfileSkillUpdateDto(1, 6)],
            false, []);

        var result = await service.UpdateAsync(17, patch);

        Assert.Contains("skills", result.Errors.Keys);
        Assert.Null(repository.UpdatedPatch);
    }

    [Fact]
    public async Task UpdateAsync_persists_skill_and_dress_selection_changes()
    {
        var repository = new EmployeeProfileRepositoryStub();
        var service = new EmployeeProfileService(repository);
        var skills = new[] { new EmployeeProfileSkillUpdateDto(3, 4) };
        var dressIds = new[] { 5, 6 };
        var patch = new EmployeeProfilePatch(
            false, null,
            false, 0,
            false, null,
            false, null,
            false, null,
            false, null,
            false, 0,
            false, 0,
            false, null,
            true, skills,
            true, dressIds);

        var result = await service.UpdateAsync(17, patch);

        Assert.Empty(result.Errors);
        Assert.Equal(skills, repository.UpdatedPatch?.Skills);
        Assert.Equal(dressIds, repository.UpdatedPatch?.DressIds);
    }

    private sealed class EmployeeProfileRepositoryStub : IEmployeeProfileRepository
    {
        public EmployeeProfilePatch? UpdatedPatch { get; private set; }

        public Task<EmployeeProfileDto?> GetByEmployeeIdAsync(
            int employeeId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<EmployeeProfileDto?>(new EmployeeProfileDto(
                "test.user",
                new GetEmployeeDto { EmployeeId = employeeId, ENumber = 17, FirstName = "Test", LastName = "Employee" },
                [],
                [],
                [],
                []));

        public Task<EmployeeProfileSummaryDto?> GetSummaryAsync(
            int employeeId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<EmployeeProfileSummaryDto?>(new EmployeeProfileSummaryDto("Test Employee", null));

        public Task<bool> IsActiveBloodGroupAsync(
            int bloodGroupId,
            CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> AreSelectableSkillsAsync(
            int employeeId,
            IReadOnlyCollection<int> skillIds,
            CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task<bool> AreSelectableDressSizesAsync(
            int employeeId,
            IReadOnlyCollection<int> dressIds,
            CancellationToken cancellationToken = default) => Task.FromResult(true);

        public Task AddImageAsync(
            int employeeId,
            string imagePath,
            CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> UpdateAsync(
            int employeeId,
            EmployeeProfilePatch patch,
            CancellationToken cancellationToken = default)
        {
            UpdatedPatch = patch;
            return Task.FromResult(true);
        }
    }
}
