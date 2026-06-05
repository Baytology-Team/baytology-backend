using System.Data;

using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Constants;
using Baytology.Domain.Entities;
using Baytology.Domain.Common.Enums;
using Baytology.Domain.Common.Results;

using MediatR;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Baytology.Application.Features.Bookings.Commands.CreateBooking;

public class CreateBookingCommandHandler(
    IAppDbContext context,
    IPaymentGateway paymentGateway,
    INotificationService notificationService,
    IIdentityService identityService,
    ISender sender)
    : IRequestHandler<CreateBookingCommand, Result<CreateBookingResponse>>
{
    public async Task<Result<CreateBookingResponse>> Handle(CreateBookingCommand request, CancellationToken ct)
    {
        Domain.Entities.Property property = null!;
        Booking booking = null!;
        Payment payment = null!;

        async Task<Result<(Domain.Entities.Property Property, Booking Booking, Payment Payment)>> PersistPendingBookingAsync()
        {
            IDbContextTransaction? transaction = null;

            try
            {
                if (context.Database.IsRelational())
                {
                    transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
                }

                var loadedProperty = await context.Properties
                    .FirstOrDefaultAsync(p => p.Id == request.PropertyId, ct);

                if (loadedProperty is null)
                {
                    await RollbackAsync(transaction, ct);
                    return Domain.Exceptions.PropertyErrors.NotFound;
                }

                var loadedBookingProperty = loadedProperty;

                if (loadedBookingProperty.Status is not PropertyStatus.Available)
                {
                    await RollbackAsync(transaction, ct);
                    return ApplicationErrors.Booking.PropertyNotAvailable;
                }

                if (loadedBookingProperty.AgentUserId == request.UserId)
                {
                    await RollbackAsync(transaction, ct);
                    return ApplicationErrors.Booking.SelfBooking;
                }

                var agentProfileExists = await context.AgentDetails
                    .AsNoTracking()
                    .AnyAsync(a => a.UserId == loadedBookingProperty.AgentUserId, ct);

                if (!agentProfileExists)
                {
                    await RollbackAsync(transaction, ct);
                    return ApplicationErrors.Booking.AgentUnavailable;
                }

                var hasConflict = await context.Bookings
                    .Where(b => b.PropertyId == request.PropertyId &&
                                b.Status != BookingStatus.Cancelled &&
                                b.StartDate < request.EndDate &&
                                request.StartDate < b.EndDate)
                    .AnyAsync(ct);

                if (hasConflict)
                {
                    await RollbackAsync(transaction, ct);
                    return ApplicationErrors.Booking.Overlapping;
                }

                var availabilityQuery = new Baytology.Application.Features.Availability.Queries.GetPropertyAvailability.GetPropertyAvailabilityQuery(
                    request.PropertyId,
                    request.StartDate.Date,
                    request.StartDate.Date.AddDays(1));

                var availabilityResult = await sender.Send(availabilityQuery, ct);
                if (availabilityResult.IsError || !availabilityResult.Value.Any(s => s.StartTime == request.StartDate && s.EndTime == request.EndDate))
                {
                    await RollbackAsync(transaction, ct);
                    return Error.Validation("Booking_TimeSlot_Invalid", "The requested time slot is not available or does not match the agent's schedule.");
                }

                var bookingResult = Booking.Create(
                    request.PropertyId,
                    request.UserId,
                    loadedBookingProperty.AgentUserId,
                    request.StartDate,
                    request.EndDate,
                    request.Notes);

                if (bookingResult.IsError)
                {
                    await RollbackAsync(transaction, ct);
                    return bookingResult.Errors;
                }

                var createdBooking = bookingResult.Value;
                var agentCommissionRate = await context.AgentDetails
                    .Where(a => a.UserId == loadedBookingProperty.AgentUserId)
                    .Select(a => (decimal?)a.CommissionRate)
                    .FirstOrDefaultAsync(ct);

                var commissionRate = request.CommissionRate > 0
                    ? request.CommissionRate
                    : agentCommissionRate ?? BaytologyConstants.DefaultCommissionRate;

                var paymentResult = Payment.Create(
                    request.PropertyId,
                    request.UserId,
                    loadedBookingProperty.AgentUserId,
                    request.Amount,
                    commissionRate,
                    PaymentPurpose.Deposit,
                    request.Currency);

                if (paymentResult.IsError)
                {
                    await RollbackAsync(transaction, ct);
                    return paymentResult.Errors;
                }

                var createdPayment = paymentResult.Value;
                createdPayment.MarkAsEscrow();

                createdBooking.AttachPayment(createdPayment.Id);

                context.Bookings.Add(createdBooking);
                context.Payments.Add(createdPayment);
                await context.SaveChangesAsync(ct);

                if (transaction is not null)
                {
                    await transaction.CommitAsync(ct);
                }

                return (loadedBookingProperty, createdBooking, createdPayment);
            }
            catch
            {
                await RollbackAsync(transaction, ct);
                throw;
            }
            finally
            {
                if (transaction is not null)
                {
                    await transaction.DisposeAsync();
                }
            }
        }

        try
        {
            var persistenceResult = context.Database.IsRelational()
                ? await context.Database.CreateExecutionStrategy().ExecuteAsync(PersistPendingBookingAsync)
                : await PersistPendingBookingAsync();

            if (persistenceResult.IsError)
            {
                return persistenceResult.Errors;
            }

            (property, booking, payment) = persistenceResult.Value;
        }
        catch (Exception ex) when (IsSqlDeadlock(ex))
        {
            var overlappingBookingExists = await context.Bookings
                .AsNoTracking()
                .Where(b => b.PropertyId == request.PropertyId &&
                            b.Status != BookingStatus.Cancelled &&
                            b.StartDate < request.EndDate &&
                            request.StartDate < b.EndDate)
                .AnyAsync(ct);

            return overlappingBookingExists
                ? ApplicationErrors.Booking.Overlapping
                : ApplicationErrors.Booking.ConcurrentRequest;
        }

        // Resolve payer info from the authenticated user's profile
        var userResult = await identityService.GetUserByIdAsync(request.UserId);
        var payerEmail = userResult.IsError ? "buyer@baytology.com" : userResult.Value.Email;

        var userProfile = await context.UserProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.UserId == request.UserId, ct);

        var payerName = userProfile?.DisplayName ?? "Baytology Buyer";
        var payerPhone = userProfile?.PhoneNumber ?? "N/A";

        var intentionResult = await paymentGateway.CreatePaymentIntentionAsync(
            payment.Amount,
            payment.Currency,
            payerEmail,
            payerName,
            payerPhone,
            payment.Id,
            ct);

        if (intentionResult.IsError)
        {
            payment.MarkFailed();
            booking.Cancel();
            await context.SaveChangesAsync(ct);
            return intentionResult.Errors;
        }

        var paymentTransactionResult = payment.RecordTransaction(
            intentionResult.Value.IntentionId,
            "Paymob",
            "Created",
            null);

        if (paymentTransactionResult.IsError)
            return paymentTransactionResult.Errors;

        context.PaymentTransactions.Add(paymentTransactionResult.Value);
        await context.SaveChangesAsync(ct);

        // Notify agent
        var notificationResult = Notification.Create(
            property.AgentUserId,
            NotificationType.PaymentUpdate,
            "New booking request",
            "A new booking has been created and is awaiting payment.",
            booking.Id.ToString(),
            ReferenceType.Booking);

        if (notificationResult.IsError)
            return notificationResult.Errors;

        await notificationService.SendAsync(notificationResult.Value, ct);

        return new CreateBookingResponse(
            booking.Id,
            payment.Id,
            intentionResult.Value.RedirectUrl);
    }

    private static async Task RollbackAsync(IDbContextTransaction? transaction, CancellationToken ct)
    {
        if (transaction is not null)
        {
            await transaction.RollbackAsync(ct);
        }
    }

    private static bool IsSqlDeadlock(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException!)
        {
            var type = current.GetType();
            if (type.FullName is "Microsoft.Data.SqlClient.SqlException" or "System.Data.SqlClient.SqlException")
            {
                var number = type.GetProperty("Number")?.GetValue(current);
                if (number is int sqlErrorNumber && sqlErrorNumber == 1205)
                    return true;
            }
        }

        return false;
    }
}
