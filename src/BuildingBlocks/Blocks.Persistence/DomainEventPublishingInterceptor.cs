using System.Data.Common;
using System.Reflection;
using Blocks.Domain;
using FastEndpoints;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Blocks.Persistence;

public class DomainEventPublishingInterceptor(ILogger<DomainEventPublishingInterceptor> logger)
    : SaveChangesInterceptor, IDbTransactionInterceptor
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

        await PublishInOrderAsync(events);
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
        return PublishInOrderAsync(events);
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

    // Handlers get CancellationToken.None, not the save's token: a client that aborts after the commit
    // must not stop the broadcast of data that is already committed.
    private async Task PublishInOrderAsync(IReadOnlyList<IDomainEvent> events)
    {
        foreach (var ev in events)
        {
            var publication = StartPublish(ev);
            await publication.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);
            LogFailures(ev, publication);
        }
    }

    private static Task StartPublish(IDomainEvent ev)
    {
        try
        {
            var typed = PublishEventMethod.MakeGenericMethod(ev.GetType());
            return (Task)typed.Invoke(null, [ev, CancellationToken.None])!;
        }
        catch (TargetInvocationException ex)
        {
            return Task.FromException(ex.InnerException ?? ex);
        }
    }

    private void LogFailures(IDomainEvent ev, Task publication)
    {
        if (publication.Exception is { } failure)
        {
            foreach (var handlerFailure in failure.InnerExceptions)
                logger.LogError(handlerFailure, "Domain event handler failed for {EventType}.", ev.GetType().Name);
        }
        else if (publication.IsCanceled)
        {
            logger.LogError("Domain event publication was cancelled for {EventType}.", ev.GetType().Name);
        }
    }

    private static Task PublishEvent<TEvent>(TEvent ev, CancellationToken ct)
        where TEvent : class, IEvent
        => ev.PublishAsync(Mode.WaitForAll, ct);
}
