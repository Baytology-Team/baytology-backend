using Baytology.Domain.Common.Results;
using MediatR;

namespace Baytology.Application.Features.Availability.Queries.GetAgentAvailability;

public record TimeSlotDto(DateTimeOffset StartTime, DateTimeOffset EndTime, Guid PropertyId, string PropertyTitle);

public record GetAgentAvailabilityQuery(
    string AgentUserId,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate) : IRequest<Result<List<TimeSlotDto>>>;
