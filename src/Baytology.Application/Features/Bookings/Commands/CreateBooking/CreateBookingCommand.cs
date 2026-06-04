using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Constants;
using Baytology.Domain.Entities;
using Baytology.Domain.Common.Enums;
using Baytology.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.Bookings.Commands.CreateBooking;

public record CreateBookingCommand(
    Guid PropertyId,
    string UserId,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate,
    decimal Amount,
    decimal CommissionRate,
    string Currency,
    string? Notes = null) : IRequest<Result<CreateBookingResponse>>;

public sealed record CreateBookingResponse(
    Guid BookingId,
    Guid PaymentId,
    string? RedirectUrl);
