using FluentValidation;

namespace Baytology.Application.Features.Identity.Commands.ResendConfirmation;

public class ResendConfirmationCommandValidator : AbstractValidator<ResendConfirmationCommand>
{
    public ResendConfirmationCommandValidator()
    {
        RuleFor(v => v.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.")
            .MaximumLength(254).WithMessage("Email cannot exceed 254 characters.");
    }
}
