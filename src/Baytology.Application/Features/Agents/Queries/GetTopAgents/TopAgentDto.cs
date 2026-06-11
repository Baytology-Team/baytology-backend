namespace Baytology.Application.Features.Agents.Queries.GetTopAgents;

public record TopAgentDto(
    Guid Id,
    string UserId,
    string? DisplayName,
    string? AvatarUrl,
    string? AgencyName,
    decimal Rating,
    int ReviewCount,
    int PropertyCount,
    bool IsVerified);
