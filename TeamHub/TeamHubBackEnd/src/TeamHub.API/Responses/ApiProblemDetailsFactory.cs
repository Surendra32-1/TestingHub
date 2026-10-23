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

    public static void AddResponseFields(ProblemDetails problem, string? message = null)
    {
        var responseMessage = message ?? problem.Detail ?? problem.Title ?? "An error occurred.";

        problem.Extensions["success"] = false;
        problem.Extensions["message"] = responseMessage;
        problem.Extensions["data"] = null;
        problem.Extensions["error"] = responseMessage;
        problem.Extensions.TryAdd("errors", new Dictionary<string, string[]>());
    }
}