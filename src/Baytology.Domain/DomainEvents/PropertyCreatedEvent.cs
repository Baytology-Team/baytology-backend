using Baytology.Domain.Common;

namespace Baytology.Domain.DomainEvents;

public sealed class PropertyCreatedEvent(Guid propertyId) : DomainEvent
{
    public Guid PropertyId { get; } = propertyId;
}
