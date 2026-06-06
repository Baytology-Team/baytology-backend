using Baytology.Application.Common.Caching;
using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Enums;
using Baytology.Domain.Common.Results;

using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.Properties.Commands.UpdateProperty;

public class UpdatePropertyCommandHandler(IAppDbContext context)
    : IRequestHandler<UpdatePropertyCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(UpdatePropertyCommand request, CancellationToken ct)
    {
        var property = await context.Properties
            .Include(p => p.Amenity)
            .FirstOrDefaultAsync(p => p.Id == request.PropertyId, ct);

        if (property is null) return ApplicationErrors.Property.NotFound;
        
        if (property.AgentUserId != request.AgentUserId) 
            return ApplicationErrors.Property.AccessDenied;

        // Check for duplicate property based on location (excluding current property)
        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            const decimal tolerance = 0.0001m; // ~11 meters tolerance for GPS accuracy

            var existingProperty = await context.Properties
                .Where(p => p.Latitude.HasValue && p.Longitude.HasValue)
                .Where(p => p.Id != request.PropertyId) // Exclude current property
                .Where(p => Math.Abs(p.Latitude.Value - request.Latitude.Value) < tolerance &&
                           Math.Abs(p.Longitude.Value - request.Longitude.Value) < tolerance)
                .Where(p => p.AgentUserId == request.AgentUserId)
                .FirstOrDefaultAsync(ct);

            if (existingProperty is not null)
            {
                return Error.Validation("Property_DuplicateLocation", 
                    "A property at this location already exists. You cannot list the same property multiple times.");
            }
        }

        var updateResult = property.Update(
            request.Title, request.Description, request.PropertyType, request.ListingType,
            request.Price, request.Area, request.Bedrooms, request.Bathrooms,
            request.Floor, request.TotalFloors, request.AddressLine, request.City, 
            request.District, request.ZipCode, request.Latitude, request.Longitude, request.IsFeatured);

        if (updateResult.IsError)
            return updateResult.Errors;

        Domain.Entities.PropertyAmenity amenity;
        if (property.Amenity is null)
        {
            var amenityResult = Domain.Entities.PropertyAmenity.Create(property.Id);
            if (amenityResult.IsError)
                return amenityResult.Errors;

            amenity = amenityResult.Value;
            context.PropertyAmenities.Add(amenity);
        }
        else
        {
            amenity = property.Amenity;
        }

        amenity.Update(
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

        await context.SaveChangesAsync(ct);
        return Result.Success;
    }
}
