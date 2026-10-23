using MediatR;
using TeamHub.Application.DTOs.Employee;

namespace TeamHub.Application.Features.Employees;

public sealed record GetEmployeeHierarchyQuery(int EmployeeId) : IRequest<EmployeeHierarchyResponse?>;
