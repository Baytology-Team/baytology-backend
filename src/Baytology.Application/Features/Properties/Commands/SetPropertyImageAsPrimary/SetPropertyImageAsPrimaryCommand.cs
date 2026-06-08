using Baytology.Domain.Common.Results;
using MediatR;

namespace Baytology.Application.Features.Properties.Commands.SetPropertyImageAsPrimary;

public record SetPropertyImageAsPrimaryCommand(Guid PropertyId, Guid ImageId, Guid AgentUserId) : IRequest<Result<Success>>;
