using FluentValidation;
using TeamHub.Application.DTOs.Employee;

namespace TeamHub.Application.Validators.Employee;

public sealed class EmployeeTrainingDtoValidator : AbstractValidator<EmployeeTrainingDto>
{
    public EmployeeTrainingDtoValidator()
    {
        RuleFor(training => training.TrainingTitle)
            .Cascade(CascadeMode.Stop)
            .Must(value => !string.IsNullOrWhiteSpace(value)).WithMessage("Training title is required.")
            .MaximumLength(200).WithMessage("Training title must be 200 characters or fewer.");

        RuleFor(training => training.TrainingProvider)
            .MaximumLength(200).WithMessage("Training provider must be 200 characters or fewer.");

        RuleFor(training => training.TrainingType)
            .MaximumLength(100).WithMessage("Training type must be 100 characters or fewer.");

        RuleFor(training => training.TrainingLocation)
            .MaximumLength(200).WithMessage("Training location must be 200 characters or fewer.");

        RuleFor(training => training.Duration)
            .MaximumLength(100).WithMessage("Duration must be 100 characters or fewer.");

        RuleFor(training => training.CertificateNo)
            .MaximumLength(100).WithMessage("Certificate number must be 100 characters or fewer.");

        RuleFor(training => training.JoinedDateBS)
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("B.S. joined date must use YYYY-MM-DD format.")
            .When(training => !string.IsNullOrWhiteSpace(training.JoinedDateBS));

        RuleFor(training => training.PassoutDateBS)
            .Matches(@"^\d{4}-\d{2}-\d{2}$").WithMessage("B.S. passout date must use YYYY-MM-DD format.")
            .When(training => !string.IsNullOrWhiteSpace(training.PassoutDateBS));

        RuleFor(training => training.PassoutDate)
            .GreaterThanOrEqualTo(training => training.JoinedDate)
            .WithMessage("Passout date cannot be before joined date.")
            .When(training => training.JoinedDate.HasValue && training.PassoutDate.HasValue);
    }
}
