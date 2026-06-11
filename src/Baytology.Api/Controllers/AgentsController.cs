using System.Security.Claims;

using Asp.Versioning;

using Baytology.Application.Features.AgentDetails.Commands.UpdateAgentDetail;
using Baytology.Application.Features.AgentDetails.Dtos;
using Baytology.Application.Features.AgentDetails.Queries.GetAgentDetail;
using Baytology.Application.Features.Agents.Queries.GetAgentProperties;
using Baytology.Application.Features.Agents.Queries.GetTopAgents;
using Baytology.Application.Features.Agents.Queries.SearchAgents;
using Baytology.Contracts.Requests.AgentDetails;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Baytology.Api.Controllers;

[ApiVersion("1")]
public class AgentsController(ISender sender) : ApiController
{
    [HttpGet("{agentUserId}")]
    [EndpointSummary("Get agent public details")]
    [ProducesResponseType(typeof(AgentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointDescription("Returns the public details for an agent, including rating and verification status.")]
    [EndpointName("GetAgentDetail")]
    [MapToApiVersion("1")]
    public async Task<IActionResult> GetAgentDetail(string agentUserId, CancellationToken ct)
    {
        var result = await sender.Send(new GetAgentDetailQuery(agentUserId), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("me")]
    [Authorize(Roles = "Agent,Admin")]
    [EndpointSummary("Get current agent details")]
    [ProducesResponseType(typeof(AgentDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointDescription("Returns the details for the current authenticated agent.")]
    [EndpointName("GetMyAgentDetail")]
    [MapToApiVersion("1")]
    public async Task<IActionResult> GetMyAgentDetail(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var result = await sender.Send(new GetAgentDetailQuery(userId), ct);
        return result.Match(Ok, Problem);
    }

    [HttpPut("me")]
    [Authorize(Roles = "Agent,Admin")]
    [EndpointSummary("Update the current agent details")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointDescription("Updates the authenticated agent's agency details and commission rate.")]
    [EndpointName("UpdateAgentDetail")]
    [MapToApiVersion("1")]
    public async Task<IActionResult> UpdateMyAgentDetail([FromBody] UpdateAgentDetailRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        var commandToSend = new UpdateAgentDetailCommand(
            userId,
            request.AgencyName,
            request.LicenseNumber,
            request.CommissionRate);
        var result = await sender.Send(commandToSend, ct);
        return result.Match(_ => Ok(), Problem);
    }

    [HttpGet("top")]
    [EndpointSummary("Get top rated agents")]
    [ProducesResponseType(typeof(List<TopAgentDto>), StatusCodes.Status200OK)]
    [EndpointDescription("Returns the top rated agents sorted by rating and review count.")]
    [EndpointName("GetTopAgents")]
    [MapToApiVersion("1")]
    public async Task<IActionResult> GetTopAgents(CancellationToken ct, [FromQuery] int limit = 20)
    {
        var result = await sender.Send(new GetTopAgentsQuery(limit), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("search")]
    [EndpointSummary("Search for agents")]
    [ProducesResponseType(typeof(List<TopAgentDto>), StatusCodes.Status200OK)]
    [EndpointDescription("Searches for agents by name or agency name.")]
    [EndpointName("SearchAgents")]
    [MapToApiVersion("1")]
    public async Task<IActionResult> SearchAgents([FromQuery] string searchTerm, CancellationToken ct, [FromQuery] int limit = 20)
    {
        var result = await sender.Send(new SearchAgentsQuery(searchTerm, limit), ct);
        return result.Match(Ok, Problem);
    }

    [HttpGet("{agentUserId}/properties")]
    [EndpointSummary("Get agent properties with ratings")]
    [ProducesResponseType(typeof(List<AgentPropertyDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointDescription("Returns all properties for a specific agent with their ratings.")]
    [EndpointName("GetAgentProperties")]
    [MapToApiVersion("1")]
    public async Task<IActionResult> GetAgentProperties(string agentUserId, CancellationToken ct)
    {
        var result = await sender.Send(new GetAgentPropertiesQuery(agentUserId), ct);
        return result.Match(Ok, Problem);
    }
}
