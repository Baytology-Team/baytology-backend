using Baytology.Domain.Common.Results;
using MediatR;

namespace Baytology.Application.Features.Agents.Queries.GetTopAgents;

public record GetTopAgentsQuery(int Limit = 20) : IRequest<Result<List<TopAgentDto>>>;
