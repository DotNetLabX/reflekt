using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.CardGroups.Events;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.CardGroups.CreateGroup;

public sealed class BroadcastGroupCreatedHandler(
    IHubContext<SprintHub> hubContext,
    IServiceScopeFactory scopeFactory)
    : IEventHandler<CardGroupCreatedEvent>
{
    public async Task HandleAsync(CardGroupCreatedEvent ev, CancellationToken ct)
    {
        var group = ev.Group;
        using var scope = scopeFactory.CreateScope();
        var cardRepository = scope.ServiceProvider.GetRequiredService<CardRepository>();
        var cards = (await cardRepository.GetBySprintIdReadOnlyAsync(group.SprintId, ct))
            .Where(c => c.GroupId == group.Id)
            .ToList();

        await hubContext.Clients.Group($"sprint:{group.SprintId}")
            .SendAsync("GroupCreated", new
            {
                group = new
                {
                    id = group.Id,
                    sprintId = group.SprintId,
                    columnType = group.ColumnType.ToString(),
                    title = group.Title,
                    cardIds = cards.Select(c => c.Id).ToList()
                }
            }, ct);
    }
}
