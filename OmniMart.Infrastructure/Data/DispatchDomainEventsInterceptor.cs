using MediatR;
using Microsoft.EntityFrameworkCore.Diagnostics;
using OmniMart.Domain.Common;

public class DispatchDomainEventsInterceptor : SaveChangesInterceptor
{
    private readonly IPublisher _publisher;
    private List<IDomainEvent> _eventsToDispatch = new();

    public DispatchDomainEventsInterceptor(IPublisher publisher)
    {
        _publisher = publisher;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context != null)
        {
            var entities = eventData.Context.ChangeTracker.Entries<BaseEntity>()
                .Where(e => e.Entity.DomainEvents.Any()).Select(e => e.Entity).ToList();

            _eventsToDispatch = entities.SelectMany(e => e.DomainEvents).ToList();

            foreach (var entity in entities) entity.ClearDomainEvents();
        }
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        if (result == 0 || !_eventsToDispatch.Any())
        {
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        foreach (var domainEvent in _eventsToDispatch)
        {
            await _publisher.Publish(domainEvent, cancellationToken);
        }

        _eventsToDispatch.Clear(); 
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }
}