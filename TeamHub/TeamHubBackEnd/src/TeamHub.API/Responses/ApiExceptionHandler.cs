using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using TeamHub.Application.Common;
using TeamHub.Application.Workflows;

namespace TeamHub.API.Responses;

public sealed class ApiExceptionHandler(ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (httpContext.RequestAborted.IsCancellationRequested)
        {
            return false;
        }

        var (status, title, detail) = exception switch
        {
            SqlException { Number: 2601 or 2627 } => (
                StatusCodes.Status409Conflict,
                "Duplicate value",
                "A record with this value already exists. Please use a unique value."),
            SqlException { Number: 547 } => (
                StatusCodes.Status409Conflict,
                "Related record conflict",
                "This record is referenced by other records and cannot be changed or deleted."),
            ReleaseWorkflowException workflowEx => (
                StatusCodes.Status409Conflict,
                "Invalid status transition",
                workflowEx.Message),
            RbacValidationException rbacValidationEx => (
                StatusCodes.Status400BadRequest,
                "Invalid assignment",
                rbacValidationEx.Message),
            PrivilegeEscalationException privilegeEx => (
                StatusCodes.Status403Forbidden,
                "Insufficient privileges",
                privilegeEx.Message),
            _ => (
                StatusCodes.Status500InternalServerError,
                "Request failed",
                string.IsNullOrWhiteSpace(exception.Message)
                    ? "An unexpected error occurred while processing your request."
                    : exception.Message)
        };

        logger.LogError(exception, "API request failed with status {StatusCode}.", status);

        var problem = new ProblemDetails
        {
            Type = $"https://httpstatuses.com/{status}",
            Title = title,
            Status = status,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        ApiProblemDetailsFactory.AddResponseFields(problem, detail);

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);
        return true;
    }
}