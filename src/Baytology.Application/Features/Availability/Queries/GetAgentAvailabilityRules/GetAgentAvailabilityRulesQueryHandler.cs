using Baytology.Application.Common.Interfaces;
using Baytology.Application.Features.Availability.Dtos;
using Baytology.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.Availability.Queries.GetAgentAvailabilityRules;

public class GetAgentAvailabilityRulesQueryHandler(IAppDbContext context)
    : IRequestHandler<GetAgentAvailabilityRulesQuery, Result<List<AvailabilityRuleDto>>>
{
    public async Task<Result<List<AvailabilityRuleDto>>> Handle(GetAgentAvailabilityRulesQuery request, CancellationToken ct)
    {
        var rules = await context.AvailabilityRules
            .Where(r => r.AgentUserId == request.AgentUserId)
            .OrderBy(r => r.PropertyId)
            .ThenBy(r => r.RecurrenceType)
            .Select(r => new AvailabilityRuleDto(
                r.Id,
                r.PropertyId,
                r.RecurrenceType,
                r.DayOfWeek,
                r.SpecificDate,
                r.StartTime,
                r.EndTime,
                r.SlotDuration))
            .ToListAsync(ct);

        return rules;
    }
}
