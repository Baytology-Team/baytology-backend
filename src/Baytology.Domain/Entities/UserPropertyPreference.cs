using Baytology.Domain.Common;
using Baytology.Domain.Common.Enums;
using Baytology.Domain.Common.Results;

namespace Baytology.Domain.Entities;

public sealed class UserPropertyPreference : Entity
{
    public string UserId { get; private set; } = null!;
    public PropertyType? PropertyType { get; private set; }
    public ListingType? ListingType { get; private set; }
    public decimal? MinPrice { get; private set; }
    public decimal? MaxPrice { get; private set; }
    public decimal? MinArea { get; private set; }
    public decimal? MaxArea { get; private set; }
    public int? MinBedrooms { get; private set; }
    public int? MaxBedrooms { get; private set; }
    public int? MinBathrooms { get; private set; }
    public int? MaxBathrooms { get; private set; }
    public string? City { get; private set; }
    public string? District { get; private set; }
    public bool? HasParking { get; private set; }
    public bool? HasPool { get; private set; }
    public bool? HasGym { get; private set; }
    public bool? HasElevator { get; private set; }
    public bool? HasSecurity { get; private set; }
    public bool? HasBalcony { get; private set; }
    public bool? HasGarden { get; private set; }
    public bool? HasCentralAC { get; private set; }
    public FurnishingStatus? FurnishingStatus { get; private set; }
    public ViewType? ViewType { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedOnUtc { get; private set; }
    public DateTimeOffset? UpdatedOnUtc { get; private set; }

    private UserPropertyPreference() { }

    private UserPropertyPreference(
        string userId,
        PropertyType? propertyType,
        ListingType? listingType,
        decimal? minPrice,
        decimal? maxPrice,
        decimal? minArea,
        decimal? maxArea,
        int? minBedrooms,
        int? maxBedrooms,
        int? minBathrooms,
        int? maxBathrooms,
        string? city,
        string? district,
        bool? hasParking,
        bool? hasPool,
        bool? hasGym,
        bool? hasElevator,
        bool? hasSecurity,
        bool? hasBalcony,
        bool? hasGarden,
        bool? hasCentralAC,
        FurnishingStatus? furnishingStatus,
        ViewType? viewType) : base(Guid.NewGuid())
    {
        UserId = userId;
        PropertyType = propertyType;
        ListingType = listingType;
        MinPrice = minPrice;
        MaxPrice = maxPrice;
        MinArea = minArea;
        MaxArea = maxArea;
        MinBedrooms = minBedrooms;
        MaxBedrooms = maxBedrooms;
        MinBathrooms = minBathrooms;
        MaxBathrooms = maxBathrooms;
        City = city;
        District = district;
        HasParking = hasParking;
        HasPool = hasPool;
        HasGym = hasGym;
        HasElevator = hasElevator;
        HasSecurity = hasSecurity;
        HasBalcony = hasBalcony;
        HasGarden = hasGarden;
        HasCentralAC = hasCentralAC;
        FurnishingStatus = furnishingStatus;
        ViewType = viewType;
        IsActive = true;
        CreatedOnUtc = DateTimeOffset.UtcNow;
    }

    public static Result<UserPropertyPreference> Create(
        string userId,
        PropertyType? propertyType = null,
        ListingType? listingType = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        decimal? minArea = null,
        decimal? maxArea = null,
        int? minBedrooms = null,
        int? maxBedrooms = null,
        int? minBathrooms = null,
        int? maxBathrooms = null,
        string? city = null,
        string? district = null,
        bool? hasParking = null,
        bool? hasPool = null,
        bool? hasGym = null,
        bool? hasElevator = null,
        bool? hasSecurity = null,
        bool? hasBalcony = null,
        bool? hasGarden = null,
        bool? hasCentralAC = null,
        FurnishingStatus? furnishingStatus = null,
        ViewType? viewType = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Error.Validation("Preference_UserIdRequired", "User ID is required.");

        // Validate price range
        if (minPrice.HasValue && minPrice.Value < 0)
            return Error.Validation("Preference_MinPriceInvalid", "Minimum price cannot be negative.");

        if (maxPrice.HasValue && maxPrice.Value < 0)
            return Error.Validation("Preference_MaxPriceInvalid", "Maximum price cannot be negative.");

        if (minPrice.HasValue && maxPrice.HasValue && minPrice.Value > maxPrice.Value)
            return Error.Validation("Preference_PriceRangeInvalid", "Minimum price cannot be greater than maximum price.");

        // Validate area range
        if (minArea.HasValue && minArea.Value < 0)
            return Error.Validation("Preference_MinAreaInvalid", "Minimum area cannot be negative.");

        if (maxArea.HasValue && maxArea.Value < 0)
            return Error.Validation("Preference_MaxAreaInvalid", "Maximum area cannot be negative.");

        if (minArea.HasValue && maxArea.HasValue && minArea.Value > maxArea.Value)
            return Error.Validation("Preference_AreaRangeInvalid", "Minimum area cannot be greater than maximum area.");

        // Validate bedrooms range
        if (minBedrooms.HasValue && minBedrooms.Value < 0)
            return Error.Validation("Preference_MinBedroomsInvalid", "Minimum bedrooms cannot be negative.");

        if (maxBedrooms.HasValue && maxBedrooms.Value < 0)
            return Error.Validation("Preference_MaxBedroomsInvalid", "Maximum bedrooms cannot be negative.");

        if (minBedrooms.HasValue && maxBedrooms.HasValue && minBedrooms.Value > maxBedrooms.Value)
            return Error.Validation("Preference_BedroomsRangeInvalid", "Minimum bedrooms cannot be greater than maximum bedrooms.");

        // Validate bathrooms range
        if (minBathrooms.HasValue && minBathrooms.Value < 0)
            return Error.Validation("Preference_MinBathroomsInvalid", "Minimum bathrooms cannot be negative.");

        if (maxBathrooms.HasValue && maxBathrooms.Value < 0)
            return Error.Validation("Preference_MaxBathroomsInvalid", "Maximum bathrooms cannot be negative.");

        if (minBathrooms.HasValue && maxBathrooms.HasValue && minBathrooms.Value > maxBathrooms.Value)
            return Error.Validation("Preference_BathroomsRangeInvalid", "Minimum bathrooms cannot be greater than maximum bathrooms.");

        // Validate city and district
        var normalizedCity = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
        var normalizedDistrict = string.IsNullOrWhiteSpace(district) ? null : district.Trim();

        if (normalizedCity is not null && normalizedCity.Length > 100)
            return Error.Validation("Preference_CityTooLong", "City cannot exceed 100 characters.");

        if (normalizedDistrict is not null && normalizedDistrict.Length > 100)
            return Error.Validation("Preference_DistrictTooLong", "District cannot exceed 100 characters.");

        return new UserPropertyPreference(
            userId.Trim(),
            propertyType,
            listingType,
            minPrice,
            maxPrice,
            minArea,
            maxArea,
            minBedrooms,
            maxBedrooms,
            minBathrooms,
            maxBathrooms,
            normalizedCity,
            normalizedDistrict,
            hasParking,
            hasPool,
            hasGym,
            hasElevator,
            hasSecurity,
            hasBalcony,
            hasGarden,
            hasCentralAC,
            furnishingStatus,
            viewType);
    }

    public Result<Success> Update(
        PropertyType? propertyType = null,
        ListingType? listingType = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        decimal? minArea = null,
        decimal? maxArea = null,
        int? minBedrooms = null,
        int? maxBedrooms = null,
        int? minBathrooms = null,
        int? maxBathrooms = null,
        string? city = null,
        string? district = null,
        bool? hasParking = null,
        bool? hasPool = null,
        bool? hasGym = null,
        bool? hasElevator = null,
        bool? hasSecurity = null,
        bool? hasBalcony = null,
        bool? hasGarden = null,
        bool? hasCentralAC = null,
        FurnishingStatus? furnishingStatus = null,
        ViewType? viewType = null)
    {
        // Validate price range
        if (minPrice.HasValue && minPrice.Value < 0)
            return Error.Validation("Preference_MinPriceInvalid", "Minimum price cannot be negative.");

        if (maxPrice.HasValue && maxPrice.Value < 0)
            return Error.Validation("Preference_MaxPriceInvalid", "Maximum price cannot be negative.");

        if (minPrice.HasValue && maxPrice.HasValue && minPrice.Value > maxPrice.Value)
            return Error.Validation("Preference_PriceRangeInvalid", "Minimum price cannot be greater than maximum price.");

        // Validate area range
        if (minArea.HasValue && minArea.Value < 0)
            return Error.Validation("Preference_MinAreaInvalid", "Minimum area cannot be negative.");

        if (maxArea.HasValue && maxArea.Value < 0)
            return Error.Validation("Preference_MaxAreaInvalid", "Maximum area cannot be negative.");

        if (minArea.HasValue && maxArea.HasValue && minArea.Value > maxArea.Value)
            return Error.Validation("Preference_AreaRangeInvalid", "Minimum area cannot be greater than maximum area.");

        // Validate bedrooms range
        if (minBedrooms.HasValue && minBedrooms.Value < 0)
            return Error.Validation("Preference_MinBedroomsInvalid", "Minimum bedrooms cannot be negative.");

        if (maxBedrooms.HasValue && maxBedrooms.Value < 0)
            return Error.Validation("Preference_MaxBedroomsInvalid", "Maximum bedrooms cannot be negative.");

        if (minBedrooms.HasValue && maxBedrooms.HasValue && minBedrooms.Value > maxBedrooms.Value)
            return Error.Validation("Preference_BedroomsRangeInvalid", "Minimum bedrooms cannot be greater than maximum bedrooms.");

        // Validate bathrooms range
        if (minBathrooms.HasValue && minBathrooms.Value < 0)
            return Error.Validation("Preference_MinBathroomsInvalid", "Minimum bathrooms cannot be negative.");

        if (maxBathrooms.HasValue && maxBathrooms.Value < 0)
            return Error.Validation("Preference_MaxBathroomsInvalid", "Maximum bathrooms cannot be negative.");

        if (minBathrooms.HasValue && maxBathrooms.HasValue && minBathrooms.Value > maxBathrooms.Value)
            return Error.Validation("Preference_BathroomsRangeInvalid", "Minimum bathrooms cannot be greater than maximum bathrooms.");

        // Validate city and district
        var normalizedCity = string.IsNullOrWhiteSpace(city) ? null : city.Trim();
        var normalizedDistrict = string.IsNullOrWhiteSpace(district) ? null : district.Trim();

        if (normalizedCity is not null && normalizedCity.Length > 100)
            return Error.Validation("Preference_CityTooLong", "City cannot exceed 100 characters.");

        if (normalizedDistrict is not null && normalizedDistrict.Length > 100)
            return Error.Validation("Preference_DistrictTooLong", "District cannot exceed 100 characters.");

        PropertyType = propertyType;
        ListingType = listingType;
        MinPrice = minPrice;
        MaxPrice = maxPrice;
        MinArea = minArea;
        MaxArea = maxArea;
        MinBedrooms = minBedrooms;
        MaxBedrooms = maxBedrooms;
        MinBathrooms = minBathrooms;
        MaxBathrooms = maxBathrooms;
        City = normalizedCity;
        District = normalizedDistrict;
        HasParking = hasParking;
        HasPool = hasPool;
        HasGym = hasGym;
        HasElevator = hasElevator;
        HasSecurity = hasSecurity;
        HasBalcony = hasBalcony;
        HasGarden = hasGarden;
        HasCentralAC = hasCentralAC;
        FurnishingStatus = furnishingStatus;
        ViewType = viewType;
        UpdatedOnUtc = DateTimeOffset.UtcNow;

        return Result.Success;
    }

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;

    public bool MatchesProperty(Property property, PropertyAmenity? amenity)
    {
        if (!IsActive)
            return false;

        // Check property type
        if (PropertyType.HasValue && property.PropertyType != PropertyType.Value)
            return false;

        // Check listing type
        if (ListingType.HasValue && property.ListingType != ListingType.Value)
            return false;

        // Check price range
        if (MinPrice.HasValue && property.Price < MinPrice.Value)
            return false;

        if (MaxPrice.HasValue && property.Price > MaxPrice.Value)
            return false;

        // Check area range
        if (MinArea.HasValue && property.Area < MinArea.Value)
            return false;

        if (MaxArea.HasValue && property.Area > MaxArea.Value)
            return false;

        // Check bedrooms range
        if (MinBedrooms.HasValue && property.Bedrooms < MinBedrooms.Value)
            return false;

        if (MaxBedrooms.HasValue && property.Bedrooms > MaxBedrooms.Value)
            return false;

        // Check bathrooms range
        if (MinBathrooms.HasValue && property.Bathrooms < MinBathrooms.Value)
            return false;

        if (MaxBathrooms.HasValue && property.Bathrooms > MaxBathrooms.Value)
            return false;

        // Check city
        if (!string.IsNullOrWhiteSpace(City) && property.City != City)
            return false;

        // Check district
        if (!string.IsNullOrWhiteSpace(District) && property.District != District)
            return false;

        // Check amenities if provided
        if (amenity is not null)
        {
            if (HasParking.HasValue && HasParking.Value && !amenity.HasParking)
                return false;

            if (HasPool.HasValue && HasPool.Value && !amenity.HasPool)
                return false;

            if (HasGym.HasValue && HasGym.Value && !amenity.HasGym)
                return false;

            if (HasElevator.HasValue && HasElevator.Value && !amenity.HasElevator)
                return false;

            if (HasSecurity.HasValue && HasSecurity.Value && !amenity.HasSecurity)
                return false;

            if (HasBalcony.HasValue && HasBalcony.Value && !amenity.HasBalcony)
                return false;

            if (HasGarden.HasValue && HasGarden.Value && !amenity.HasGarden)
                return false;

            if (HasCentralAC.HasValue && HasCentralAC.Value && !amenity.HasCentralAC)
                return false;

            if (FurnishingStatus.HasValue && amenity.FurnishingStatus != FurnishingStatus.Value)
                return false;

            if (ViewType.HasValue && amenity.ViewType != ViewType.Value)
                return false;
        }

        return true;
    }
}
