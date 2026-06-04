using Baytology.Application.Features.Availability.Dtos;
using Baytology.Domain.Common.Results;
using MediatR;

namespace Baytology.Application.Features.Availability.Queries.GetAgentAvailabilityRules;

public record GetAgentAvailabilityRulesQuery(string AgentUserId) : IRequest<Result<List<AvailabilityRuleDto>>>;
