using FluentValidation;

namespace Baytology.Application.Features.Properties.Commands.CreateProperty;

public class CreatePropertyCommandValidator : AbstractValidator<CreatePropertyCommand>
{
    public CreatePropertyCommandValidator()
    {
        RuleFor(x => x.AgentUserId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3).MaximumLength(500);
        RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than 0.");
        RuleFor(x => x.Price).GreaterThanOrEqualTo(1000).WithMessage("Price cannot be less than 1000.");
        RuleFor(x => x.Price).LessThanOrEqualTo(999_999_999).WithMessage("Price cannot exceed 999,999,999.");
        RuleFor(x => x.Area).GreaterThan(0).WithMessage("Area must be greater than 0.");
        RuleFor(x => x.Area).GreaterThanOrEqualTo(10).WithMessage("Area cannot be less than 10 square meters.");
        RuleFor(x => x.Area).LessThanOrEqualTo(100_000).WithMessage("Area cannot exceed 100,000 square meters.");
        RuleFor(x => x.Bedrooms).GreaterThanOrEqualTo(0).LessThanOrEqualTo(100);
        RuleFor(x => x.Bathrooms).GreaterThanOrEqualTo(0).LessThanOrEqualTo(100);
        RuleFor(x => x.Floor).GreaterThanOrEqualTo(0).LessThanOrEqualTo(999).When(x => x.Floor.HasValue);
        RuleFor(x => x.TotalFloors).GreaterThan(0).LessThanOrEqualTo(999).When(x => x.TotalFloors.HasValue);
        RuleFor(x => x)
            .Must(x => !x.Floor.HasValue || !x.TotalFloors.HasValue || x.Floor.Value <= x.TotalFloors.Value)
            .WithMessage("Floor cannot exceed total floors.");
        RuleFor(x => x.Description).MaximumLength(5000).When(x => x.Description is not null);
        RuleFor(x => x.AddressLine).MaximumLength(500).When(x => x.AddressLine is not null);
        RuleFor(x => x.City).MaximumLength(100).When(x => x.City is not null);
        RuleFor(x => x.District).MaximumLength(100).When(x => x.District is not null);
        RuleFor(x => x.ZipCode).MaximumLength(20).When(x => x.ZipCode is not null);
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
        RuleFor(x => x.ImageUrls)
            .NotEmpty().WithMessage("At least one property image is required.")
            .Must(urls => urls != null && urls.All(u => !string.IsNullOrWhiteSpace(u) && u.Length <= 1000))
            .WithMessage("Image URLs cannot be empty or exceed 1000 characters.");
    }
}
