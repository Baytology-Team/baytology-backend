using Baytology.Domain.Common.Results;
using MediatR;

namespace Baytology.Application.Features.Agents.Queries.GetAgentProperties;

public record GetAgentPropertiesQuery(string AgentUserId) : IRequest<Result<List<AgentPropertyDto>>>;
