using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Enums;
using FluentValidation;

namespace Baytology.Application.Features.PropertyPreferences.Commands.SetPropertyPreference;

public class SetPropertyPreferenceCommandValidator : AbstractValidator<SetPropertyPreferenceCommand>
{
    public SetPropertyPreferenceCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("User ID is required.");

        RuleFor(x => x.MinPrice)
            .GreaterThanOrEqualTo(0).When(x => x.MinPrice.HasValue)
            .WithMessage("Minimum price cannot be negative.");

        RuleFor(x => x.MaxPrice)
            .GreaterThanOrEqualTo(0).When(x => x.MaxPrice.HasValue)
            .WithMessage("Maximum price cannot be negative.");

        RuleFor(x => x)
            .Must(x => !x.MinPrice.HasValue || !x.MaxPrice.HasValue || x.MinPrice <= x.MaxPrice)
            .WithMessage("Minimum price cannot be greater than maximum price.");

        RuleFor(x => x.MinArea)
            .GreaterThanOrEqualTo(0).When(x => x.MinArea.HasValue)
            .WithMessage("Minimum area cannot be negative.");

        RuleFor(x => x.MaxArea)
            .GreaterThanOrEqualTo(0).When(x => x.MaxArea.HasValue)
            .WithMessage("Maximum area cannot be negative.");

        RuleFor(x => x)
            .Must(x => !x.MinArea.HasValue || !x.MaxArea.HasValue || x.MinArea <= x.MaxArea)
            .WithMessage("Minimum area cannot be greater than maximum area.");

        RuleFor(x => x.MinBedrooms)
            .GreaterThanOrEqualTo(0).When(x => x.MinBedrooms.HasValue)
            .WithMessage("Minimum bedrooms cannot be negative.");

        RuleFor(x => x.MaxBedrooms)
            .GreaterThanOrEqualTo(0).When(x => x.MaxBedrooms.HasValue)
            .WithMessage("Maximum bedrooms cannot be negative.");

        RuleFor(x => x)
            .Must(x => !x.MinBedrooms.HasValue || !x.MaxBedrooms.HasValue || x.MinBedrooms <= x.MaxBedrooms)
            .WithMessage("Minimum bedrooms cannot be greater than maximum bedrooms.");

        RuleFor(x => x.MinBathrooms)
            .GreaterThanOrEqualTo(0).When(x => x.MinBathrooms.HasValue)
            .WithMessage("Minimum bathrooms cannot be negative.");

        RuleFor(x => x.MaxBathrooms)
            .GreaterThanOrEqualTo(0).When(x => x.MaxBathrooms.HasValue)
            .WithMessage("Maximum bathrooms cannot be negative.");

        RuleFor(x => x)
            .Must(x => !x.MinBathrooms.HasValue || !x.MaxBathrooms.HasValue || x.MinBathrooms <= x.MaxBathrooms)
            .WithMessage("Minimum bathrooms cannot be greater than maximum bathrooms.");

        RuleFor(x => x.City)
            .MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.City))
            .WithMessage("City cannot exceed 100 characters.");

        RuleFor(x => x.District)
            .MaximumLength(100).When(x => !string.IsNullOrWhiteSpace(x.District))
            .WithMessage("District cannot exceed 100 characters.");
    }
}
