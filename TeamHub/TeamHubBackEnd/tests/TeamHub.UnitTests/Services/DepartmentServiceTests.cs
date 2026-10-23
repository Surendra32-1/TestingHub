using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.Department;
using TeamHub.Application.Services;
using TeamHub.Domain.Entities;

namespace TeamHub.UnitTests.Services;

public class DepartmentServiceTests
{
    [Fact]
    public async Task CreateAsync_MapsRequestAndReturnsCreatedDepartment()
    {
        var repository = new FakeDepartmentRepository();
        var service = new DepartmentService(repository);

        var result = await service.CreateAsync(new CreateOrUpdateDepartmentRequest
        {
            DepartmentName = "Engineering",
            IsActive = true
        });

        Assert.Equal(42, result.DepartmentId);
        Assert.Equal("Engineering", result.DepartmentName);
        Assert.True(result.IsActive);
        Assert.Equal("Engineering", repository.CreatedDepartment?.DepartmentName);
        Assert.Equal("System", repository.CreatedDepartment?.CreatedBy);
    }

    private sealed class FakeDepartmentRepository : IDepartmentRepository
    {
        public Department? CreatedDepartment { get; private set; }

        public Task<IReadOnlyList<GetDepartmentDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<GetDepartmentDto>>(Array.Empty<GetDepartmentDto>());

        public Task<GetDepartmentDto?> GetByIdAsync(int departmentId, CancellationToken cancellationToken = default) =>
            Task.FromResult<GetDepartmentDto?>(null);

        public Task<int> CreateAsync(Department department, CancellationToken cancellationToken = default)
        {
            CreatedDepartment = department;
            return Task.FromResult(42);
        }

        public Task<bool> UpdateAsync(Department department, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);

        public Task<bool> DeleteAsync(int departmentId, CancellationToken cancellationToken = default) =>
            Task.FromResult(true);
    }
}