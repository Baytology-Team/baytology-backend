using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Results;
using Baytology.Domain.Entities;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.Conversations.Commands.CreateConversation;

public record CreateConversationCommand(Guid PropertyId, string BuyerUserId, string? AgentUserId = null)
    : IRequest<Result<Guid>>;
