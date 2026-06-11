using System.Security.Claims;
using Asp.Versioning;
using Baytology.Application.Features.Availability.Commands.CreateAvailabilityRule;
using Baytology.Application.Features.Availability.Commands.DeleteAvailabilityRule;
using Baytology.Application.Features.Availability.Dtos;
using Baytology.Application.Features.Availability.Queries.GetAgentAvailability;
using Baytology.Application.Features.Availability.Queries.GetAgentAvailabilityRules;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baytology.Api.Controllers;

[ApiVersion("1")]
[Authorize(Roles = "Agent")]
public class AvailabilityController(ISender sender) : ApiController
{
    [HttpPost("rules")]
    [EndpointSummary("Create an availability rule")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateRule([FromBody] CreateAvailabilityRuleCommand request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (request.AgentUserId != userId)
            return Forbid();

        var result = await sender.Send(request, ct);
        return result.Match(id => Created(string.Empty, new { Id = id }), Problem);
    }

    [HttpDelete("rules/{id:guid}")]
    [EndpointSummary("Delete an availability rule")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteRule(Guid id, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var command = new DeleteAvailabilityRuleCommand(id, userId);
        var result = await sender.Send(command, ct);
        return result.Match(_ => Ok(), Problem);
    }

    [HttpGet("rules")]
    [EndpointSummary("Get all availability rules for the current agent")]
    [ProducesResponseType(typeof(List<AvailabilityRuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRules(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var query = new GetAgentAvailabilityRulesQuery(userId);
        var result = await sender.Send(query, ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("slots")]
    [EndpointSummary("Get available time slots for the agent")]
    [ProducesResponseType(typeof(List<Baytology.Application.Features.Availability.Queries.GetAgentAvailability.TimeSlotDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAvailability([FromQuery] DateTimeOffset startDate, [FromQuery] DateTimeOffset endDate, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var query = new GetAgentAvailabilityQuery(userId, startDate, endDate);
        var result = await sender.Send(query, ct);
        return result.Match(Ok, Problem);
    }
}
