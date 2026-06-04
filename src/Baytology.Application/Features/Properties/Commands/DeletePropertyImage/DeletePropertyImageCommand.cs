using Baytology.Application.Common.Caching;
using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Results;

using MediatR;

namespace Baytology.Application.Features.Properties.Commands.DeletePropertyImage;

public record DeletePropertyImageCommand(Guid PropertyId, Guid ImageId, string AgentUserId) : IRequest<Result<Success>>, ICacheInvalidationRequest
{
    public IEnumerable<string> CacheTagsToInvalidate =>
    [
        ApplicationCacheTags.Properties,
        ApplicationCacheTags.Property(PropertyId)
    ];
}
