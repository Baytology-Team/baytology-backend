using Baytology.Application.Features.Availability.Queries.GetPropertyAvailability;
using Baytology.Domain.Common.Results;
using MediatR;

namespace Baytology.Application.Tests.Support;

public class TestSender : ISender
{
    public List<TimeSlotDto> ExpectedSlots { get; set; } = new List<TimeSlotDto>();

    public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        if (request is GetPropertyAvailabilityQuery availabilityQuery)
        {
            Result<List<TimeSlotDto>> result = ExpectedSlots;
            return Task.FromResult((TResponse)(object)result);
        }

        return Task.FromResult(default(TResponse)!);
    }

    public Task<object?> Send(object request, CancellationToken cancellationToken = default) => Task.FromResult<object?>(null);

    public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default) where TRequest : IRequest
    {
        return Task.CompletedTask;
    }

    public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}
