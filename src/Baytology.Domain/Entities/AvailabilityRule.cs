using Baytology.Domain.Common;
using Baytology.Domain.Common.Enums;
using Baytology.Domain.Common.Results;
using Baytology.Domain.Exceptions;

namespace Baytology.Domain.Entities;

public sealed class AvailabilityRule : Entity
{
    public string AgentUserId { get; private set; } = null!;
    public Guid? PropertyId { get; private set; }
    public RecurrenceType RecurrenceType { get; private set; }
    public DayOfWeek? DayOfWeek { get; private set; }
    public DateOnly? SpecificDate { get; private set; }
    public TimeSpan StartTime { get; private set; }
    public TimeSpan EndTime { get; private set; }
    public TimeSpan SlotDuration { get; private set; }

    private AvailabilityRule() { }

    private AvailabilityRule(
        Guid id,
        string agentUserId,
        Guid? propertyId,
        RecurrenceType recurrenceType,
        DayOfWeek? dayOfWeek,
        DateOnly? specificDate,
        TimeSpan startTime,
        TimeSpan endTime,
        TimeSpan slotDuration) : base(id)
    {
        AgentUserId = agentUserId;
        PropertyId = propertyId;
        RecurrenceType = recurrenceType;
        DayOfWeek = dayOfWeek;
        SpecificDate = specificDate;
        StartTime = startTime;
        EndTime = endTime;
        SlotDuration = slotDuration;
    }

    public static Result<AvailabilityRule> Create(
        string agentUserId,
        Guid? propertyId,
        RecurrenceType recurrenceType,
        DayOfWeek? dayOfWeek,
        DateOnly? specificDate,
        TimeSpan startTime,
        TimeSpan endTime,
        TimeSpan slotDuration)
    {
        if (string.IsNullOrWhiteSpace(agentUserId))
            return AvailabilityRuleErrors.AgentRequired;

        if (endTime <= startTime)
            return AvailabilityRuleErrors.TimeRangeInvalid;

        if (slotDuration <= TimeSpan.Zero || slotDuration > (endTime - startTime))
            return AvailabilityRuleErrors.SlotDurationInvalid;

        if (recurrenceType == RecurrenceType.Weekly && dayOfWeek is null)
            return AvailabilityRuleErrors.DayOfWeekRequired;

        if (recurrenceType == RecurrenceType.None && specificDate is null)
            return AvailabilityRuleErrors.SpecificDateRequired;

        return new AvailabilityRule(
            Guid.NewGuid(),
            agentUserId.Trim(),
            propertyId,
            recurrenceType,
            recurrenceType == RecurrenceType.Weekly ? dayOfWeek : null,
            recurrenceType == RecurrenceType.None ? specificDate : null,
            startTime,
            endTime,
            slotDuration);
    }
}
