using MediatR;

namespace EventGateway.Application.Queries;

public record EventDto(
    string EventId, string AccountId, string Type, decimal Amount,
    string Currency, DateTimeOffset EventTimestamp, string Status, DateTimeOffset ReceivedAt);

public record GetEventByIdQuery(string EventId) : IRequest<EventDto?>;
public record GetEventsByAccountQuery(string AccountId) : IRequest<List<EventDto>>;