using FluentValidation;

namespace Baytology.Application.Features.Identity.Queries.GenerateTokens;

public sealed class GenerateTokenQueryValidator : AbstractValidator<GenerateTokenQuery>
{
    public GenerateTokenQueryValidator()
    {
        RuleFor(request => request.Email)
            .NotNull().NotEmpty().EmailAddress().MaximumLength(254)
            .WithErrorCode("Email_Invalid")
            .WithMessage("Email must be a valid email address");

        RuleFor(request => request.Password)
            .NotNull().NotEmpty()
            .WithErrorCode("Password_Null_Or_Empty")
            .WithMessage("Password cannot be null or empty.");
    }
}