using System.Text.Json;
using System.Text.Json.Nodes;

using Asp.Versioning;

using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Results;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baytology.Api.Controllers;

[ApiVersion("1")]
[AllowAnonymous]
public sealed class AiAssistantController(
    IChatbotApiClient chatbotApiClient,
    IRecommendationApiClient recommendationApiClient,
    IVoiceRecognitionApiClient voiceRecognitionApiClient,
    IImageSearchApiClient imageSearchApiClient) : ApiController
{
    private const long MaxVoiceUploadBytes = 25 * 1024 * 1024;
    private const long MaxImageUploadBytes = 15 * 1024 * 1024;

    [HttpGet("status")]
    [EndpointSummary("Get external AI integrations status")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        var chatbotStatusTask = chatbotApiClient.GetStatusAsync(ct);
        var recommendationStatusTask = recommendationApiClient.GetStatusAsync(ct);
        var voiceRecognitionStatusTask = voiceRecognitionApiClient.GetStatusAsync(ct);
        var imageSearchStatusTask = imageSearchApiClient.GetStatusAsync(ct);

        await Task.WhenAll(chatbotStatusTask, recommendationStatusTask, voiceRecognitionStatusTask, imageSearchStatusTask);

        var chatbotStatus = await chatbotStatusTask;
        var recommendationStatus = await recommendationStatusTask;
        var voiceRecognitionStatus = await voiceRecognitionStatusTask;
        var imageSearchStatus = await imageSearchStatusTask;

        var payload = new JsonObject
        {
            ["overallStatus"] = chatbotStatus.IsSuccess && recommendationStatus.IsSuccess && voiceRecognitionStatus.IsSuccess && imageSearchStatus.IsSuccess ? "Online" : "Degraded",
            ["chatbot"] = BuildStatusNode(chatbotStatus),
            ["recommendation"] = BuildStatusNode(recommendationStatus),
            ["voiceRecognition"] = BuildStatusNode(voiceRecognitionStatus),
            ["imageSearch"] = BuildStatusNode(imageSearchStatus)
        };

        return Ok(payload);
    }

    [HttpPost("parse")]
    [EndpointSummary("Proxy parse request to the external chatbot API")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    public async Task<IActionResult> Parse([FromBody] JsonElement request, CancellationToken ct)
    {
        var result = await chatbotApiClient.ParseAsync(request, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("question")]
    [EndpointSummary("Proxy follow-up question request to the external chatbot API")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    public async Task<IActionResult> Question([FromBody] JsonElement request, CancellationToken ct)
    {
        var result = await chatbotApiClient.AskQuestionAsync(request, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("search")]
    [EndpointSummary("Proxy property search request to the external chatbot API")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    public async Task<IActionResult> Search([FromBody] JsonElement request, CancellationToken ct)
    {
        var result = await chatbotApiClient.SearchAsync(request, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("rank")]
    [EndpointSummary("Proxy ranking request to the external chatbot API")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    public async Task<IActionResult> Rank([FromBody] JsonElement request, CancellationToken ct)
    {
        var result = await chatbotApiClient.RankAsync(request, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("chat")]
    [EndpointSummary("Proxy chat request to the external chatbot API")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    public async Task<IActionResult> Chat([FromBody] JsonElement request, CancellationToken ct)
    {
        var result = await chatbotApiClient.ChatAsync(request, ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("recommend/{houseId:int}")]
    [EndpointSummary("Proxy recommendation request to the external recommendation API")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [MapToApiVersion("1")]
    public async Task<IActionResult> Recommend(int houseId, [FromQuery] int topN = 5, CancellationToken ct = default)
    {
        var result = await recommendationApiClient.RecommendAsync(houseId, topN, ct);
        return result.Match(Ok, Problem);
    }

    [HttpPost("voice-chat")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxVoiceUploadBytes)]
    [EndpointSummary("Proxy voice chat request to the external voice recognition API")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [MapToApiVersion("1")]
    public async Task<IActionResult> VoiceChat(
        [FromForm(Name = "session_id")] string sessionId,
        [FromForm(Name = "audio")] IFormFile? audio,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Problem(new List<Error>
            {
                Error.Validation("VoiceRecognition.SessionIdRequired", "session_id is required.")
            });
        }

        if (audio is null || audio.Length == 0)
        {
            return Problem(new List<Error>
            {
                Error.Validation("VoiceRecognition.AudioRequired", "Audio file is required.")
            });
        }

        if (audio.Length > MaxVoiceUploadBytes)
        {
            return Problem(
                statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "VoiceRecognition.AudioTooLarge",
                detail: $"Audio file exceeds the {MaxVoiceUploadBytes / 1024 / 1024} MB upload limit.");
        }

        await using var audioStream = audio.OpenReadStream();
        var result = await voiceRecognitionApiClient.VoiceChatAsync(
            sessionId.Trim(),
            audioStream,
            audio.FileName,
            audio.ContentType,
            ct);

        return result.Match(Ok, Problem);
    }

    [HttpPost("image-search")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxImageUploadBytes)]
    [EndpointSummary("Proxy image search request to the external image search API")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [MapToApiVersion("1")]
    public async Task<IActionResult> ImageSearch(
        [FromForm(Name = "image")] IFormFile? image,
        [FromForm(Name = "top_n")] int topN = 10,
        CancellationToken ct = default)
    {
        if (image is null || image.Length == 0)
        {
            return Problem(new List<Error>
            {
                Error.Validation("ImageSearch.ImageRequired", "Image file is required.")
            });
        }

        if (image.Length > MaxImageUploadBytes)
        {
            return Problem(
                statusCode: StatusCodes.Status413PayloadTooLarge,
                title: "ImageSearch.ImageTooLarge",
                detail: $"Image file exceeds the {MaxImageUploadBytes / 1024 / 1024} MB upload limit.");
        }

        await using var imageStream = image.OpenReadStream();
        var result = await imageSearchApiClient.SearchByImageAsync(
            imageStream,
            image.FileName,
            image.ContentType,
            topN,
            ct);

        return result.Match(Ok, Problem);
    }

    private static JsonNode BuildStatusNode(Baytology.Domain.Common.Results.Result<JsonNode?> result)
    {
        if (result.IsSuccess)
        {
            return result.Value?.DeepClone() ?? new JsonObject
            {
                ["available"] = true
            };
        }

        return new JsonObject
        {
            ["available"] = false,
            ["errorCode"] = result.TopError.Code,
            ["error"] = result.TopError.Description
        };
    }
}
