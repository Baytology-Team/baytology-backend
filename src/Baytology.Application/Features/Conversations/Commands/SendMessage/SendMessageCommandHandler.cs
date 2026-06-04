using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Results;
using Baytology.Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.Conversations.Commands.SendMessage;

public class SendMessageCommandHandler(IAppDbContext context)
    : IRequestHandler<SendMessageCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(SendMessageCommand request, CancellationToken ct)
    {
        var conversation = await context.Conversations.FindAsync([request.ConversationId], ct);

        if (conversation is null)
            return Domain.Exceptions.ConversationErrors.NotFound;

        var messageResult = conversation.SendMessage(request.SenderId, request.Content, request.AttachmentUrl);
        if (messageResult.IsError)
            return messageResult.Errors;

        var message = messageResult.Value;
        context.Messages.Add(message);
        await context.SaveChangesAsync(ct);

        return message.Id;
    }
}
