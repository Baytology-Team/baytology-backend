using Baytology.Contracts.Common;

namespace Baytology.Contracts.Responses.PropertyPreferences;

public record GetPropertyPreferenceResponse(
    Guid Id,
    PropertyType? PropertyType,
    ListingType? ListingType,
    decimal? MinPrice,
    decimal? MaxPrice,
    decimal? MinArea,
    decimal? MaxArea,
    int? MinBedrooms,
    int? MaxBedrooms,
    int? MinBathrooms,
    int? MaxBathrooms,
    string? City,
    string? District,
    bool? HasParking,
    bool? HasPool,
    bool? HasGym,
    bool? HasElevator,
    bool? HasSecurity,
    bool? HasBalcony,
    bool? HasGarden,
    bool? HasCentralAC,
    FurnishingStatus? FurnishingStatus,
    ViewType? ViewType,
    bool IsActive,
    DateTimeOffset CreatedOnUtc,
    DateTimeOffset? UpdatedOnUtc);
