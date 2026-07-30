using EventGateway.Domain.Entities;

namespace EventGateway.Application.Interfaces;

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(string eventId, CancellationToken ct);
    Task<List<Event>> GetByAccountIdAsync(string accountId, CancellationToken ct);
    Task AddAsync(Event @event, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}