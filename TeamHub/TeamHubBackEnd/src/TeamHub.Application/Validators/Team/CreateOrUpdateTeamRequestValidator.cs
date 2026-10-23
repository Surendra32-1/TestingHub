using FluentValidation;
using TeamHub.Application.DTOs.Team;

namespace TeamHub.Application.Validators.Team;

public sealed class CreateOrUpdateTeamRequestValidator : AbstractValidator<CreateOrUpdateTeamRequest>
{
    public CreateOrUpdateTeamRequestValidator()
    {
        RuleFor(request => request.TeamName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Team name is required.")
            .MaximumLength(100).WithMessage("Team name must be 100 characters or fewer.");
    }
}