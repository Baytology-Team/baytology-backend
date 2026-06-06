using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Results;
using Baytology.Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.Properties.Commands.CreateProperty;

public class CreatePropertyCommandHandler(IAppDbContext context)
    : IRequestHandler<CreatePropertyCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreatePropertyCommand request, CancellationToken ct)
    {
        // Check for duplicate property based on location
        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            const decimal tolerance = 0.0001m; // ~11 meters tolerance for GPS accuracy

            var existingProperty = await context.Properties
                .Where(p => p.Latitude.HasValue && p.Longitude.HasValue)
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

        var propertyResult = Property.Create(
            request.AgentUserId,
            request.Title,
            request.Description,
            request.PropertyType,
            request.ListingType,
            request.Price,
            request.Area,
            request.Bedrooms,
            request.Bathrooms,
            request.City,
            request.District,
            floor: request.Floor,
            totalFloors: request.TotalFloors);

        if (propertyResult.IsError)
            return propertyResult.Errors;

        var property = propertyResult.Value;

        var locationResult = property.SetLocation(
            request.AddressLine, request.City, request.District,
            request.ZipCode, request.Latitude, request.Longitude);

        if (locationResult.IsError)
            return locationResult.Errors;

        var amenityResult = PropertyAmenity.Create(property.Id);
        if (amenityResult.IsError)
            return amenityResult.Errors;

        var amenity = amenityResult.Value;
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

        context.PropertyAmenities.Add(amenity);

        if (request.ImageUrls is not null)
        {
            for (var i = 0; i < request.ImageUrls.Count; i++)
            {
                var imageResult = PropertyImage.Create(property.Id, request.ImageUrls[i], i == 0, i);
                if (imageResult.IsError)
                    return imageResult.Errors;

                property.AddImage(imageResult.Value);
            }
        }

        context.Properties.Add(property);
        await context.SaveChangesAsync(ct);

        return property.Id;
    }
}
