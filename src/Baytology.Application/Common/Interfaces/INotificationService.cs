using Baytology.Domain.Entities;

namespace Baytology.Application.Common.Interfaces;

public interface INotificationService
{
    Task SendAsync(Notification notification, CancellationToken ct = default);
}
