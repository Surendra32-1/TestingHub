using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.Contracts.Services;
using TeamHub.Application.DTOs.Employee;

namespace TeamHub.Application.Services;

public sealed class EmployeeSearchService(IEmployeeSearchRepository repository) : IEmployeeSearchService
{
    public Task<IReadOnlyList<EmployeeSearchCardDto>> SearchActiveAsync(
        EmployeeSearchFilter filter,
        CancellationToken cancellationToken = default) =>
        repository.SearchActiveAsync(filter, cancellationToken);
}
