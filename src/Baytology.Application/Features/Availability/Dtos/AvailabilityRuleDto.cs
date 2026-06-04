using Baytology.Domain.Common.Enums;

namespace Baytology.Application.Features.Availability.Dtos;

public record AvailabilityRuleDto(
    Guid Id,
    Guid? PropertyId,
    RecurrenceType RecurrenceType,
    DayOfWeek? DayOfWeek,
    DateOnly? SpecificDate,
    TimeSpan StartTime,
    TimeSpan EndTime,
    TimeSpan SlotDuration);
