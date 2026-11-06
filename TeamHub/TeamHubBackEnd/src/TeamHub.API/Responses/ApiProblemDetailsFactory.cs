using Microsoft.AspNetCore.Mvc;

namespace TeamHub.API.Responses;

public static class ApiProblemDetailsFactory
{
    public static ProblemDetails NotFound(string title, string detail, PathString instance)
    {
        var problem = new ProblemDetails
        {
            Type = "https://httpstatuses.com/404",
            Title = title,
            Status = StatusCodes.Status404NotFound,
            Detail = detail,
            Instance = instance
        };

        AddResponseFields(problem, detail);
        return problem;
    }

    public static ProblemDetails Forbidden(string title, string detail, PathString instance)
    {
        var problem = new ProblemDetails
        {
            Type = "https://httpstatuses.com/403",
            Title = title,
            Status = StatusCodes.Status403Forbidden,
            Detail = detail,
            Instance = instance
        };

        AddResponseFields(problem, detail);
        return problem;
    }

    public static ValidationProblemDetails Validation(
        IDictionary<string, string[]> errors,
        string title,
        PathString instance)
    {
        var problem = new ValidationProblemDetails(
            new Dictionary<string, string[]>(errors))
        {
            Title = title,
            Status = StatusCodes.Status400BadRequest,
            Instance = instance
        };

        var message = string.Join(" ", errors
            .SelectMany(entry => entry.Value)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .Distinct());

        AddResponseFields(problem, string.IsNullOrWhiteSpace(message) ? title : message);
        return problem;
    }

    public static void AddResponseFields(ProblemDetails problem, string? message = null)
    {
        var responseMessage = message ?? problem.Detail ?? problem.Title ?? "An error occurred.";

        if (string.IsNullOrWhiteSpace(problem.Detail))
        {
            problem.Detail = responseMessage;
        }

        problem.Extensions["success"] = false;
        problem.Extensions["message"] = responseMessage;
        problem.Extensions["data"] = null;
        problem.Extensions["error"] = responseMessage;

        if (problem is not ValidationProblemDetails)
        {
            problem.Extensions.TryAdd("errors", new Dictionary<string, string[]>());
        }
    }
}