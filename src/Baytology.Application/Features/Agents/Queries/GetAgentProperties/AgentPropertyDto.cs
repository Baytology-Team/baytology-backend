using Baytology.Domain.Common.Enums;

namespace Baytology.Application.Features.Agents.Queries.GetAgentProperties;

public record AgentPropertyDto(
    Guid Id,
    string Title,
    string? Description,
    decimal Price,
    decimal Area,
    int Bedrooms,
    int Bathrooms,
    string City,
    string? District,
    string? ImageUrl,
    decimal? AverageRating,
    int ReviewCount,
    PropertyStatus Status);
