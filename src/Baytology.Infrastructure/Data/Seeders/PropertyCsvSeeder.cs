using System.Globalization;
using System.Text.Json;

using Baytology.Domain.Common.Enums;
using Baytology.Domain.Entities;
using Baytology.Infrastructure.Identity;

using CsvHelper;
using CsvHelper.Configuration;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Baytology.Infrastructure.Data.Seeders;

public static class PropertyCsvSeeder
{
    public static async Task SeedAsync(IServiceProvider services, ILogger logger)
    {
        var context = services.GetRequiredService<AppDbContext>();

        var csvPath = FindCsvFile();
        if (csvPath is null)
        {
            logger.LogWarning("CSV file not found. Skipping property seeding.");
            return;
        }

        // Ensure system agent user exists
        var userManager = services.GetRequiredService<UserManager<AppUser>>();
        const string agentEmail = "agent@baytology.com";
        var agentUser = await userManager.FindByEmailAsync(agentEmail);

        if (agentUser is null)
        {
            agentUser = new AppUser
            {
                UserName = agentEmail,
                Email = agentEmail,
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(agentUser, "Agent@123456");
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to create CSV seed agent user: {FormatIdentityErrors(result.Errors)}");
            }
        }

        if (!await userManager.IsInRoleAsync(agentUser, Role.Agent))
        {
            var roleResult = await userManager.AddToRoleAsync(agentUser, Role.Agent);
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Failed to add CSV seed agent user to role '{Role.Agent}': {FormatIdentityErrors(roleResult.Errors)}");
            }
        }

        await BackfillSourceListingMetadataAsync(services, logger, csvPath);
        await SyncPropertiesFromCsvAsync(context, logger, csvPath, agentUser!.Id);
    }

    private static async Task SyncPropertiesFromCsvAsync(
        AppDbContext context,
        ILogger logger,
        string csvPath,
        string agentUserId)
    {
        logger.LogInformation("Synchronizing properties from CSV: {Path}", csvPath);
        var imageIdMapping = LoadImageIdMapping(csvPath, logger);

        var existingProperties = await context.Properties
            .AsNoTracking()
            .Select(property => new
            {
                property.Title,
                property.Price,
                property.City,
                property.District,
                property.Area,
                property.Bedrooms,
                property.Bathrooms,
                property.SourceListingUrl
            })
            .ToListAsync();

        var normalizedExistingUrls = existingProperties
            .Select(property => NormalizeUrl(property.SourceListingUrl))
            .Where(url => !string.IsNullOrWhiteSpace(url))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var knownSignatures = existingProperties
            .Select(property => BuildSignature(
                property.Title,
                property.Price,
                property.City,
                property.District,
                property.Area,
                property.Bedrooms,
                property.Bathrooms))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var importedCount = 0;
        var skippedCount = 0;
        var processedCount = 0;
        const int batchSize = 250;
        var autoDetectChangesEnabled = context.ChangeTracker.AutoDetectChangesEnabled;

        context.ChangeTracker.AutoDetectChangesEnabled = false;

        try
        {
            using var reader = new StreamReader(csvPath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
                HeaderValidated = null
            });

            await foreach (var record in csv.GetRecordsAsync<dynamic>())
            {
                processedCount++;

                try
                {
                    var dict = (IDictionary<string, object>)record;

                    string? GetString(string key)
                        => dict.TryGetValue(key, out var value) ? value?.ToString() : null;

                    var price = ParseDecimal(GetString("price"));
                    var bedrooms = ParseInt(GetString("bedrooms"));
                    var bathrooms = ParseInt(GetString("bathrooms"));
                    var area = ParseDecimal(GetString("size_sqm"));
                    var city = TrimToLength(GetString("city"), 100);
                    var district = TrimToLength(GetString("district"), 100);
                    var description = TrimToLength(GetString("description"), 5000);
                    var compound = TrimToLength(GetString("compound"), 500);
                    var type = GetString("type")?.Trim()?.ToLower();
                    var url = TrimToLength(GetString("url"), 1000);
                    var sourceId = ResolveSourceId(GetString("Image_Path"), imageIdMapping);
                    var normalizedUrl = NormalizeUrl(url);

                    if (price <= 0)
                    {
                        skippedCount++;
                        continue;
                    }

                    var propertyType = type switch
                    {
                        var t when t?.Contains("villa") == true => PropertyType.Villa,
                        var t when t?.Contains("office") == true => PropertyType.Office,
                        var t when t?.Contains("land") == true => PropertyType.Land,
                        _ => PropertyType.Apartment
                    };

                    var title = TrimToLength(BuildSeedTitle(compound, city, type), 500) ?? "Property in Egypt";
                    var signature = BuildSignature(title, price, city, district, area, bedrooms, bathrooms);

                    if ((!string.IsNullOrWhiteSpace(normalizedUrl) && normalizedExistingUrls.Contains(normalizedUrl)) ||
                        knownSignatures.Contains(signature))
                    {
                        skippedCount++;
                        continue;
                    }

                    var propertyResult = Property.Create(
                        agentUserId,
                        title,
                        description?[..Math.Min(description.Length, 5000)],
                        propertyType,
                        InferListingType(url),
                        price,
                        area,
                        bedrooms,
                        bathrooms,
                        city,
                        district,
                        url,
                        id: sourceId);

                    if (propertyResult.IsError)
                    {
                        skippedCount++;
                        continue;
                    }

                    var property = propertyResult.Value;
                    property.ClearDomainEvents();

                    var amenityResult = PropertyAmenity.Create(property.Id);
                    if (amenityResult.IsError)
                    {
                        skippedCount++;
                        continue;
                    }

                    context.Properties.Add(property);
                    context.PropertyAmenities.Add(amenityResult.Value);

                    if (!string.IsNullOrWhiteSpace(normalizedUrl))
                        normalizedExistingUrls.Add(normalizedUrl);

                    knownSignatures.Add(signature);
                    importedCount++;

                    if (importedCount % batchSize == 0)
                    {
                        await context.SaveChangesAsync();
                        context.ChangeTracker.Clear();
                        logger.LogInformation("Imported {Count} additional properties from CSV so far...", importedCount);
                    }
                }
                catch
                {
                    skippedCount++;
                }
            }

            await context.SaveChangesAsync();
            context.ChangeTracker.Clear();
            logger.LogInformation(
                "Property CSV synchronization finished. Processed={Processed}, Imported={Imported}, Skipped={Skipped}.",
                processedCount,
                importedCount,
                skippedCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error synchronizing properties from CSV.");
        }
        finally
        {
            context.ChangeTracker.AutoDetectChangesEnabled = autoDetectChangesEnabled;
        }
    }

    private static string? FindCsvFile()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "Search_By_Image", "data", "updated_properties.csv"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Search_By_Image", "data", "updated_properties.csv"),
            Path.Combine(Directory.GetCurrentDirectory(), "Search_By_Image", "data", "updated_properties.csv"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "egypt_real_estate_preprocessed_analysis-and-segmentation.csv"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "egypt_real_estate_preprocessed_analysis-and-segmentation.csv"),
            Path.Combine(Directory.GetCurrentDirectory(), "egypt_real_estate_preprocessed_analysis-and-segmentation.csv"),
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static IReadOnlyDictionary<string, Guid> LoadImageIdMapping(string csvPath, ILogger logger)
    {
        var mappingPath = Path.Combine(Path.GetDirectoryName(csvPath) ?? string.Empty, "faiss_id_mapping.json");
        if (!File.Exists(mappingPath))
            return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(mappingPath));
            var mapping = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in document.RootElement.EnumerateObject())
            {
                if (Guid.TryParse(property.Value.GetString(), out var id))
                    mapping[property.Name] = id;
            }

            logger.LogInformation("Loaded {Count} image-search FAISS ID mappings from {Path}.", mapping.Count, mappingPath);
            return mapping;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load image-search FAISS ID mappings from {Path}.", mappingPath);
            return new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static Guid? ResolveSourceId(string? value, IReadOnlyDictionary<string, Guid> imageIdMapping)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var fileName = Path.GetFileNameWithoutExtension(value.Trim());
        if (Guid.TryParse(fileName, out var id))
            return id;

        return imageIdMapping.TryGetValue(fileName, out var mappedId)
            ? mappedId
            : null;
    }

    private static decimal ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        value = new string(value.Where(c => char.IsDigit(c) || c == '.').ToArray());
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : 0;
    }

    private static int ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return 0;
        var normalized = new string(value.Where(c => char.IsDigit(c) || c == '.' || c == '-' || c == ',').ToArray())
            .Replace(",", string.Empty);

        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.InvariantCulture, out var result)
            ? (int)Math.Round(result, MidpointRounding.AwayFromZero)
            : 0;
    }

    private static ListingType InferListingType(string? url)
    {
        if (!string.IsNullOrWhiteSpace(url) && url.Contains("/rent/", StringComparison.OrdinalIgnoreCase))
            return ListingType.Rent;

        return ListingType.Sale;
    }

    private static async Task BackfillSourceListingMetadataAsync(IServiceProvider services, ILogger logger, string csvPath)
    {
        var context = services.GetRequiredService<AppDbContext>();

        var propertiesToBackfill = await context.Properties
            .Where(property => property.SourceListingUrl == null)
            .ToListAsync();

        if (propertiesToBackfill.Count == 0)
            return;

        try
        {
            using var reader = new StreamReader(csvPath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                MissingFieldFound = null,
                BadDataFound = null,
                HeaderValidated = null
            });

            var records = csv.GetRecords<dynamic>().ToList();
            var recordLookup = new Dictionary<string, Queue<(string? Url, ListingType ListingType)>>(StringComparer.OrdinalIgnoreCase);

            foreach (var record in records)
            {
                try
                {
                    var dict = (IDictionary<string, object>)record;

                    string? GetString(string key)
                        => dict.TryGetValue(key, out var value) ? value?.ToString() : null;

                    var title = BuildSeedTitle(GetString("compound")?.Trim(), GetString("city")?.Trim(), GetString("type")?.Trim()?.ToLower());
                    var key = BuildSignature(
                        title,
                        ParseDecimal(GetString("price")),
                        GetString("city")?.Trim(),
                        GetString("district")?.Trim(),
                        ParseDecimal(GetString("size_sqm")),
                        ParseInt(GetString("bedrooms")),
                        ParseInt(GetString("bathrooms")));

                    if (!recordLookup.TryGetValue(key, out var queue))
                    {
                        queue = new Queue<(string? Url, ListingType ListingType)>();
                        recordLookup[key] = queue;
                    }

                    var url = GetString("url")?.Trim();
                    queue.Enqueue((url, InferListingType(url)));
                }
                catch
                {
                    // Ignore malformed CSV rows during metadata backfill.
                }
            }

            var updated = 0;

            foreach (var property in propertiesToBackfill)
            {
                var key = BuildSignature(
                    property.Title,
                    property.Price,
                    property.City,
                    property.District,
                    property.Area,
                    property.Bedrooms,
                    property.Bathrooms);

                if (!recordLookup.TryGetValue(key, out var candidates) || candidates.Count == 0)
                    continue;

                var candidate = candidates.Dequeue();
                property.SetSourceListingUrl(candidate.Url);

                if (property.ListingType != candidate.ListingType)
                {
                    var updateResult = property.Update(
                        property.Title,
                        property.Description,
                        property.PropertyType,
                        candidate.ListingType,
                        property.Price,
                        property.Area,
                        property.Bedrooms,
                        property.Bathrooms,
                        property.Floor,
                        property.TotalFloors,
                        property.AddressLine,
                        property.City,
                        property.District,
                        property.ZipCode,
                        property.Latitude,
                        property.Longitude,
                        property.IsFeatured);

                    if (updateResult.IsError)
                    {
                        continue;
                    }
                }

                updated++;
            }

            if (updated > 0)
            {
                await context.SaveChangesAsync();
                logger.LogInformation("Backfilled source listing metadata for {Count} existing properties.", updated);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error backfilling property source listing metadata.");
        }
    }

    private static string BuildSeedTitle(string? compound, string? city, string? type)
    {
        var propertyType = type switch
        {
            var t when t?.Contains("villa") == true => "Villa",
            var t when t?.Contains("office") == true => "Office",
            var t when t?.Contains("land") == true => "Land",
            _ => "Apartment"
        };

        return !string.IsNullOrWhiteSpace(compound)
            ? compound
            : $"{propertyType} in {city ?? "Egypt"}";
    }

    private static string BuildSignature(
        string? title,
        decimal price,
        string? city,
        string? district,
        decimal area,
        int bedrooms,
        int bathrooms)
    {
        return string.Join(
            '|',
            Normalize(title),
            price.ToString("0.##", CultureInfo.InvariantCulture),
            Normalize(city),
            Normalize(district),
            area.ToString("0.##", CultureInfo.InvariantCulture),
            bedrooms.ToString(CultureInfo.InvariantCulture),
            bathrooms.ToString(CultureInfo.InvariantCulture));
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty).Trim().ToLowerInvariant();
    }

    private static string NormalizeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        if (Uri.TryCreate(value, UriKind.Absolute, out var uri))
            return uri.GetLeftPart(UriPartial.Path).TrimEnd('/').ToLowerInvariant();

        return value.Trim().TrimEnd('/').ToLowerInvariant();
    }

    private static string? TrimToLength(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength
            ? trimmed
            : trimmed[..maxLength];
    }

    private static string FormatIdentityErrors(IEnumerable<IdentityError> errors)
        => string.Join(", ", errors.Select(error => error.Description));
}
