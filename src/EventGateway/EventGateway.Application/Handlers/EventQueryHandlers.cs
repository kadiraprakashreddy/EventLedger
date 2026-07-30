using EventGateway.Application.Interfaces;
using EventGateway.Application.Queries;
using MediatR;

namespace EventGateway.Application.Handlers;

public class GetEventByIdQueryHandler : IRequestHandler<GetEventByIdQuery, EventDto?>
{
    private readonly IEventRepository _repository;
    public GetEventByIdQueryHandler(IEventRepository repository) => _repository = repository;

    public async Task<EventDto?> Handle(GetEventByIdQuery request, CancellationToken ct)
    {
        var e = await _repository.GetByIdAsync(request.EventId, ct);
        return e is null ? null : Map(e);
    }

    internal static EventDto Map(Domain.Entities.Event e) => new(
        e.EventId, e.AccountId, e.Type.ToString(), e.Amount, e.Currency, e.EventTimestamp, e.Status.ToString(), e.ReceivedAt);
}

public class GetEventsByAccountQueryHandler : IRequestHandler<GetEventsByAccountQuery, List<EventDto>>
{
    private readonly IEventRepository _repository;
    public GetEventsByAccountQueryHandler(IEventRepository repository) => _repository = repository;

    public async Task<List<EventDto>> Handle(GetEventsByAccountQuery request, CancellationToken ct)
    {
        var events = await _repository.GetByAccountIdAsync(request.AccountId, ct);
        return events
            .OrderBy(e => e.EventTimestamp)   // listings ordered chronologically by eventTimestamp
            .Select(GetEventByIdQueryHandler.Map)
            .ToList();
    }
}