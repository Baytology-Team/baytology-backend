using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Results;
using MediatR;

namespace Baytology.Application.Features.Properties.Commands.SetPropertyImageAsPrimary;

public class SetPropertyImageAsPrimaryCommandHandler(IAppDbContext context)
    : IRequestHandler<SetPropertyImageAsPrimaryCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(SetPropertyImageAsPrimaryCommand request, CancellationToken ct)
    {
        var property = await context.Properties.FindAsync([request.PropertyId], ct);
        if (property is null)
            return ApplicationErrors.Property.NotFound;

        if (property.AgentUserId != request.AgentUserId.ToString())
            return ApplicationErrors.Property.AccessDenied;

        var result = property.SetPrimaryImage(request.ImageId);
        if (result.IsError)
            return result;

        await context.SaveChangesAsync(ct);
        return Result.Success;
    }
}
