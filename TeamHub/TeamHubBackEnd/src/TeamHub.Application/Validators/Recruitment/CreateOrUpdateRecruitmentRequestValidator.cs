using FluentValidation;
using TeamHub.Application.DTOs.Recruitment;

namespace TeamHub.Application.Validators.Recruitment;

public sealed class CreateOrUpdateRecruitmentRequestValidator : AbstractValidator<CreateOrUpdateRecruitmentRequest>
{
    public CreateOrUpdateRecruitmentRequestValidator()
    {
        RuleFor(request => request.RecruitmentName)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name)).WithMessage("Recruitment name is required.")
            .MaximumLength(100).WithMessage("Recruitment name must be 100 characters or fewer.");
    }
}