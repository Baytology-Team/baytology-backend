using Baytology.Application.Common.Interfaces;
using Baytology.Application.Features.Agents.Queries.GetTopAgents;
using Baytology.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.Agents.Queries.SearchAgents;

public class SearchAgentsQueryHandler(IAppDbContext context)
    : IRequestHandler<SearchAgentsQuery, Result<List<TopAgentDto>>>
{
    public async Task<Result<List<TopAgentDto>>> Handle(SearchAgentsQuery request, CancellationToken ct)
    {
        var searchTerm = request.SearchTerm.ToLower();

        var agents = await context.AgentDetails
            .AsNoTracking()
            .Where(a => a.IsVerified)
            .Join(context.UserProfiles,
                agent => agent.UserId,
                profile => profile.UserId,
                (agent, profile) => new { agent, profile })
            .Where(au => au.agent.AgencyName != null && au.agent.AgencyName.ToLower().Contains(searchTerm) ||
                       au.profile.DisplayName != null && au.profile.DisplayName.ToLower().Contains(searchTerm))
            .GroupJoin(context.Properties,
                au => au.agent.UserId,
                prop => prop.AgentUserId,
                (au, props) => new { au.agent, au.profile, PropertyCount = props.Count() })
            .OrderByDescending(x => x.agent.Rating)
            .ThenByDescending(x => x.agent.ReviewCount)
            .Take(request.Limit)
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

        return agents;
    }
}
