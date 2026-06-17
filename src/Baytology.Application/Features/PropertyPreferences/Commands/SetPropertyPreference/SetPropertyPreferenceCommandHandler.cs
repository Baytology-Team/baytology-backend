using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Enums;
using Baytology.Domain.Common.Results;
using Baytology.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.PropertyPreferences.Commands.SetPropertyPreference;

public class SetPropertyPreferenceCommandHandler(IAppDbContext context)
    : IRequestHandler<SetPropertyPreferenceCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SetPropertyPreferenceCommand request, CancellationToken ct)
    {
        // Check if user already has a preference
        var existingPreference = await context.UserPropertyPreferences
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);

        if (existingPreference is not null)
        {
            // Update existing preference
            var updateResult = existingPreference.Update(
                request.PropertyType,
                request.ListingType,
                request.MinPrice,
                request.MaxPrice,
                request.MinArea,
                request.MaxArea,
                request.MinBedrooms,
                request.MaxBedrooms,
                request.MinBathrooms,
                request.MaxBathrooms,
                request.City,
                request.District,
                request.HasParking,
                request.HasPool,
                request.HasGym,
                request.HasElevator,
                request.HasSecurity,
                request.HasBalcony,
                request.HasGarden,
                request.HasCentralAC,
                request.FurnishingStatus,
                request.ViewType);

            if (updateResult.IsError)
                return updateResult.Errors;

            existingPreference.Activate();
            await context.SaveChangesAsync(ct);
            return existingPreference.Id;
        }

        // Create new preference
        var preferenceResult = UserPropertyPreference.Create(
            request.UserId,
            request.PropertyType,
            request.ListingType,
            request.MinPrice,
            request.MaxPrice,
            request.MinArea,
            request.MaxArea,
            request.MinBedrooms,
            request.MaxBedrooms,
            request.MinBathrooms,
            request.MaxBathrooms,
            request.City,
            request.District,
            request.HasParking,
            request.HasPool,
            request.HasGym,
            request.HasElevator,
            request.HasSecurity,
            request.HasBalcony,
            request.HasGarden,
            request.HasCentralAC,
            request.FurnishingStatus,
            request.ViewType);

        if (preferenceResult.IsError)
            return preferenceResult.Errors;

        var preference = preferenceResult.Value;
        context.UserPropertyPreferences.Add(preference);
        await context.SaveChangesAsync(ct);

        return preference.Id;
    }
}
