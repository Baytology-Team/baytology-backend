namespace Baytology.Application.Features.Bookings.Dtos;

public record BookingDto(
    Guid Id,
    Guid PropertyId,
    string PropertyTitle,
    string UserId,
    string AgentUserId,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    string Status,
    decimal Amount,
    decimal Commission,
    decimal NetAmount,
    string Currency,
    decimal CommissionRate,
    Guid? PaymentId,
    DateTimeOffset CreatedOnUtc,
    string? Notes,
    string? BuyerName);
