using Baytology.Domain.Common;

namespace Baytology.Domain.DomainEvents;

public sealed class PropertyViewedEvent(Guid propertyId, string? userId) : DomainEvent
{
    public Guid PropertyId { get; } = propertyId;
    public string? UserId { get; } = userId;
}
