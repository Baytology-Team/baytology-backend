using FluentValidation;

namespace Baytology.Application.Features.Properties.Commands.SetPropertyImageAsPrimary;

public class SetPropertyImageAsPrimaryCommandValidator : AbstractValidator<SetPropertyImageAsPrimaryCommand>
{
    public SetPropertyImageAsPrimaryCommandValidator()
    {
        RuleFor(x => x.PropertyId)
            .NotEmpty().WithMessage("Property ID is required.");

        RuleFor(x => x.ImageId)
            .NotEmpty().WithMessage("Image ID is required.");

        RuleFor(x => x.AgentUserId)
            .NotEmpty().WithMessage("Agent user ID is required.");
    }
}
