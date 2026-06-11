using Baytology.Application.Features.Agents.Queries.GetTopAgents;
using Baytology.Domain.Common.Results;
using MediatR;

namespace Baytology.Application.Features.Agents.Queries.SearchAgents;

public record SearchAgentsQuery(string SearchTerm, int Limit = 20) : IRequest<Result<List<TopAgentDto>>>;
