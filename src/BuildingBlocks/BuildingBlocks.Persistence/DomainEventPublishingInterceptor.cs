using System.Reflection;
using BuildingBlocks.Domain;
using FastEndpoints;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace BuildingBlocks.Persistence;

public class DomainEventPublishingInterceptor : SaveChangesInterceptor
{
    // FastEndpoints' PublishAsync<TEvent> extension dispatches by compile-time T.
    // When events are typed as IDomainEvent, T resolves to IDomainEvent and no concrete
    // handler (IEventHandler<CardCreatedEvent>, ...) is ever invoked. We invoke the
    // generic helper reflectively with the runtime type instead. MethodInfo is cached.
    private static readonly MethodInfo PublishEventMethod = typeof(DomainEventPublishingInterceptor)
        .GetMethod(nameof(PublishEvent), BindingFlags.NonPublic | BindingFlags.Static)!;

    private List<IDomainEvent> _pendingEvents = [];

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;
        if (context is not null)
        {
            var aggregates = context.ChangeTracker
                .Entries()
                .Select(e => e.Entity)
                .OfType<IHasDomainEvents>()
                .Where(a => a.DomainEvents.Count > 0)
                .ToList();

            _pendingEvents = aggregates
                .SelectMany(a => a.DomainEvents)
                .ToList();

            foreach (var aggregate in aggregates)
                aggregate.ClearDomainEvents();
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var events = _pendingEvents;
        _pendingEvents = [];

        var tasks = events.Select(ev =>
        {
            var typed = PublishEventMethod.MakeGenericMethod(ev.GetType());
            return (Task)typed.Invoke(null, [ev, cancellationToken])!;
        });

        await Task.WhenAll(tasks);

        return result;
    }

    private static Task PublishEvent<TEvent>(TEvent ev, CancellationToken ct)
        where TEvent : class, IEvent
        => ev.PublishAsync(Mode.WaitForNone, ct);
}
