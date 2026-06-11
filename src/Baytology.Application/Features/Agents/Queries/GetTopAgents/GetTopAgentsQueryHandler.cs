using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.Agents.Queries.GetTopAgents;

public class GetTopAgentsQueryHandler(IAppDbContext context)
    : IRequestHandler<GetTopAgentsQuery, Result<List<TopAgentDto>>>
{
    public async Task<Result<List<TopAgentDto>>> Handle(GetTopAgentsQuery request, CancellationToken ct)
    {
        var topAgents = await context.AgentDetails
            .AsNoTracking()
            .Where(a => a.IsVerified)
            .OrderByDescending(a => a.Rating)
            .ThenByDescending(a => a.ReviewCount)
            .Take(request.Limit)
            .Join(context.UserProfiles,
                agent => agent.UserId,
                profile => profile.UserId,
                (agent, profile) => new { agent, profile })
            .GroupJoin(context.Properties,
                au => au.agent.UserId,
                prop => prop.AgentUserId,
                (au, props) => new { au.agent, au.profile, PropertyCount = props.Count() })
            .Select(x => new TopAgentDto(
                x.agent.Id,
                x.agent.UserId,
                x.profile.DisplayName,
                x.profile.AvatarUrl,
                x.agent.AgencyName,
                x.agent.Rating,
                x.agent.ReviewCount,
                x.PropertyCount,
                x.agent.IsVerified))
            .ToListAsync(ct);

        return topAgents;
    }
}
