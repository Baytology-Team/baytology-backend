using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Results;
using Baytology.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.Availability.Commands.DeleteAvailabilityRule;

public class DeleteAvailabilityRuleCommandHandler(IAppDbContext context)
    : IRequestHandler<DeleteAvailabilityRuleCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(DeleteAvailabilityRuleCommand request, CancellationToken ct)
    {
        var rule = await context.AvailabilityRules
            .FirstOrDefaultAsync(r => r.Id == request.Id, ct);

        if (rule is null)
            return AvailabilityRuleErrors.NotFound;

        if (rule.AgentUserId != request.AgentUserId)
            return Error.Forbidden("AvailabilityRule_Delete_Forbidden", "You can only delete your own availability rules.");

        context.AvailabilityRules.Remove(rule);
        await context.SaveChangesAsync(ct);

        return Result.Success;
    }
}
