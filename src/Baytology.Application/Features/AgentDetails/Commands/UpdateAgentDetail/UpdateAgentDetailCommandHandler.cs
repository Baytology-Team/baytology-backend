using Baytology.Application.Common.Caching;
using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.AgentDetails.Commands.UpdateAgentDetail;

public class UpdateAgentDetailCommandHandler(IAppDbContext context)
    : IRequestHandler<UpdateAgentDetailCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(UpdateAgentDetailCommand request, CancellationToken ct)
    {
        var agent = await context.AgentDetails
            .FirstOrDefaultAsync(a => a.UserId == request.UserId, ct);

        if (agent is null)
            return ApplicationErrors.AgentDetails.NotFound;

        var updateResult = agent.Update(request.AgencyName, request.LicenseNumber, request.CommissionRate);
        if (updateResult.IsError)
            return updateResult.Errors;

        await context.SaveChangesAsync(ct);

        return Result.Success;
    }
}
