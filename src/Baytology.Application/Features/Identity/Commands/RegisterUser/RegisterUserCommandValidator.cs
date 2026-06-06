using FluentValidation;

namespace Baytology.Application.Features.Identity.Commands.RegisterUser;

public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(254);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8)
            .Must(password => password.Any(char.IsUpper))
            .WithMessage("Password must contain at least one uppercase letter.")
            .Must(password => password.Any(char.IsLower))
            .WithMessage("Password must contain at least one lowercase letter.")
            .Must(password => password.Any(char.IsDigit))
            .WithMessage("Password must contain at least one digit.")
            .Must(password => password.Any(char.IsSymbol) || password.Any(char.IsPunctuation))
            .WithMessage("Password must contain at least one special character.");
        RuleFor(x => x.DisplayName).NotEmpty().MinimumLength(2).MaximumLength(200);
        RuleFor(x => x.Role).Must(role => role is "Buyer" or "Agent")
            .WithMessage("Role must be Buyer or Agent.");
    }
}
