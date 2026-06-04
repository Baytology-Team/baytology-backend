using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Results;
using Baytology.Domain.Entities;
using MediatR;

namespace Baytology.Application.Features.Availability.Commands.CreateAvailabilityRule;

public class CreateAvailabilityRuleCommandHandler(IAppDbContext context)
    : IRequestHandler<CreateAvailabilityRuleCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateAvailabilityRuleCommand request, CancellationToken ct)
    {
        var ruleResult = AvailabilityRule.Create(
            request.AgentUserId,
            request.PropertyId,
            request.RecurrenceType,
            request.DayOfWeek,
            request.SpecificDate,
            request.StartTime,
            request.EndTime,
            request.SlotDuration);

        if (ruleResult.IsError)
            return ruleResult.Errors;

        context.AvailabilityRules.Add(ruleResult.Value);
        await context.SaveChangesAsync(ct);

        return ruleResult.Value.Id;
    }
}
