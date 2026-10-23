using MediatR;
using TeamHub.Application.Contracts.Repositories;
using TeamHub.Application.DTOs.Employee;

namespace TeamHub.Application.Features.Employees;

public sealed class GetEmployeeHierarchyQueryHandler(IEmployeeRepository repository)
    : IRequestHandler<GetEmployeeHierarchyQuery, EmployeeHierarchyResponse?>
{
    public Task<EmployeeHierarchyResponse?> Handle(
        GetEmployeeHierarchyQuery request,
        CancellationToken cancellationToken) =>
        repository.GetHierarchyAsync(request.EmployeeId, cancellationToken);
}
