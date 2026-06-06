using Baytology.Domain.Common;
using Baytology.Domain.Exceptions;
using Baytology.Domain.Common.Enums;
using Baytology.Domain.Common.Results;

namespace Baytology.Domain.Entities;

public sealed class Booking : AuditableEntity
{
    public Guid PropertyId { get; private set; }
    public string UserId { get; private set; } = null!;
    public string AgentUserId { get; private set; } = null!;
    public Guid? PaymentId { get; private set; }
    public DateTimeOffset StartDate { get; private set; }
    public DateTimeOffset EndDate { get; private set; }
    public BookingStatus Status { get; private set; }
    public string? Notes { get; private set; }

    private Booking() { }

    private Booking(
        Guid id,
        Guid propertyId,
        string userId,
        string agentUserId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        string? notes) : base(id)
    {
        PropertyId = propertyId;
        UserId = userId;
        AgentUserId = agentUserId;
        StartDate = startDate;
        EndDate = endDate;
        Status = BookingStatus.Pending;
        Notes = notes;
    }

    public static Result<Booking> Create(
        Guid propertyId,
        string userId,
        string agentUserId,
        DateTimeOffset startDate,
        DateTimeOffset endDate,
        string? notes = null)
    {
        if (propertyId == Guid.Empty)
            return BookingErrors.PropertyRequired;

        if (string.IsNullOrWhiteSpace(userId))
            return BookingErrors.UserRequired;

        if (string.IsNullOrWhiteSpace(agentUserId))
            return BookingErrors.AgentRequired;

        if (endDate <= startDate)
            return BookingErrors.DateRangeInvalid;

        if (startDate < DateTimeOffset.UtcNow)
            return BookingErrors.StartDateInvalid;

        var maxEndDate = DateTimeOffset.UtcNow.AddYears(1);
        if (endDate > maxEndDate)
            return Error.Validation("Booking_EndDateTooFar", "Booking end date cannot be more than 1 year in the future.");

        if (notes is not null && notes.Length > 1000)
            return Error.Validation("Booking_NotesTooLong", "Notes cannot exceed 1000 characters.");

        return new Booking(
            Guid.NewGuid(),
            propertyId,
            userId.Trim(),
            agentUserId.Trim(),
            startDate,
            endDate,
            notes?.Trim());
    }

    public void AttachPayment(Guid paymentId)
    {
        if (paymentId == Guid.Empty)
            return;

        PaymentId = paymentId;
    }

    public void Confirm() => Status = BookingStatus.Confirmed;

    public void Cancel() => Status = BookingStatus.Cancelled;
}
