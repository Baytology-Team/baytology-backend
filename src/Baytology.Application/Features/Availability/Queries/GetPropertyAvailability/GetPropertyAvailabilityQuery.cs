using Baytology.Domain.Common.Results;
using MediatR;

namespace Baytology.Application.Features.Availability.Queries.GetPropertyAvailability;

public record TimeSlotDto(DateTimeOffset StartTime, DateTimeOffset EndTime);

public record GetPropertyAvailabilityQuery(
    Guid PropertyId,
    DateTimeOffset StartDate,
    DateTimeOffset EndDate) : IRequest<Result<List<TimeSlotDto>>>;
