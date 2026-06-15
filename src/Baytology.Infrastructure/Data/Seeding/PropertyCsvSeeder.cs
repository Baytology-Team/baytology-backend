using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Enums;
using Baytology.Domain.Entities;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Baytology.Infrastructure.Data.Seeding;

public class PropertyCsvSeeder(IAppDbContext context)
{
    public async Task SeedAsync(string csvFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(csvFilePath))
        {
            return;
        }

        using var reader = new StreamReader(csvFilePath);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        
        var records = csv.GetRecords<CsvPropertyRecord>();
        var properties = new List<Property>();
        var skippedCount = 0;
        var importedCount = 0;

        // Default agent user ID (use an existing agent from database)
        const string defaultAgentUserId = "04dc9322-313f-4a14-991f-a5beda247a32";

        foreach (var record in records)
        {
            try
            {
                // Map CSV record to Property entity
                var property = MapCsvToProperty(record, defaultAgentUserId);
                if (property != null)
                {
                    properties.Add(property);
                    importedCount++;
                }
                else
                {
                    skippedCount++;
                }

                // Batch insert every 1000 records to avoid memory issues
                if (properties.Count >= 1000)
                {
                    await context.Properties.AddRangeAsync(properties, cancellationToken);
                    await context.SaveChangesAsync(cancellationToken);
                    properties.Clear();
                }
            }
            catch (Exception)
            {
                skippedCount++;
            }
        }

        // Insert remaining records
        if (properties.Count > 0)
        {
            await context.Properties.AddRangeAsync(properties, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
        }
    }

    private Property? MapCsvToProperty(CsvPropertyRecord record, string agentUserId)
    {
        try
        {
            // Parse price
            if (!decimal.TryParse(record.Price, NumberStyles.Any, CultureInfo.InvariantCulture, out var price) || price <= 0)
            {
                return null;
            }

            // Parse area (prefer sqm, fallback to sqft converted to sqm)
            decimal area;
            if (!string.IsNullOrWhiteSpace(record.SizeSqm) && decimal.TryParse(record.SizeSqm, NumberStyles.Any, CultureInfo.InvariantCulture, out var areaSqm))
            {
                area = areaSqm;
            }
            else if (!string.IsNullOrWhiteSpace(record.SizeSqft) && decimal.TryParse(record.SizeSqft, NumberStyles.Any, CultureInfo.InvariantCulture, out var areaSqft))
            {
                area = areaSqft * 0.092903m; // Convert sqft to sqm
            }
            else if (!string.IsNullOrWhiteSpace(record.Size) && decimal.TryParse(record.Size, NumberStyles.Any, CultureInfo.InvariantCulture, out var size))
            {
                area = size;
            }
            else
            {
                return null;
            }

            // Parse bedrooms
            int bedrooms = 0;
            if (!string.IsNullOrWhiteSpace(record.Bedrooms) && int.TryParse(record.Bedrooms, out var beds))
            {
                bedrooms = beds;
            }

            // Parse bathrooms
            int bathrooms = 0;
            if (!string.IsNullOrWhiteSpace(record.Bathrooms) && int.TryParse(record.Bathrooms, out var baths))
            {
                bathrooms = baths;
            }

            // Determine property type from CSV 'type' field
            var propertyType = DeterminePropertyType(record.Type);

            // Determine listing type (default to Sale for now)
            var listingType = ListingType.Sale;

            // Create title from description or location
            var title = !string.IsNullOrWhiteSpace(record.Description) 
                ? TruncateString(record.Description, 100) 
                : !string.IsNullOrWhiteSpace(record.Location) 
                    ? TruncateString(record.Location, 100)
                    : "Property for Sale";

            // Map city (validate against Egyptian cities)
            var city = ValidateCity(record.City);

            // Map district
            var district = !string.IsNullOrWhiteSpace(record.District) ? record.District : null;

            // Create property
            var result = Property.Create(
                agentUserId: agentUserId,
                title: title,
                description: record.Description,
                propertyType: propertyType,
                listingType: listingType,
                price: price,
                area: area,
                bedrooms: bedrooms,
                bathrooms: bathrooms,
                city: city,
                district: district,
                sourceListingUrl: record.Url);

            if (result.IsError)
            {
                return null;
            }

            var property = result.Value;
            property.SetSourceListingUrl(record.Url);

            return property;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private PropertyType DeterminePropertyType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return PropertyType.Apartment;

        var lowerType = type.ToLowerInvariant();
        
        return lowerType switch
        {
            "apartment" or "flat" or "شقة" or "studio" or "ستوديو" or "penthouse" or "بنتهاوس" or "duplex" or "دوبلكس" or "townhouse" or "تاون هاوس" or "chalet" or "شاليه" => PropertyType.Apartment,
            "villa" or "فيلا" => PropertyType.Villa,
            "land" or "plot" or "أرض" => PropertyType.Land,
            "commercial" or "office" or "مكتب" => PropertyType.Office,
            _ => PropertyType.Apartment
        };
    }

    private string? ValidateCity(string? city)
    {
        if (string.IsNullOrWhiteSpace(city))
            return "Cairo"; // Default to Cairo

        var validCities = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Cairo", "القاهرة",
            "Giza", "الجيزة",
            "Alexandria", "الإسكندرية",
            "Dakahlia", "الدقهلية",
            "Red Sea", "البحر الأحمر",
            "Beheira", "البحيرة",
            "Faiyum", "الفيوم",
            "Gharbia", "الغربية",
            "Ismailia", "الإسماعيلية",
            "Menofia", "المنوفية",
            "Minya", "المنيا",
            "Qaliubiya", "القليوبية",
            "New Valley", "الوادي الجديد",
            "Suez", "السويس",
            "Aswan", "أسوان",
            "Asyut", "أسيوط",
            "Beni Suef", "بني سويف",
            "Port Said", "بورسعيد",
            "Damietta", "دمياط",
            "Sharkia", "الشرقية",
            "Sohag", "سوهاج",
            "South Sinai", "جنوب سيناء",
            "Kafr El Sheikh", "كفر الشيخ",
            "Matrouh", "مطروح",
            "Luxor", "الأقصر",
            "Qena", "قنا",
            "North Sinai", "شمال سيناء",
            "Hurghada", "الغردقة"
        };

        return validCities.Contains(city.Trim()) ? city.Trim() : "Cairo";
    }

    private string TruncateString(string value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Length > maxLength ? value.Substring(0, maxLength) : value;
    }
}

public class CsvPropertyRecord
{
    public string Url { get; set; } = string.Empty;
    public string Price { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Size { get; set; } = string.Empty;
    public string Bedrooms { get; set; } = string.Empty;
    public string Bathrooms { get; set; } = string.Empty;
    public string AvailableFrom { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string DownPayment { get; set; } = string.Empty;
    public string SizeSqft { get; set; } = string.Empty;
    public string UnitSqft { get; set; } = string.Empty;
    public string SizeSqm { get; set; } = string.Empty;
    public string UnitSqm { get; set; } = string.Empty;
    public string MidRoom { get; set; } = string.Empty;
    public string Compound { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Governorate { get; set; } = string.Empty;
    public string PricePerSqm { get; set; } = string.Empty;
    public string PricePerSqft { get; set; } = string.Empty;
}
