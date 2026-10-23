using FluentValidation;
using TeamHub.Application.DTOs.JobType;

namespace TeamHub.Application.Validators.JobType;

public sealed class CreateOrUpdateJobTypeRequestValidator : AbstractValidator<CreateOrUpdateJobTypeRequest>
{
    public CreateOrUpdateJobTypeRequestValidator()
    {
        RuleFor(request => request.JobTypeName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Job type name is required.")
            .MaximumLength(100).WithMessage("Job type name must be 100 characters or fewer.");
    }
}