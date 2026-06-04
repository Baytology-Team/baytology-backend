using Baytology.Domain.Common.Results;
using MediatR;

namespace Baytology.Application.Features.Availability.Commands.DeleteAvailabilityRule;

public record DeleteAvailabilityRuleCommand(Guid Id, string AgentUserId) : IRequest<Result<Success>>;
