using Baytology.Domain.Common;
using Baytology.Domain.Exceptions;
using Baytology.Domain.Common.Enums;
using Baytology.Domain.Common.Results;
using Baytology.Domain.DomainEvents;

namespace Baytology.Domain.Entities;

public sealed class Property : AuditableEntity
{
    public string AgentUserId { get; private set; } = null!;
    public string Title { get; private set; } = null!;
    public string? Description { get; private set; }
    public PropertyType PropertyType { get; private set; }
    public ListingType ListingType { get; private set; }
    public decimal Price { get; private set; }
    public decimal Area { get; private set; }
    public int Bedrooms { get; private set; }
    public int Bathrooms { get; private set; }
    public int? Floor { get; private set; }
    public int? TotalFloors { get; private set; }
    public string? AddressLine { get; private set; }
    public string? City { get; private set; }
    public string? District { get; private set; }
    public string? ZipCode { get; private set; }
    public string? SourceListingUrl { get; private set; }
    public decimal? Latitude { get; private set; }
    public decimal? Longitude { get; private set; }
    public PropertyStatus Status { get; private set; }
    public bool IsFeatured { get; private set; }

    // Navigation properties
    private readonly List<PropertyImage> _images = [];
    public IReadOnlyCollection<PropertyImage> Images => _images.AsReadOnly();

    public PropertyAmenity? Amenity { get; private set; }

    private Property() { }

    private Property(
        Guid id,
        string agentUserId,
        string title,
        string? description,
        PropertyType propertyType,
        ListingType listingType,
        decimal price,
        decimal area,
        int bedrooms,
        int bathrooms,
        string? city,
        string? district,
        string? sourceListingUrl) : base(id)
    {
        AgentUserId = agentUserId;
        Title = title;
        Description = description;
        PropertyType = propertyType;
        ListingType = listingType;
        Price = price;
        Area = area;
        Bedrooms = bedrooms;
        Bathrooms = bathrooms;
        City = city;
        District = district;
        SourceListingUrl = sourceListingUrl;
        Status = PropertyStatus.Available;
        IsFeatured = false;
    }

    public static Result<Property> Create(
        string agentUserId,
        string title,
        string? description,
        PropertyType propertyType,
        ListingType listingType,
        decimal price,
        decimal area,
        int bedrooms,
        int bathrooms,
        string? city = null,
        string? district = null,
        string? sourceListingUrl = null,
        int? floor = null,
        int? totalFloors = null,
        Guid? id = null)
    {
        if (string.IsNullOrWhiteSpace(agentUserId))
            return PropertyErrors.AgentRequired;

        if (string.IsNullOrWhiteSpace(title))
            return PropertyErrors.TitleRequired;

        if (title.Trim().Length < 3)
            return Error.Validation("Property_TitleTooShort", "Title must be at least 3 characters long.");

        if (title.Trim().Length > 500)
            return Error.Validation("Property_TitleTooLong", "Title cannot exceed 500 characters.");

        if (price <= 0)
            return PropertyErrors.PriceInvalid;

        if (price < 1000)
            return Error.Validation("Property_PriceTooLow", "Price cannot be less than 1000.");

        if (price > 999_999_999)
            return Error.Validation("Property_PriceTooHigh", "Price cannot exceed 999,999,999.");

        if (area <= 0)
            return PropertyErrors.AreaInvalid;

        if (area < 10)
            return Error.Validation("Property_AreaTooSmall", "Area cannot be less than 10 square meters.");

        if (area > 100_000)
            return Error.Validation("Property_AreaTooLarge", "Area cannot exceed 100,000 square meters.");

        if (bedrooms < 0)
            return PropertyErrors.BedroomsInvalid;

        if (bedrooms > 100)
            return Error.Validation("Property_BedroomsTooMany", "Bedrooms cannot exceed 100.");

        if (bathrooms < 0)
            return PropertyErrors.BathroomsInvalid;

        if (bathrooms > 100)
            return Error.Validation("Property_BathroomsTooMany", "Bathrooms cannot exceed 100.");

        if (description is not null && description.Length > 5000)
            return Error.Validation("Property_DescriptionTooLong", "Description cannot exceed 5000 characters.");

        var structureValidation = ValidateStructure(floor, totalFloors);
        if (structureValidation is not null)
            return structureValidation.Value;

        var property = new Property(
            id ?? Guid.NewGuid(), agentUserId.Trim(), title.Trim(), description?.Trim(),
            propertyType, listingType, price, area, bedrooms, bathrooms,
            city?.Trim(), district?.Trim(), sourceListingUrl?.Trim());

        property.Floor = floor;
        property.TotalFloors = totalFloors;
        property.AddDomainEvent(new PropertyCreatedEvent(property.Id));

        return property;
    }

    public Result<Success> Update(
        string title,
        string? description,
        PropertyType propertyType,
        ListingType listingType,
        decimal price,
        decimal area,
        int bedrooms,
        int bathrooms,
        int? floor,
        int? totalFloors,
        string? addressLine,
        string? city,
        string? district,
        string? zipCode,
        decimal? latitude,
        decimal? longitude,
        bool isFeatured)
    {
        if (string.IsNullOrWhiteSpace(title))
            return PropertyErrors.TitleRequired;

        if (title.Trim().Length < 3)
            return Error.Validation("Property_TitleTooShort", "Title must be at least 3 characters long.");

        if (title.Trim().Length > 500)
            return Error.Validation("Property_TitleTooLong", "Title cannot exceed 500 characters.");

        if (price <= 0)
            return PropertyErrors.PriceInvalid;

        if (price < 1000)
            return Error.Validation("Property_PriceTooLow", "Price cannot be less than 1000.");

        if (price > 999_999_999)
            return Error.Validation("Property_PriceTooHigh", "Price cannot exceed 999,999,999.");

        if (area <= 0)
            return PropertyErrors.AreaInvalid;

        if (area < 10)
            return Error.Validation("Property_AreaTooSmall", "Area cannot be less than 10 square meters.");

        if (area > 100_000)
            return Error.Validation("Property_AreaTooLarge", "Area cannot exceed 100,000 square meters.");

        if (bedrooms < 0)
            return PropertyErrors.BedroomsInvalid;

        if (bedrooms > 100)
            return Error.Validation("Property_BedroomsTooMany", "Bedrooms cannot exceed 100.");

        if (bathrooms < 0)
            return PropertyErrors.BathroomsInvalid;

        if (bathrooms > 100)
            return Error.Validation("Property_BathroomsTooMany", "Bathrooms cannot exceed 100.");

        if (description is not null && description.Length > 5000)
            return Error.Validation("Property_DescriptionTooLong", "Description cannot exceed 5000 characters.");

        var structureValidation = ValidateStructure(floor, totalFloors);
        if (structureValidation is not null)
            return structureValidation.Value;

        Title = title.Trim();
        Description = description;
        PropertyType = propertyType;
        ListingType = listingType;
        Price = price;
        Area = area;
        Bedrooms = bedrooms;
        Bathrooms = bathrooms;
        Floor = floor;
        TotalFloors = totalFloors;
        AddressLine = addressLine;
        City = city;
        District = district;
        ZipCode = zipCode;
        Latitude = latitude;
        Longitude = longitude;
        IsFeatured = isFeatured;

        return Result.Success;
    }

    public void SetSourceListingUrl(string? sourceListingUrl)
    {
        SourceListingUrl = sourceListingUrl;
    }

    public Result<Success> SetLocation(string? addressLine, string? city, string? district, string? zipCode, decimal? lat, decimal? lng)
    {
        if (addressLine is not null && addressLine.Length > 500)
            return Error.Validation("Property_AddressLineTooLong", "Address line cannot exceed 500 characters.");

        if (city is not null && city.Length > 100)
            return Error.Validation("Property_CityTooLong", "City cannot exceed 100 characters.");

        if (district is not null && district.Length > 100)
            return Error.Validation("Property_DistrictTooLong", "District cannot exceed 100 characters.");

        if (zipCode is not null && zipCode.Length > 20)
            return Error.Validation("Property_ZipCodeTooLong", "Zip code cannot exceed 20 characters.");

        if (lat.HasValue && (lat.Value < -90 || lat.Value > 90))
            return Error.Validation("Property_LatitudeOutOfRange", "Latitude must be between -90 and 90 degrees.");

        if (lng.HasValue && (lng.Value < -180 || lng.Value > 180))
            return Error.Validation("Property_LongitudeOutOfRange", "Longitude must be between -180 and 180 degrees.");

        // Validate Egyptian cities
        if (city is not null && !IsValidEgyptianCity(city))
            return PropertyErrors.CityInvalid;

        // Validate district name (if provided)
        if (district is not null && !IsValidDistrictName(district))
            return PropertyErrors.DistrictInvalid;

        // Validate Egyptian zip code format (5 digits)
        if (zipCode is not null && !IsValidEgyptianZipCode(zipCode))
            return PropertyErrors.ZipCodeInvalid;

        // At least city or district must be provided
        if (string.IsNullOrWhiteSpace(city) && string.IsNullOrWhiteSpace(district))
            return PropertyErrors.LocationRequired;

        AddressLine = addressLine;
        City = city;
        District = district;
        ZipCode = zipCode;
        Latitude = lat;
        Longitude = lng;

        return Result.Success;
    }

    private static bool IsValidEgyptianCity(string city)
    {
        var validCities = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
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

        return validCities.Contains(city.Trim());
    }

    private static bool IsValidDistrictName(string district)
    {
        // District name should be at least 2 characters and contain only Arabic or Latin letters, numbers, and spaces
        var trimmed = district.Trim();
        if (trimmed.Length < 2 || trimmed.Length > 100)
            return false;

        // Allow Arabic and Latin letters, numbers, spaces, and common punctuation
        return System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^[\p{L}\p{N}\s\-\.]+$");
    }

    private static bool IsValidEgyptianZipCode(string zipCode)
    {
        // Egyptian zip codes are 5 digits
        var trimmed = zipCode.Trim();
        return trimmed.Length == 5 && System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d{5}$");
    }

    public void ChangeStatus(PropertyStatus status) => Status = status;

    public void MarkUnavailableForConfirmedBooking()
    {
        Status = ListingType == ListingType.Rent
            ? PropertyStatus.Rented
            : PropertyStatus.Sold;
    }

    public void AddImage(PropertyImage image) => _images.Add(image);

    public Result<Success> SetPrimaryImage(Guid imageId)
    {
        var image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image is null)
            return PropertyErrors.ImageNotFound;

        // Set all images to non-primary first
        foreach (var img in _images)
        {
            img.SetAsPrimary(false);
        }

        // Set the selected image as primary
        image.SetAsPrimary(true);
        return Result.Success;
    }

    public Result<Success> RemoveImage(Guid imageId)
    {
        if (_images.Count <= 1)
            return PropertyErrors.CannotDeleteLastImage;

        var image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image is null)
            return PropertyErrors.ImageNotFound;

        // If deleting the primary image, auto-select a new primary image
        if (image.IsPrimary && _images.Count > 1)
        {
            var nextPrimary = _images.FirstOrDefault(i => i.Id != imageId);
            if (nextPrimary is not null)
                nextPrimary.SetAsPrimary(true);
        }

        image.SetAsPrimary(false);
        _images.Remove(image);
        return Result.Success;
    }

    private static Error? ValidateStructure(int? floor, int? totalFloors)
    {
        if (floor.HasValue && floor.Value < 0)
            return PropertyErrors.FloorInvalid;

        if (floor.HasValue && floor.Value > 999)
            return Error.Validation("Property_FloorTooHigh", "Floor cannot exceed 999.");

        if (totalFloors.HasValue && totalFloors.Value <= 0)
            return PropertyErrors.TotalFloorsInvalid;

        if (totalFloors.HasValue && totalFloors.Value > 999)
            return Error.Validation("Property_TotalFloorsTooHigh", "Total floors cannot exceed 999.");

        if (floor.HasValue && totalFloors.HasValue && floor.Value > totalFloors.Value)
            return PropertyErrors.FloorRangeInvalid;

        return null;
    }
}
