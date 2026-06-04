using Baytology.Domain.Common.Enums;
using FluentValidation;

namespace Baytology.Application.Features.Availability.Commands.CreateAvailabilityRule;

public class CreateAvailabilityRuleCommandValidator : AbstractValidator<CreateAvailabilityRuleCommand>
{
    public CreateAvailabilityRuleCommandValidator()
    {
        RuleFor(x => x.AgentUserId).NotEmpty();
        RuleFor(x => x.StartTime).LessThan(x => x.EndTime).WithMessage("Start time must be before end time.");
        RuleFor(x => x.SlotDuration).GreaterThan(TimeSpan.Zero);
        
        RuleFor(x => x)
            .Must(x => x.SlotDuration <= (x.EndTime - x.StartTime))
            .WithMessage("Slot duration must fit within the start and end time.");

        RuleFor(x => x.DayOfWeek)
            .NotNull()
            .When(x => x.RecurrenceType == RecurrenceType.Weekly)
            .WithMessage("DayOfWeek is required for Weekly recurrence.");

        RuleFor(x => x.SpecificDate)
            .NotNull()
            .When(x => x.RecurrenceType == RecurrenceType.None)
            .WithMessage("SpecificDate is required when recurrence is None.");
    }
}
