using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Enums;
using Baytology.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.Agents.Queries.GetAgentProperties;

public class GetAgentPropertiesQueryHandler(IAppDbContext context)
    : IRequestHandler<GetAgentPropertiesQuery, Result<List<AgentPropertyDto>>>
{
    public async Task<Result<List<AgentPropertyDto>>> Handle(GetAgentPropertiesQuery request, CancellationToken ct)
    {
        var properties = await context.Properties
            .AsNoTracking()
            .Where(p => p.AgentUserId == request.AgentUserId)
            .GroupJoin(context.AgentReviews,
                prop => prop.Id,
                review => review.PropertyId,
                (prop, reviews) => new { prop, reviews })
            .Select(x => new AgentPropertyDto(
                x.prop.Id,
                x.prop.Title,
                x.prop.Description,
                x.prop.Price,
                x.prop.Area,
                x.prop.Bedrooms,
                x.prop.Bathrooms,
                x.prop.City ?? string.Empty,
                x.prop.District,
                x.prop.Images.FirstOrDefault() != null ? x.prop.Images.First().Url : null,
                x.reviews.Any() ? (decimal?)x.reviews.Average(r => r.Rating) : null,
                x.reviews.Count(),
                x.prop.Status))
            .ToListAsync(ct);

        return properties;
    }
}
