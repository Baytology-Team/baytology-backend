using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.Conversations.Commands.MarkConversationAsRead;

public class MarkConversationAsReadCommandHandler(IAppDbContext context)
    : IRequestHandler<MarkConversationAsReadCommand, Result<int>>
{
    public async Task<Result<int>> Handle(MarkConversationAsReadCommand request, CancellationToken ct)
    {
        var conversation = await context.Conversations
            .FirstOrDefaultAsync(c => c.Id == request.ConversationId, ct);

        if (conversation is null ||
            (conversation.BuyerUserId != request.UserId && conversation.AgentUserId != request.UserId))
        {
            return ApplicationErrors.Conversation.MessageNotFound;
        }

        var unreadMessages = await context.Messages
            .Where(m => m.ConversationId == request.ConversationId && !m.IsRead && m.SenderId != request.UserId)
            .ToListAsync(ct);

        int markedCount = 0;
        foreach (var message in unreadMessages)
        {
            if (message.MarkAsRead())
                markedCount++;
        }

        if (markedCount > 0)
            await context.SaveChangesAsync(ct);

        return markedCount;
    }
}
