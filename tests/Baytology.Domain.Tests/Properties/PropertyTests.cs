using Baytology.Domain.Common.Enums;
using Baytology.Domain.Entities;
using Baytology.Domain.DomainEvents;

namespace Baytology.Domain.Tests.Properties;

public sealed class PropertyTests
{
    [Fact]
    public void Create_raises_property_created_event()
    {
        var result = Property.Create(
            "agent-1",
            "Luxury Apartment",
            "Nice property",
            PropertyType.Apartment,
            ListingType.Rent,
            10000m,
            150m,
            3,
            2,
            "Cairo",
            "Nasr City");

        Assert.True(result.IsSuccess);
        Assert.Equal(PropertyStatus.Available, result.Value.Status);
        Assert.Single(result.Value.DomainEvents);
        Assert.IsType<PropertyCreatedEvent>(result.Value.DomainEvents.Single());
    }

    [Theory]
    [InlineData(ListingType.Rent, PropertyStatus.Rented)]
    [InlineData(ListingType.Sale, PropertyStatus.Sold)]
    public void MarkUnavailableForConfirmedBooking_sets_status_based_on_listing_type(
        ListingType listingType,
        PropertyStatus expectedStatus)
    {
        var property = Property.Create(
            "agent-1",
            "Luxury Apartment",
            "Nice property",
            PropertyType.Apartment,
            listingType,
            10000m,
            150m,
            3,
            2).Value;

        property.MarkUnavailableForConfirmedBooking();

        Assert.Equal(expectedStatus, property.Status);
    }

    [Fact]
    public void Create_rejects_invalid_floor_range()
    {
        var result = Property.Create(
            "agent-1",
            "Luxury Apartment",
            "Nice property",
            PropertyType.Apartment,
            ListingType.Rent,
            10000m,
            150m,
            3,
            2,
            "Cairo",
            "Nasr City",
            floor: 8,
            totalFloors: 5);

        Assert.True(result.IsError);
        Assert.Equal("Property_Floor_Range_Invalid", result.TopError.Code);
    }

    [Fact]
    public void RemoveImage_auto_selects_new_primary_when_deleting_primary_image()
    {
        var property = Property.Create(
            "agent-1",
            "Luxury Apartment",
            "Nice property",
            PropertyType.Apartment,
            ListingType.Rent,
            10000m,
            150m,
            3,
            2).Value;

        var image1 = PropertyImage.Create(property.Id, "https://example.com/image1.jpg", true, 0).Value;
        var image2 = PropertyImage.Create(property.Id, "https://example.com/image2.jpg", false, 1).Value;
        var image3 = PropertyImage.Create(property.Id, "https://example.com/image3.jpg", false, 2).Value;

        property.AddImage(image1);
        property.AddImage(image2);
        property.AddImage(image3);

        var result = property.RemoveImage(image1.Id);

        Assert.True(result.IsSuccess);
        Assert.False(image1.IsPrimary);
        Assert.True(image2.IsPrimary);
        Assert.False(image3.IsPrimary);
    }

    [Fact]
    public void RemoveImage_rejects_deleting_last_image()
    {
        var property = Property.Create(
            "agent-1",
            "Luxury Apartment",
            "Nice property",
            PropertyType.Apartment,
            ListingType.Rent,
            10000m,
            150m,
            3,
            2).Value;

        var image1 = PropertyImage.Create(property.Id, "https://example.com/image1.jpg", true, 0).Value;
        property.AddImage(image1);

        var result = property.RemoveImage(image1.Id);

        Assert.True(result.IsError);
        Assert.Equal("Property_Cannot_Delete_Last_Image", result.TopError.Code);
    }

    [Fact]
    public void RemoveImage_allows_deleting_non_primary_image()
    {
        var property = Property.Create(
            "agent-1",
            "Luxury Apartment",
            "Nice property",
            PropertyType.Apartment,
            ListingType.Rent,
            10000m,
            150m,
            3,
            2).Value;

        var image1 = PropertyImage.Create(property.Id, "https://example.com/image1.jpg", true, 0).Value;
        var image2 = PropertyImage.Create(property.Id, "https://example.com/image2.jpg", false, 1).Value;
        property.AddImage(image1);
        property.AddImage(image2);

        var result = property.RemoveImage(image2.Id);

        Assert.True(result.IsSuccess);
        Assert.True(image1.IsPrimary);
        Assert.Single(property.Images);
    }
}
