using System.Net.Http.Json;
using Baytology.Application.Common.Errors;
using System.Net.Http.Headers;
using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Enums;
using Baytology.Domain.Common.Results;
using Baytology.Infrastructure.Settings;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Baytology.Infrastructure.AI;

internal sealed class ChatbotApiClient(
    HttpClient httpClient,
    IOptions<ExternalAiServicesSettings> settings,
    ILogger<ChatbotApiClient> logger)
    : IChatbotApiClient
{
    private readonly ExternalAiServicesSettings _settings = settings.Value;

    public Task<Result<JsonNode?>> GetStatusAsync(CancellationToken ct = default)
        => GetAsync("/", "Chatbot", isEnabled: _settings.ChatbotEnabled, ct);

    public Task<Result<JsonNode?>> ParseAsync(JsonElement payload, CancellationToken ct = default)
        => PostAsync("/parse", payload, "Chatbot.Parse", isEnabled: _settings.ChatbotEnabled, ct);

    public Task<Result<JsonNode?>> AskQuestionAsync(JsonElement payload, CancellationToken ct = default)
        => PostAsync("/question", payload, "Chatbot.Question", isEnabled: _settings.ChatbotEnabled, ct);

    public Task<Result<JsonNode?>> SearchAsync(JsonElement payload, CancellationToken ct = default)
        => PostAsync("/search", payload, "Chatbot.Search", isEnabled: _settings.ChatbotEnabled, ct);

    public Task<Result<JsonNode?>> RankAsync(JsonElement payload, CancellationToken ct = default)
        => PostAsync("/rank", payload, "Chatbot.Rank", isEnabled: _settings.ChatbotEnabled, ct);

    public Task<Result<JsonNode?>> ChatAsync(JsonElement payload, CancellationToken ct = default)
        => PostAsync("/chat", payload, "Chatbot.Chat", isEnabled: _settings.ChatbotEnabled, ct);

    private async Task<Result<JsonNode?>> GetAsync(string path, string serviceName, bool isEnabled, CancellationToken ct)
    {
        if (!TryValidateConfiguration(serviceName, isEnabled, _settings.ChatbotBaseUrl, out var error))
            return error;

        try
        {
            using var response = await httpClient.GetAsync(path, ct);
            return await ReadResponseAsync(response, serviceName, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ServiceName} request failed.", serviceName);
            return ApplicationErrors.ExternalAi.Unavailable(serviceName);
        }
    }

    private async Task<Result<JsonNode?>> PostAsync(
        string path,
        JsonElement payload,
        string serviceName,
        bool isEnabled,
        CancellationToken ct)
    {
        if (!TryValidateConfiguration(serviceName, isEnabled, _settings.ChatbotBaseUrl, out var error))
            return error;

        try
        {
            using var response = await httpClient.PostAsJsonAsync(path, payload, cancellationToken: ct);
            return await ReadResponseAsync(response, serviceName, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ServiceName} request failed.", serviceName);
            return ApplicationErrors.ExternalAi.Unavailable(serviceName);
        }
    }

    private static bool TryValidateConfiguration(string serviceName, bool isEnabled, string baseUrl, out Error error)
    {
        if (!isEnabled)
        {
            error = ApplicationErrors.ExternalAi.Disabled(serviceName);
            return false;
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            error = ApplicationErrors.ExternalAi.NotConfigured(serviceName);
            return false;
        }

        error = default;
        return true;
    }

    private static async Task<Result<JsonNode?>> ReadResponseAsync(HttpResponseMessage response, string serviceName, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            return ApplicationErrors.ExternalAi.Failed(serviceName, (int)response.StatusCode, body);
        }

        if (string.IsNullOrWhiteSpace(body))
            return (JsonNode?)null;

        return JsonNode.Parse(body);
    }

    private static string Truncate(string value)
    {
        return value.Length <= 300 ? value : value[..300];
    }
}

internal sealed class RecommendationApiClient(
    HttpClient httpClient,
    IOptions<ExternalAiServicesSettings> settings,
    ILogger<RecommendationApiClient> logger)
    : IRecommendationApiClient
{
    private readonly ExternalAiServicesSettings _settings = settings.Value;

    public Task<Result<JsonNode?>> GetStatusAsync(CancellationToken ct = default)
        => GetAsync("/", "Recommendation", ct);

    public Task<Result<JsonNode?>> RecommendAsync(int houseId, int topN, CancellationToken ct = default)
    {
        var safeTopN = Math.Clamp(topN, 1, 20);
        return GetAsync($"/recommend/{houseId}?n={safeTopN}", "Recommendation", ct);
    }

    private async Task<Result<JsonNode?>> GetAsync(string path, string serviceName, CancellationToken ct)
    {
        if (!TryValidateConfiguration(serviceName, _settings.RecommendationEnabled, _settings.RecommendationBaseUrl, out var error))
            return error;

        try
        {
            using var response = await httpClient.GetAsync(path, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
                return ApplicationErrors.ExternalAi.Failed(serviceName, (int)response.StatusCode, body);
        }

            if (string.IsNullOrWhiteSpace(body))
                return (JsonNode?)null;

            return JsonNode.Parse(body);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ServiceName} request failed.", serviceName);
            return ApplicationErrors.ExternalAi.Unavailable(serviceName);
        }
    }

    private static bool TryValidateConfiguration(string serviceName, bool isEnabled, string baseUrl, out Error error)
    {
        if (!isEnabled)
        {
            error = ApplicationErrors.ExternalAi.Disabled(serviceName);
            return false;
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            error = ApplicationErrors.ExternalAi.NotConfigured(serviceName);
            return false;
        }

        error = default;
        return true;
    }
}

internal sealed class VoiceRecognitionApiClient(
    HttpClient httpClient,
    IOptions<ExternalAiServicesSettings> settings,
    ILogger<VoiceRecognitionApiClient> logger)
    : IVoiceRecognitionApiClient
{
    private const string ServiceName = "VoiceRecognition";
    private const string DefaultAudioContentType = "audio/webm";
    private const string DefaultAudioFileName = "recording.webm";

    private readonly ExternalAiServicesSettings _settings = settings.Value;

    public Task<Result<JsonNode?>> GetStatusAsync(CancellationToken ct = default)
        => GetAsync("/voice-health", ct);

    public async Task<Result<JsonNode?>> VoiceChatAsync(
        string sessionId,
        Stream audioStream,
        string fileName,
        string? contentType,
        CancellationToken ct = default)
    {
        if (!TryValidateConfiguration(out var error))
            return error;

        try
        {
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(sessionId), "session_id");

            var audioContent = new StreamContent(audioStream);
            audioContent.Headers.ContentType = GetAudioContentType(contentType);
            form.Add(audioContent, "audio", GetSafeFileName(fileName));

            using var response = await httpClient.PostAsync("/voice-chat", form, ct);
            return await ReadResponseAsync(response, ServiceName, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ServiceName} request failed.", ServiceName);
            return ApplicationErrors.ExternalAi.Unavailable(ServiceName);
        }
    }

    private async Task<Result<JsonNode?>> GetAsync(string path, CancellationToken ct)
    {
        if (!TryValidateConfiguration(out var error))
            return error;

        try
        {
            using var response = await httpClient.GetAsync(path, ct);
            return await ReadResponseAsync(response, ServiceName, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ServiceName} request failed.", ServiceName);
            return ApplicationErrors.ExternalAi.Unavailable(ServiceName);
        }
    }

    private bool TryValidateConfiguration(out Error error)
    {
        if (!_settings.VoiceRecognitionEnabled)
        {
            error = ApplicationErrors.ExternalAi.Disabled(ServiceName);
            return false;
        }

        if (string.IsNullOrWhiteSpace(GetConfiguredBaseUrl()))
        {
            error = ApplicationErrors.ExternalAi.NotConfigured(ServiceName);
            return false;
        }

        error = default;
        return true;
    }

    private string GetConfiguredBaseUrl()
    {
        return string.IsNullOrWhiteSpace(_settings.VoiceRecognitionBaseUrl)
            ? _settings.ChatbotBaseUrl
            : _settings.VoiceRecognitionBaseUrl;
    }

    private static MediaTypeHeaderValue GetAudioContentType(string? contentType)
    {
        var normalizedContentType = string.IsNullOrWhiteSpace(contentType)
            ? DefaultAudioContentType
            : contentType.Trim();

        return MediaTypeHeaderValue.TryParse(normalizedContentType, out var mediaType)
            ? mediaType
            : MediaTypeHeaderValue.Parse(DefaultAudioContentType);
    }

    private static string GetSafeFileName(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);
        return string.IsNullOrWhiteSpace(safeFileName) ? DefaultAudioFileName : safeFileName;
    }

    private static async Task<Result<JsonNode?>> ReadResponseAsync(HttpResponseMessage response, string serviceName, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            return ApplicationErrors.ExternalAi.Failed(serviceName, (int)response.StatusCode, body);
        }

        if (string.IsNullOrWhiteSpace(body))
            return (JsonNode?)null;

        return JsonNode.Parse(body);
    }
}

internal sealed class ImageSearchApiClient(
    HttpClient httpClient,
    IOptions<ExternalAiServicesSettings> settings,
    IAppDbContext context,
    ILogger<ImageSearchApiClient> logger)
    : IImageSearchApiClient
{
    private const string ServiceName = "ImageSearch";
    private const string DefaultImageContentType = "image/jpeg";
    private const string DefaultImageFileName = "property-search.jpg";

    private readonly ExternalAiServicesSettings _settings = settings.Value;

    public Task<Result<JsonNode?>> GetStatusAsync(CancellationToken ct = default)
        => GetStatusCoreAsync(ct);

    public async Task<Result<JsonNode?>> SearchByImageAsync(
        Stream imageStream,
        string fileName,
        string? contentType,
        int topN,
        CancellationToken ct = default)
    {
        if (!TryValidateConfiguration(out var error))
            return error;

        try
        {
            using var buffer = new MemoryStream();
            await imageStream.CopyToAsync(buffer, ct);
            var imageBytes = buffer.ToArray();

            if (imageBytes.Length == 0)
                return Error.Validation("ImageSearch.ImageEmpty", "Image file is empty.");

            var dedicatedResult = await TrySearchDedicatedImageServiceAsync(
                imageBytes,
                fileName,
                contentType,
                topN,
                ct);

            if (dedicatedResult.EndpointFound)
                return dedicatedResult.Result;

            return await SearchCombinedChatbotImageEndpointAsync(
                imageBytes,
                fileName,
                contentType,
                topN,
                ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ServiceName} request failed.", ServiceName);
            return ApplicationErrors.ExternalAi.Unavailable(ServiceName);
        }
    }

    private async Task<Result<JsonNode?>> GetStatusCoreAsync(CancellationToken ct)
    {
        if (!TryValidateConfiguration(out var error))
            return error;

        try
        {
            using var healthResponse = await httpClient.GetAsync("/health", ct);

            if (healthResponse.StatusCode is not HttpStatusCode.NotFound and not HttpStatusCode.MethodNotAllowed)
                return await ReadResponseAsync(healthResponse, ServiceName, ct);

            using var rootResponse = await httpClient.GetAsync("/", ct);
            return await ReadResponseAsync(rootResponse, ServiceName, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "{ServiceName} status request failed.", ServiceName);
            return ApplicationErrors.ExternalAi.Unavailable(ServiceName);
        }
    }

    private async Task<(bool EndpointFound, Result<JsonNode?> Result)> TrySearchDedicatedImageServiceAsync(
        byte[] imageBytes,
        string fileName,
        string? contentType,
        int topN,
        CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(Math.Clamp(topN, 1, 50).ToString(CultureInfo.InvariantCulture)), "k");

        using var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = GetImageContentType(contentType);
        form.Add(imageContent, "image", GetSafeFileName(fileName));

        using var response = await httpClient.PostAsync("/api/v1/search-image", form, ct);

        if (response.StatusCode is HttpStatusCode.NotFound or HttpStatusCode.MethodNotAllowed)
            return (false, (JsonNode?)null);

        var body = await response.Content.ReadAsStringAsync(ct);
        if (!response.IsSuccessStatusCode)
            return (true, ApplicationErrors.ExternalAi.Failed(ServiceName, (int)response.StatusCode, body));

        if (string.IsNullOrWhiteSpace(body))
            return (true, BuildImageSearchResponse(
                imageBytes,
                contentType,
                "faiss_resnet50",
                [],
                "No image search matches were returned."));

        var matches = ParseDedicatedImageMatches(body);
        var properties = await EnrichImageMatchesAsync(matches, ct);

        return (true, BuildImageSearchResponse(
            imageBytes,
            contentType,
            "faiss_resnet50",
            properties,
            properties.Count == 0
                ? "No visually similar properties were found."
                : $"Found {properties.Count} visually similar properties."));
    }

    private async Task<Result<JsonNode?>> SearchCombinedChatbotImageEndpointAsync(
        byte[] imageBytes,
        string fileName,
        string? contentType,
        int topN,
        CancellationToken ct)
    {
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(Math.Clamp(topN, 1, 50).ToString(CultureInfo.InvariantCulture)), "top_n");

        using var imageContent = new ByteArrayContent(imageBytes);
        imageContent.Headers.ContentType = GetImageContentType(contentType);
        form.Add(imageContent, "image", GetSafeFileName(fileName));

        using var response = await httpClient.PostAsync("/image-search", form, ct);
        return await ReadResponseAsync(response, ServiceName, ct);
    }

    private async Task<List<JsonObject>> EnrichImageMatchesAsync(
        IReadOnlyList<DedicatedImageMatch> matches,
        CancellationToken ct)
    {
        if (matches.Count == 0)
            return [];

        var propertyIds = matches.Select(match => match.PropertyId).Distinct().ToList();
        var images = await context.PropertyImages
            .AsNoTracking()
            .Where(image => propertyIds.Contains(image.PropertyId))
            .OrderByDescending(image => image.IsPrimary)
            .ThenBy(image => image.SortOrder)
            .Select(image => new { image.PropertyId, image.Url })
            .ToListAsync(ct);

        var imageByPropertyId = images
            .GroupBy(image => image.PropertyId)
            .ToDictionary(group => group.Key, group => group.First().Url);

        var properties = await context.Properties
            .AsNoTracking()
            .Where(property => propertyIds.Contains(property.Id))
            .Select(property => new
            {
                property.Id,
                property.Title,
                property.Description,
                property.PropertyType,
                property.ListingType,
                property.Price,
                property.Area,
                property.Bedrooms,
                property.Bathrooms,
                property.City,
                property.District,
                property.SourceListingUrl,
                property.Status
            })
            .ToListAsync(ct);

        var propertyById = properties.ToDictionary(property => property.Id);
        var result = new List<JsonObject>();

        foreach (var match in matches)
        {
            if (!propertyById.TryGetValue(match.PropertyId, out var property))
                continue;

            imageByPropertyId.TryGetValue(property.Id, out var imageUrl);

            result.Add(new JsonObject
            {
                ["property_id"] = property.Id,
                ["propertyId"] = property.Id,
                ["title"] = property.Title,
                ["description"] = property.Description,
                ["type"] = property.PropertyType.ToString(),
                ["property_type"] = property.PropertyType.ToString(),
                ["listing_type"] = property.ListingType.ToString(),
                ["price"] = property.Price,
                ["area"] = property.Area,
                ["size_sqm"] = property.Area,
                ["bedrooms"] = property.Bedrooms,
                ["bathrooms"] = NormalizeRoomCount(property.Bathrooms),
                ["city"] = property.City,
                ["district"] = property.District,
                ["location"] = string.Join(", ", new[] { property.District, property.City }.Where(value => !string.IsNullOrWhiteSpace(value))),
                ["url"] = property.SourceListingUrl,
                ["image_url"] = imageUrl,
                ["status"] = property.Status.ToString(),
                ["visual_similarity_score"] = match.SimilarityScore,
                ["visual_similarity_engine"] = "faiss_resnet50"
            });
        }

        return result;
    }

    private static IReadOnlyList<DedicatedImageMatch> ParseDedicatedImageMatches(string body)
    {
        using var document = JsonDocument.Parse(body);
        if (!document.RootElement.TryGetProperty("matches", out var matchesElement) ||
            matchesElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var matches = new List<DedicatedImageMatch>();
        foreach (var item in matchesElement.EnumerateArray())
        {
            if (!item.TryGetProperty("property_id", out var propertyIdElement) ||
                !Guid.TryParse(propertyIdElement.GetString(), out var propertyId))
            {
                continue;
            }

            var score = 0f;
            if (item.TryGetProperty("similarity_score", out var scoreElement) &&
                scoreElement.TryGetSingle(out var parsedScore))
            {
                score = parsedScore;
            }

            matches.Add(new DedicatedImageMatch(propertyId, score));
        }

        return matches;
    }

    private static int NormalizeRoomCount(int value)
    {
        return value >= 10 && value % 10 == 0
            ? value / 10
            : value;
    }

    private static JsonNode BuildImageSearchResponse(
        byte[] imageBytes,
        string? contentType,
        string engine,
        IReadOnlyList<JsonObject> properties,
        string message)
    {
        var propertyArray = new JsonArray();
        foreach (var property in properties)
            propertyArray.Add(property);

        return new JsonObject
        {
            ["count"] = properties.Count,
            ["properties"] = propertyArray,
            ["message"] = message,
            ["engine"] = engine,
            ["query_image"] = new JsonObject
            {
                ["content_type"] = string.IsNullOrWhiteSpace(contentType) ? DefaultImageContentType : contentType,
                ["size_bytes"] = imageBytes.Length
            }
        };
    }

    private bool TryValidateConfiguration(out Error error)
    {
        if (!_settings.ImageSearchEnabled)
        {
            error = ApplicationErrors.ExternalAi.Disabled(ServiceName);
            return false;
        }

        if (string.IsNullOrWhiteSpace(GetConfiguredBaseUrl()))
        {
            error = ApplicationErrors.ExternalAi.NotConfigured(ServiceName);
            return false;
        }

        error = default;
        return true;
    }

    private string GetConfiguredBaseUrl()
    {
        return string.IsNullOrWhiteSpace(_settings.ImageSearchBaseUrl)
            ? _settings.ChatbotBaseUrl
            : _settings.ImageSearchBaseUrl;
    }

    private static MediaTypeHeaderValue GetImageContentType(string? contentType)
    {
        var normalizedContentType = string.IsNullOrWhiteSpace(contentType)
            ? DefaultImageContentType
            : contentType.Trim();

        return MediaTypeHeaderValue.TryParse(normalizedContentType, out var mediaType)
            ? mediaType
            : MediaTypeHeaderValue.Parse(DefaultImageContentType);
    }

    private static string GetSafeFileName(string fileName)
    {
        var safeFileName = Path.GetFileName(fileName);
        return string.IsNullOrWhiteSpace(safeFileName) ? DefaultImageFileName : safeFileName;
    }

    private static async Task<Result<JsonNode?>> ReadResponseAsync(HttpResponseMessage response, string serviceName, CancellationToken ct)
    {
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            return ApplicationErrors.ExternalAi.Failed(serviceName, (int)response.StatusCode, body);
        }

        if (string.IsNullOrWhiteSpace(body))
            return (JsonNode?)null;

        return JsonNode.Parse(body);
    }

    private sealed record DedicatedImageMatch(Guid PropertyId, float SimilarityScore);
}
