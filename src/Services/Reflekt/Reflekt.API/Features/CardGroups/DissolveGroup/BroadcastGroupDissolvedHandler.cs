using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.CardGroups.Events;

namespace Reflekt.API.Features.CardGroups.DissolveGroup;

public sealed class BroadcastGroupDissolvedHandler(
    IHubContext<SprintHub> hubContext)
    : IEventHandler<GroupDissolvedEvent>
{
    public async Task HandleAsync(GroupDissolvedEvent ev, CancellationToken ct)
    {
        await hubContext.Clients.Group($"sprint:{ev.Group.SprintId}")
            .SendAsync("GroupDissolved", new { groupId = ev.Group.Id }, ct);
    }
}
