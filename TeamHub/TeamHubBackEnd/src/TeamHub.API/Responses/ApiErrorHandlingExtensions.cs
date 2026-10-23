using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace TeamHub.API.Responses;

public static class ApiErrorHandlingExtensions
{
    public static IServiceCollection AddApiErrorHandling(this IServiceCollection services)
    {
        services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                options.InvalidModelStateResponseFactory = context =>
                {
                    var errors = context.ModelState
                        .Where(entry => entry.Value?.Errors.Count > 0)
                        .ToDictionary(
                            entry => entry.Key,
                            entry => entry.Value!.Errors
                                .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                                    ? "The supplied value is invalid."
                                    : error.ErrorMessage)
                                .ToArray());

                    var problem = new ProblemDetails
                    {
                        Type = "https://httpstatuses.com/400",
                        Title = "Validation failed",
                        Status = StatusCodes.Status400BadRequest,
                        Detail = "One or more validation errors occurred.",
                        Instance = context.HttpContext.Request.Path
                    };

                    var validationMessage = errors
                        .SelectMany(entry => entry.Value)
                        .Distinct()
                        .ToArray();
                    ApiProblemDetailsFactory.AddResponseFields(
                        problem,
                        validationMessage.Length == 0
                            ? "One or more validation errors occurred."
                            : string.Join(" ", validationMessage));
                    problem.Extensions["errors"] = errors;

                    return new BadRequestObjectResult(problem);
                };
            });

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
                ApiProblemDetailsFactory.AddResponseFields(context.ProblemDetails);
        });

        return services;
    }
}