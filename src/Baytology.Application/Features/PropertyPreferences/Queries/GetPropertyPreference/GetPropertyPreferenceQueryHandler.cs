using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Enums;
using Baytology.Domain.Common.Results;
using Baytology.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.PropertyPreferences.Queries.GetPropertyPreference;

public record GetPropertyPreferenceQuery(string UserId) : IRequest<Result<PropertyPreferenceDto>>;

public record PropertyPreferenceDto(
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

public class GetPropertyPreferenceQueryHandler(IAppDbContext context)
    : IRequestHandler<GetPropertyPreferenceQuery, Result<PropertyPreferenceDto>>
{
    public async Task<Result<PropertyPreferenceDto>> Handle(GetPropertyPreferenceQuery request, CancellationToken ct)
    {
        var preference = await context.UserPropertyPreferences
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);

        if (preference is null)
            return Error.NotFound("PropertyPreference.NotFound", "No property preference found for this user.");

        return new PropertyPreferenceDto(
            preference.Id,
            preference.PropertyType,
            preference.ListingType,
            preference.MinPrice,
            preference.MaxPrice,
            preference.MinArea,
            preference.MaxArea,
            preference.MinBedrooms,
            preference.MaxBedrooms,
            preference.MinBathrooms,
            preference.MaxBathrooms,
            preference.City,
            preference.District,
            preference.HasParking,
            preference.HasPool,
            preference.HasGym,
            preference.HasElevator,
            preference.HasSecurity,
            preference.HasBalcony,
            preference.HasGarden,
            preference.HasCentralAC,
            preference.FurnishingStatus,
            preference.ViewType,
            preference.IsActive,
            preference.CreatedOnUtc,
            preference.UpdatedOnUtc);
    }
}
