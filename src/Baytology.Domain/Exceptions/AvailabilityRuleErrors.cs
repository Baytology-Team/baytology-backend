using Baytology.Domain.Common.Results;

namespace Baytology.Domain.Exceptions;

public static class AvailabilityRuleErrors
{
    public static readonly Error AgentRequired =
        Error.Validation("AvailabilityRule_Agent_Required", "Agent user ID is required.");

    public static readonly Error TimeRangeInvalid =
        Error.Validation("AvailabilityRule_TimeRange_Invalid", "End time must be after start time.");

    public static readonly Error SlotDurationInvalid =
        Error.Validation("AvailabilityRule_SlotDuration_Invalid", "Slot duration must be greater than zero and fit within the time range.");

    public static readonly Error DayOfWeekRequired =
        Error.Validation("AvailabilityRule_DayOfWeek_Required", "DayOfWeek is required for Weekly recurrence.");

    public static readonly Error SpecificDateRequired =
        Error.Validation("AvailabilityRule_SpecificDate_Required", "SpecificDate is required when recurrence is None.");

    public static readonly Error NotFound =
        Error.NotFound("AvailabilityRule_Not_Found", "Availability rule not found.");
}
