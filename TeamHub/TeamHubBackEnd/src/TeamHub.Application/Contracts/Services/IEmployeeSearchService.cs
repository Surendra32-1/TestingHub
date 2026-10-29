using TeamHub.Application.DTOs.Employee;

namespace TeamHub.Application.Contracts.Services;

public interface IEmployeeSearchService
{
    Task<IReadOnlyList<EmployeeSearchCardDto>> SearchActiveAsync(
        EmployeeSearchFilter filter,
        CancellationToken cancellationToken = default);
}
