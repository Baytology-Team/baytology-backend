using Baytology.Domain.Common.Enums;
using Baytology.Domain.Common.Results;
using MediatR;

namespace Baytology.Application.Features.PropertyPreferences.Commands.SetPropertyPreference;

public record SetPropertyPreferenceCommand(
    string UserId,
    PropertyType? PropertyType = null,
    ListingType? ListingType = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    decimal? MinArea = null,
    decimal? MaxArea = null,
    int? MinBedrooms = null,
    int? MaxBedrooms = null,
    int? MinBathrooms = null,
    int? MaxBathrooms = null,
    string? City = null,
    string? District = null,
    bool? HasParking = null,
    bool? HasPool = null,
    bool? HasGym = null,
    bool? HasElevator = null,
    bool? HasSecurity = null,
    bool? HasBalcony = null,
    bool? HasGarden = null,
    bool? HasCentralAC = null,
    FurnishingStatus? FurnishingStatus = null,
    ViewType? ViewType = null) : IRequest<Result<Guid>>;
