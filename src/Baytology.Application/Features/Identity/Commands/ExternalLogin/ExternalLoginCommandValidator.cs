using FluentValidation;

namespace Baytology.Application.Features.Identity.Commands.ExternalLogin;

public class ExternalLoginCommandValidator : AbstractValidator<ExternalLoginCommand>
{
    public ExternalLoginCommandValidator()
    {
        RuleFor(v => v.Provider)
            .NotEmpty().WithMessage("Provider is required.")
            .Must(p => p.Equals("Google", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Provider must be Google.");

        RuleFor(v => v.IdToken)
            .NotEmpty().WithMessage("IdToken is required.");
    }
}
