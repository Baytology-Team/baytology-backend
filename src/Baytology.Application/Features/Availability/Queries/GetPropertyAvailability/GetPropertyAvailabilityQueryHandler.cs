using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Enums;
using Baytology.Domain.Common.Results;
using Baytology.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.Availability.Queries.GetPropertyAvailability;

public class GetPropertyAvailabilityQueryHandler(IAppDbContext context)
    : IRequestHandler<GetPropertyAvailabilityQuery, Result<List<TimeSlotDto>>>
{
    public async Task<Result<List<TimeSlotDto>>> Handle(GetPropertyAvailabilityQuery request, CancellationToken ct)
    {
        if (request.EndDate <= request.StartDate)
            return Error.Validation("Availability_DateRange_Invalid", "End date must be after start date.");

        var property = await context.Properties
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == request.PropertyId, ct);

        if (property is null)
            return PropertyErrors.NotFound;

        var agentId = property.AgentUserId;

        var propertyRules = await context.AvailabilityRules
            .AsNoTracking()
            .Where(r => r.PropertyId == request.PropertyId)
            .ToListAsync(ct);

        var rules = propertyRules;
        if (rules.Count == 0)
        {
            rules = await context.AvailabilityRules
                .AsNoTracking()
                .Where(r => r.AgentUserId == agentId && r.PropertyId == null)
                .ToListAsync(ct);
        }

        if (rules.Count == 0)
            return new List<TimeSlotDto>(); // No availability defined

        var bookings = await context.Bookings
            .AsNoTracking()
            .Where(b => b.AgentUserId == agentId && b.Status != BookingStatus.Cancelled)
            .Where(b => b.EndDate > request.StartDate && b.StartDate < request.EndDate)
            .ToListAsync(ct);

        var availableSlots = new List<TimeSlotDto>();
        var currentDate = request.StartDate.Date;
        var endDate = request.EndDate.Date;

        while (currentDate <= endDate)
        {
            var dateOnly = DateOnly.FromDateTime(currentDate);
            var dayOfWeek = currentDate.DayOfWeek;

            var dayRules = rules.Where(r => 
                (r.RecurrenceType == RecurrenceType.Daily) ||
                (r.RecurrenceType == RecurrenceType.Weekly && r.DayOfWeek == dayOfWeek) ||
                (r.RecurrenceType == RecurrenceType.None && r.SpecificDate == dateOnly)
            ).ToList();

            foreach (var rule in dayRules)
            {
                var slotStart = rule.StartTime;
                while (slotStart + rule.SlotDuration <= rule.EndTime)
                {
                    var slotEnd = slotStart + rule.SlotDuration;
                    
                    var slotStartOffset = new DateTimeOffset(currentDate.Year, currentDate.Month, currentDate.Day, slotStart.Hours, slotStart.Minutes, slotStart.Seconds, request.StartDate.Offset);
                    var slotEndOffset = new DateTimeOffset(currentDate.Year, currentDate.Month, currentDate.Day, slotEnd.Hours, slotEnd.Minutes, slotEnd.Seconds, request.StartDate.Offset);

                    // Skip slots in the past
                    if (slotStartOffset >= DateTimeOffset.UtcNow && slotStartOffset >= request.StartDate && slotEndOffset <= request.EndDate)
                    {
                        // Check if it overlaps with any booking
                        var overlaps = bookings.Any(b => b.StartDate < slotEndOffset && b.EndDate > slotStartOffset);
                        if (!overlaps)
                        {
                            availableSlots.Add(new TimeSlotDto(slotStartOffset, slotEndOffset));
                        }
                    }

                    slotStart += rule.SlotDuration;
                }
            }

            currentDate = currentDate.AddDays(1);
        }

        return availableSlots.OrderBy(s => s.StartTime).ToList();
    }
}
