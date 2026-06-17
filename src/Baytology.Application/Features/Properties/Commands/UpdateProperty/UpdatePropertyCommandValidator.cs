using FluentValidation;
using System.Text.RegularExpressions;

namespace Baytology.Application.Features.Properties.Commands.UpdateProperty;

public class UpdatePropertyCommandValidator : AbstractValidator<UpdatePropertyCommand>
{
    private static readonly HashSet<string> EgyptianCities = new(StringComparer.OrdinalIgnoreCase)
    {
        "Cairo", "القاهرة",
        "Giza", "الجيزة",
        "Alexandria", "الإسكندرية",
        "Dakahlia", "الدقهلية",
        "Red Sea", "البحر الأحمر",
        "Beheira", "البحيرة",
        "Faiyum", "الفيوم",
        "Gharbia", "الغربية",
        "Ismailia", "الإسماعيلية",
        "Menofia", "المنوفية",
        "Minya", "المنيا",
        "Qaliubiya", "القليوبية",
        "New Valley", "الوادي الجديد",
        "Suez", "السويس",
        "Aswan", "أسوان",
        "Asyut", "أسيوط",
        "Beni Suef", "بني سويف",
        "Port Said", "بورسعيد",
        "Damietta", "دمياط",
        "Sharkia", "الشرقية",
        "Sohag", "سوهاج",
        "South Sinai", "جنوب سيناء",
        "Kafr El Sheikh", "كفر الشيخ",
        "Matrouh", "مطروح",
        "Luxor", "الأقصر",
        "Qena", "قنا",
        "North Sinai", "شمال سيناء"
    };

    public UpdatePropertyCommandValidator()
    {
        RuleFor(x => x.PropertyId).NotEmpty();
        RuleFor(x => x.AgentUserId).NotEmpty();
        RuleFor(x => x.Title).NotEmpty().MinimumLength(3).MaximumLength(500)
            .Must(title => !ContainsHtmlTags(title))
            .WithMessage("Title cannot contain HTML tags.");
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
        
        // Validate Egyptian cities
        RuleFor(x => x.City)
            .Must(city => string.IsNullOrWhiteSpace(city) || EgyptianCities.Contains(city.Trim()))
            .WithMessage("City must be a valid Egyptian city.")
            .When(x => x.City is not null);
        
        RuleFor(x => x.District).MaximumLength(100).When(x => x.District is not null);
        
        // Validate district name format
        RuleFor(x => x.District)
            .Must(district => string.IsNullOrWhiteSpace(district) || IsValidDistrictName(district))
            .WithMessage("District name is invalid. Only letters, numbers, spaces, hyphens, and dots are allowed.")
            .When(x => x.District is not null);
        
        // Validate Egyptian zip code format (5 digits) - optional
        RuleFor(x => x.ZipCode)
            .Must(zip => string.IsNullOrWhiteSpace(zip) || (zip.Trim().Length == 5 && Regex.IsMatch(zip.Trim(), @"^\d{5}$")))
            .WithMessage("Zip code must be a valid Egyptian postal code (5 digits).")
            .When(x => !string.IsNullOrWhiteSpace(x.ZipCode));
        
        // At least city or district must be provided
        RuleFor(x => x)
            .Must(x => !string.IsNullOrWhiteSpace(x.City) || !string.IsNullOrWhiteSpace(x.District))
            .WithMessage("At least city or district must be provided.");
        
        RuleFor(x => x.Latitude).InclusiveBetween(-90, 90).When(x => x.Latitude.HasValue);
        RuleFor(x => x.Longitude).InclusiveBetween(-180, 180).When(x => x.Longitude.HasValue);
        
        // Validate Egypt-specific coordinates
        RuleFor(x => x.Latitude)
            .Must(lat => !lat.HasValue || (lat.Value >= 22 && lat.Value <= 32))
            .WithMessage("Latitude must be within Egypt's boundaries (22° to 32°).")
            .When(x => x.Latitude.HasValue);
        
        RuleFor(x => x.Longitude)
            .Must(lng => !lng.HasValue || (lng.Value >= 24 && lng.Value <= 37))
            .WithMessage("Longitude must be within Egypt's boundaries (24° to 37°).")
            .When(x => x.Longitude.HasValue);
    }

    private static bool IsValidDistrictName(string district)
    {
        var trimmed = district.Trim();
        if (trimmed.Length < 2 || trimmed.Length > 100)
            return false;
        return Regex.IsMatch(trimmed, @"^[\p{L}\p{N}\s\-\.]+$");
    }

    private static bool ContainsHtmlTags(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;
        
        return Regex.IsMatch(text, @"<[^>]+>");
    }
}
