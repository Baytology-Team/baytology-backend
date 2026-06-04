using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Results;
using Baytology.Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.Conversations.Commands.SendMessage;

public record SendMessageCommand(Guid ConversationId, string SenderId, string Content, string? AttachmentUrl)
    : IRequest<Result<Guid>>;
