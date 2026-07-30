using EventGateway.Application.Interfaces;
using EventGateway.Domain.Entities;
using EventGateway.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EventGateway.Infrastructure.Repositories;

public class EventRepository : IEventRepository
{
    private readonly EventDbContext _context;

    public EventRepository(EventDbContext context)
    {
        _context = context;
    }

    public async Task<Event?> GetByIdAsync(string eventId, CancellationToken ct)
        => await _context.Events.FirstOrDefaultAsync(e => e.EventId == eventId, ct);

    public async Task<List<Event>> GetByAccountIdAsync(string accountId, CancellationToken ct)
        => await _context.Events.Where(e => e.AccountId == accountId).ToListAsync(ct);

    public async Task AddAsync(Event @event, CancellationToken ct)
        => await _context.Events.AddAsync(@event, ct);

    public async Task SaveChangesAsync(CancellationToken ct)
        => await _context.SaveChangesAsync(ct);
}