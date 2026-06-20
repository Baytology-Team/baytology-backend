using System.Security.Claims;

using Asp.Versioning;

using Baytology.Application.Features.PropertyPreferences.Commands.SetPropertyPreference;
using Baytology.Application.Features.PropertyPreferences.Queries.GetPropertyPreference;
using Baytology.Contracts.Requests.PropertyPreferences;
using Baytology.Contracts.Responses.PropertyPreferences;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baytology.Api.Controllers;

[ApiVersion("1")]
[Authorize]
public class PropertyPreferencesController(ISender sender) : ApiController
{
    [HttpPost]
    [EndpointSummary("Set or update user property preferences")]
    [ProducesResponseType(typeof(SetPropertyPreferenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointDescription("Sets or updates the authenticated user's property preferences. When a new property matching these preferences is created, the user will receive a notification.")]
    [EndpointName("SetPropertyPreference")]
    [MapToApiVersion("1")]
    public async Task<IActionResult> SetPropertyPreference([FromBody] SetPropertyPreferenceRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var commandToSend = new SetPropertyPreferenceCommand(
            userId,
            request.PropertyType is null ? null : (Baytology.Domain.Common.Enums.PropertyType)request.PropertyType,
            request.ListingType is null ? null : (Baytology.Domain.Common.Enums.ListingType)request.ListingType,
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
            request.FurnishingStatus is null ? null : (Baytology.Domain.Common.Enums.FurnishingStatus)request.FurnishingStatus,
            request.ViewType is null ? null : (Baytology.Domain.Common.Enums.ViewType)request.ViewType);
        var result = await sender.Send(commandToSend, ct);
        return result.Match(id => Ok(new SetPropertyPreferenceResponse(id)), Problem);
    }

    [HttpGet]
    [EndpointSummary("Get user property preferences")]
    [ProducesResponseType(typeof(GetPropertyPreferenceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    [EndpointDescription("Returns the authenticated user's current property preferences.")]
    [EndpointName("GetPropertyPreference")]
    [MapToApiVersion("1")]
    public async Task<IActionResult> GetPropertyPreference(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var query = new GetPropertyPreferenceQuery(userId);
        var result = await sender.Send(query, ct);
        return result.Match(preference => Ok(new GetPropertyPreferenceResponse(
            preference.Id,
            preference.PropertyType is null ? null : (Baytology.Contracts.Common.PropertyType)preference.PropertyType,
            preference.ListingType is null ? null : (Baytology.Contracts.Common.ListingType)preference.ListingType,
            preference.MinPrice,
            preference.MaxPrice,
            preference.MinArea,
            preference.MaxArea,
            preference.MinBedrooms,
            preference.MaxBedrooms,
            preference.MinBathrooms,
            preference.MaxBathrooms,
            preference.City,
            preference.District,
            preference.HasParking,
            preference.HasPool,
            preference.HasGym,
            preference.HasElevator,
            preference.HasSecurity,
            preference.HasBalcony,
            preference.HasGarden,
            preference.HasCentralAC,
            preference.FurnishingStatus is null ? null : (Baytology.Contracts.Common.FurnishingStatus)preference.FurnishingStatus,
            preference.ViewType is null ? null : (Baytology.Contracts.Common.ViewType)preference.ViewType,
            preference.IsActive,
            preference.CreatedOnUtc,
            preference.UpdatedOnUtc)), Problem);
    }
}
