using System.Data.Common;
using System.Reflection;
using Blocks.Domain;
using FastEndpoints;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Blocks.Persistence;

public class DomainEventPublishingInterceptor : SaveChangesInterceptor, IDbTransactionInterceptor
{
    // FastEndpoints' PublishAsync<TEvent> extension dispatches by compile-time T.
    // When events are typed as IDomainEvent, T resolves to IDomainEvent and no concrete
    // handler (IEventHandler<CardCreatedEvent>, ...) is ever invoked. We invoke the
    // generic helper reflectively with the runtime type instead. MethodInfo is cached.
    private static readonly MethodInfo PublishEventMethod = typeof(DomainEventPublishingInterceptor)
        .GetMethod(nameof(PublishEvent), BindingFlags.NonPublic | BindingFlags.Static)!;

    private List<IDomainEvent> _collectedEvents = [];
    private List<IDomainEvent> _heldEvents = [];

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

            _collectedEvents = aggregates
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
        var events = _collectedEvents;
        _collectedEvents = [];

        // EF's own save transaction has already committed and closed here, so an open
        // transaction is always an explicit one whose commit has not happened yet.
        if (eventData.Context?.Database.CurrentTransaction is not null)
        {
            _heldEvents.AddRange(events);
            return result;
        }

        await PublishAsync(events, cancellationToken);
        return result;
    }

    public override Task SaveChangesFailedAsync(
        DbContextErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _collectedEvents = [];
        return base.SaveChangesFailedAsync(eventData, cancellationToken);
    }

    public override Task SaveChangesCanceledAsync(
        DbContextEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _collectedEvents = [];
        return base.SaveChangesCanceledAsync(eventData, cancellationToken);
    }

    // EF raises no callback for a transaction disposed without commit, so its held events
    // are dropped here, when the next transaction starts: an explicit one, before any of its saves,
    // or EF's own for a save that opens one. A save that opens no transaction leaves them in place;
    // nothing but a commit publishes them.
    public ValueTask<DbTransaction> TransactionStartedAsync(
        DbConnection connection,
        TransactionEndEventData eventData,
        DbTransaction result,
        CancellationToken cancellationToken = default)
    {
        _heldEvents = [];
        return ValueTask.FromResult(result);
    }

    public Task TransactionCommittedAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        var events = _heldEvents;
        _heldEvents = [];
        return PublishAsync(events, cancellationToken);
    }

    public Task TransactionRolledBackAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _heldEvents = [];
        return Task.CompletedTask;
    }

    public Task TransactionFailedAsync(
        DbTransaction transaction,
        TransactionErrorEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _heldEvents = [];
        return Task.CompletedTask;
    }

    private static Task PublishAsync(IReadOnlyList<IDomainEvent> events, CancellationToken cancellationToken)
    {
        if (events.Count == 0)
            return Task.CompletedTask;

        var tasks = events.Select(ev =>
        {
            var typed = PublishEventMethod.MakeGenericMethod(ev.GetType());
            return (Task)typed.Invoke(null, [ev, cancellationToken])!;
        });

        return Task.WhenAll(tasks);
    }

    private static Task PublishEvent<TEvent>(TEvent ev, CancellationToken ct)
        where TEvent : class, IEvent
        => ev.PublishAsync(Mode.WaitForNone, ct);
}
