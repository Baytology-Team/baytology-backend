namespace Baytology.Contracts.Requests.Conversations;

public sealed record CreateConversationRequest(Guid PropertyId, string? BuyerUserId = null, string? AgentUserId = null);
