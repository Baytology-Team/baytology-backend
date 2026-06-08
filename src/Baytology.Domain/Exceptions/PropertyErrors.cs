using Baytology.Domain.Common.Results;

namespace Baytology.Domain.Exceptions;

public static class PropertyErrors
{
    public static readonly Error AgentRequired =
        Error.Validation("Property_Agent_Required", "Agent user ID is required.");

    public static readonly Error TitleRequired =
        Error.Validation("Property_Title_Required", "Property title is required.");

    public static readonly Error PriceInvalid =
        Error.Validation("Property_Price_Invalid", "Property price must be greater than zero.");

    public static readonly Error AreaInvalid =
        Error.Validation("Property_Area_Invalid", "Property area must be greater than zero.");

    public static readonly Error BedroomsInvalid =
        Error.Validation("Property_Bedrooms_Invalid", "Bedrooms cannot be negative.");

    public static readonly Error BathroomsInvalid =
        Error.Validation("Property_Bathrooms_Invalid", "Bathrooms cannot be negative.");

    public static readonly Error FloorInvalid =
        Error.Validation("Property_Floor_Invalid", "Floor cannot be negative.");

    public static readonly Error TotalFloorsInvalid =
        Error.Validation("Property_TotalFloors_Invalid", "Total floors must be greater than zero.");

    public static readonly Error FloorRangeInvalid =
        Error.Validation("Property_Floor_Range_Invalid", "Floor cannot exceed total floors.");

    public static readonly Error PropertyIdRequired =
        Error.Validation("Property_Id_Required", "Property id is required.");

    public static readonly Error ImageUrlRequired =
        Error.Validation("Property_ImageUrl_Required", "Image url is required.");

    public static readonly Error ImageUrlTooLong =
        Error.Validation("Property_ImageUrl_TooLong", "Image url cannot exceed 1000 characters.");

    public static readonly Error ImageSortOrderInvalid =
        Error.Validation("Property_ImageSortOrder_Invalid", "Image sort order cannot be negative.");

    public static readonly Error SavedPropertyUserRequired =
        Error.Validation("SavedProperty_User_Required", "User id is required.");

    public static readonly Error IpAddressTooLong =
        Error.Validation("PropertyView_IpAddress_TooLong", "IP address cannot exceed 50 characters.");

    public static readonly Error NotFound =
        Error.NotFound("Property_Not_Found", "Property not found.");

    public static readonly Error AlreadySaved =
        Error.Conflict("Property_Already_Saved", "Property is already saved.");

    public static readonly Error NotSaved =
        Error.NotFound("Property_Not_Saved", "Property is not in your saved list.");

    public static readonly Error ImageNotFound =
        Error.NotFound("Property_Image_Not_Found", "Image not found.");

    public static readonly Error CannotDeleteLastImage =
        Error.Conflict("Property_Cannot_Delete_Last_Image", "Cannot delete the last image of a property.");

    public static readonly Error CityInvalid =
        Error.Validation("Property_City_Invalid", "City must be a valid Egyptian city.");

    public static readonly Error DistrictInvalid =
        Error.Validation("Property_District_Invalid", "District name is invalid.");

    public static readonly Error ZipCodeInvalid =
        Error.Validation("Property_ZipCode_Invalid", "Zip code must be a valid Egyptian postal code (5 digits).");

    public static readonly Error ImageUrlInvalid =
        Error.Validation("Property_ImageUrl_Invalid", "Image URL must be a valid URL.");

    public static readonly Error LocationRequired =
        Error.Validation("Property_Location_Required", "At least city or district must be provided.");
}
