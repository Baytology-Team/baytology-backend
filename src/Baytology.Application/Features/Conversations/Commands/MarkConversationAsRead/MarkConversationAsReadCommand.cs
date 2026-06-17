using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.Conversations.Commands.MarkConversationAsRead;

public record MarkConversationAsReadCommand(Guid ConversationId, string UserId) : IRequest<Result<int>>;
