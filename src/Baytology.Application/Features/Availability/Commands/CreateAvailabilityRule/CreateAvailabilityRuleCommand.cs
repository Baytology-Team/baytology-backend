using Baytology.Domain.Common.Enums;
using Baytology.Domain.Common.Results;
using MediatR;

namespace Baytology.Application.Features.Availability.Commands.CreateAvailabilityRule;

public record CreateAvailabilityRuleCommand(
    string AgentUserId,
    Guid? PropertyId,
    RecurrenceType RecurrenceType,
    DayOfWeek? DayOfWeek,
    DateOnly? SpecificDate,
    TimeSpan StartTime,
    TimeSpan EndTime,
    TimeSpan SlotDuration) : IRequest<Result<Guid>>;
