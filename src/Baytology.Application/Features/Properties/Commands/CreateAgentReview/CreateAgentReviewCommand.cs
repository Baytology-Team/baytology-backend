using Baytology.Application.Common.Caching;
using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Entities;
using Baytology.Domain.Common.Enums;
using Baytology.Domain.Common.Results;

using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.Properties.Commands.CreateAgentReview;

public record CreateAgentReviewCommand(
    string AgentUserId,
    string ReviewerUserId,
    Guid? PropertyId,
    int Rating,
    string? Comment) : IRequest<Result<Guid>>, ICacheInvalidationRequest
{
    public IEnumerable<string> CacheTagsToInvalidate =>
    [
        ApplicationCacheTags.AgentDetails,
        ApplicationCacheTags.AgentDetail(AgentUserId),
        ApplicationCacheTags.Properties
    ];
}
