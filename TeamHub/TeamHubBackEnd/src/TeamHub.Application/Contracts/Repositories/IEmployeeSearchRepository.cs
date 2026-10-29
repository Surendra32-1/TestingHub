using TeamHub.Application.DTOs.Employee;

namespace TeamHub.Application.Contracts.Repositories;

public interface IEmployeeSearchRepository
{
    Task<IReadOnlyList<EmployeeSearchCardDto>> SearchActiveAsync(
        EmployeeSearchFilter filter,
        CancellationToken cancellationToken = default);
}
