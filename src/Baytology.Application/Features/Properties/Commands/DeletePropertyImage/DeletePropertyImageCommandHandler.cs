using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Results;
using Baytology.Domain.Exceptions;

using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.Properties.Commands.DeletePropertyImage;

public class DeletePropertyImageCommandHandler(IAppDbContext context)
    : IRequestHandler<DeletePropertyImageCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(DeletePropertyImageCommand request, CancellationToken ct)
    {
        var property = await context.Properties
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.PropertyId, ct);

        if (property is null)
            return PropertyErrors.NotFound;

        if (property.AgentUserId != request.AgentUserId)
            return Error.Forbidden("Property_Delete_Forbidden", "You can only delete images for your own properties.");

        var removeResult = property.RemoveImage(request.ImageId);
        if (removeResult.IsError)
            return removeResult.Errors;

        await context.SaveChangesAsync(ct);

        return Result.Success;
    }
}
