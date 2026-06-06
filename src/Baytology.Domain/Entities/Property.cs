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

        if (price <= 0)
            return PropertyErrors.PriceInvalid;

        if (price < 1000)
            return Error.Validation("Property_PriceTooLow", "Price cannot be less than 1000.");

        if (area <= 0)
            return PropertyErrors.AreaInvalid;

        if (area < 10)
            return Error.Validation("Property_AreaTooSmall", "Area cannot be less than 10 square meters.");

        if (bedrooms < 0)
            return PropertyErrors.BedroomsInvalid;

        if (bathrooms < 0)
            return PropertyErrors.BathroomsInvalid;

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

        if (price <= 0)
            return PropertyErrors.PriceInvalid;

        if (price < 1000)
            return Error.Validation("Property_PriceTooLow", "Price cannot be less than 1000.");

        if (area <= 0)
            return PropertyErrors.AreaInvalid;

        if (area < 10)
            return Error.Validation("Property_AreaTooSmall", "Area cannot be less than 10 square meters.");

        if (bedrooms < 0)
            return PropertyErrors.BedroomsInvalid;

        if (bathrooms < 0)
            return PropertyErrors.BathroomsInvalid;

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

    public void SetLocation(string? addressLine, string? city, string? district, string? zipCode, decimal? lat, decimal? lng)
    {
        AddressLine = addressLine;
        City = city;
        District = district;
        ZipCode = zipCode;
        Latitude = lat;
        Longitude = lng;
    }

    public void ChangeStatus(PropertyStatus status) => Status = status;

    public void MarkUnavailableForConfirmedBooking()
    {
        Status = ListingType == ListingType.Rent
            ? PropertyStatus.Rented
            : PropertyStatus.Sold;
    }

    public void AddImage(PropertyImage image) => _images.Add(image);

    public Result<Success> RemoveImage(Guid imageId)
    {
        if (_images.Count <= 1)
            return PropertyErrors.CannotDeleteLastImage;

        var image = _images.FirstOrDefault(i => i.Id == imageId);
        if (image is null)
            return PropertyErrors.ImageNotFound;

        _images.Remove(image);
        return Result.Success;
    }

    private static Error? ValidateStructure(int? floor, int? totalFloors)
    {
        if (floor.HasValue && floor.Value < 0)
            return PropertyErrors.FloorInvalid;

        if (totalFloors.HasValue && totalFloors.Value <= 0)
            return PropertyErrors.TotalFloorsInvalid;

        if (floor.HasValue && totalFloors.HasValue && floor.Value > totalFloors.Value)
            return PropertyErrors.FloorRangeInvalid;

        return null;
    }
}
