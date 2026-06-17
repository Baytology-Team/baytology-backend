using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Enums;
using Baytology.Domain.DomainEvents;
using Baytology.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.Properties.EventHandlers;

public class PropertyCreatedEventHandler(
    IAppDbContext context,
    INotificationService notificationService) : INotificationHandler<PropertyCreatedEvent>
{
    public async Task Handle(PropertyCreatedEvent notification, CancellationToken ct)
    {
        // Get the newly created property with its amenities
        var property = await context.Properties
            .Include(p => p.Amenity)
            .FirstOrDefaultAsync(p => p.Id == notification.PropertyId, ct);

        if (property is null)
            return;

        // Get all active user preferences
        var preferences = await context.UserPropertyPreferences
            .Where(p => p.IsActive)
            .ToListAsync(ct);

        // Find matching preferences
        var matchedPreferences = preferences
            .Where(p => p.MatchesProperty(property, property.Amenity))
            .ToList();

        // Send notifications to users with matching preferences
        foreach (var preference in matchedPreferences)
        {
            // Don't notify the agent who created the property
            if (preference.UserId == property.AgentUserId)
                continue;

            var notificationResult = Notification.Create(
                preference.UserId,
                NotificationType.PropertyMatch,
                "عقار جديد يطابق تفضيلاتك",
                $"تم إضافة عقار جديد يطابق تفضيلاتك: {property.Title} في {property.City ?? property.District ?? "موقع غير محدد"} بسعر {property.Price:N0} جنيه",
                property.Id.ToString(),
                ReferenceType.Property);

            if (notificationResult.IsError)
                continue;

            await notificationService.SendAsync(notificationResult.Value, ct);
        }
    }
}
