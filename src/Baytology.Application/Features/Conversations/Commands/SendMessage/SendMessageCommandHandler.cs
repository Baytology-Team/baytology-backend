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
        {
            // Auto-create conversation if PropertyId and AgentUserId are provided
            if (request.PropertyId.HasValue && !string.IsNullOrWhiteSpace(request.AgentUserId))
            {
                var property = await context.Properties
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.Id == request.PropertyId.Value, ct);

                if (property is null)
                    return Domain.Exceptions.PropertyErrors.NotFound;

                var agentUserId = request.AgentUserId;
                var buyerUserId = request.SenderId;

                // Check if agent profile exists
                var agentProfileExists = await context.AgentDetails
                    .AsNoTracking()
                    .AnyAsync(a => a.UserId == agentUserId, ct);

                if (!agentProfileExists)
                    return ApplicationErrors.Conversation.AgentUnavailable;

                // Check if conversation already exists
                var existingConversationId = await context.Conversations
                    .Where(c =>
                        c.PropertyId == request.PropertyId.Value &&
                        c.BuyerUserId == buyerUserId &&
                        c.AgentUserId == agentUserId)
                    .Select(c => (Guid?)c.Id)
                    .FirstOrDefaultAsync(ct);

                if (existingConversationId.HasValue)
                {
                    conversation = await context.Conversations.FindAsync([existingConversationId.Value], ct);
                    if (conversation is null)
                        return Domain.Exceptions.ConversationErrors.NotFound;
                }
                else
                {
                    var conversationResult = Conversation.Create(request.PropertyId.Value, buyerUserId, agentUserId);
                    if (conversationResult.IsError)
                        return conversationResult.Errors;

                    conversation = conversationResult.Value;
                    context.Conversations.Add(conversation);
                    await context.SaveChangesAsync(ct);
                }
            }
            else
            {
                return Domain.Exceptions.ConversationErrors.NotFound;
            }
        }

        var messageResult = conversation.SendMessage(request.SenderId, request.Content, request.AttachmentUrl);
        if (messageResult.IsError)
            return messageResult.Errors;

        var message = messageResult.Value;
        context.Messages.Add(message);
        await context.SaveChangesAsync(ct);

        return message.Id;
    }
}
