using Baytology.Application.Common.Interfaces;
using Baytology.Domain.Common.Enums;
using Baytology.Domain.Common.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Baytology.Application.Features.Availability.Queries.GetAgentAvailability;

public class GetAgentAvailabilityQueryHandler(IAppDbContext context)
    : IRequestHandler<GetAgentAvailabilityQuery, Result<List<TimeSlotDto>>>
{
    public async Task<Result<List<TimeSlotDto>>> Handle(GetAgentAvailabilityQuery request, CancellationToken ct)
    {
        if (request.EndDate <= request.StartDate)
            return Error.Validation("Availability_DateRange_Invalid", "End date must be after start date.");

        var agentId = request.AgentUserId;

        var agentRules = await context.AvailabilityRules
            .AsNoTracking()
            .Where(r => r.AgentUserId == agentId)
            .ToListAsync(ct);

        if (agentRules.Count == 0)
            return new List<TimeSlotDto>(); // No availability defined

        var propertyIds = agentRules.Where(r => r.PropertyId.HasValue).Select(r => r.PropertyId!.Value).Distinct().ToList();

        var properties = await context.Properties
            .AsNoTracking()
            .Where(p => propertyIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Title })
            .ToListAsync(ct);

        var propertyDictionary = properties.ToDictionary(p => p.Id, p => p.Title);

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

            var dayRules = agentRules.Where(r =>
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
                            var propertyId = rule.PropertyId ?? Guid.Empty;
                            var propertyTitle = propertyId != Guid.Empty && propertyDictionary.TryGetValue(propertyId, out var title) ? title : "General Availability";
                            availableSlots.Add(new TimeSlotDto(slotStartOffset, slotEndOffset, propertyId, propertyTitle));
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
