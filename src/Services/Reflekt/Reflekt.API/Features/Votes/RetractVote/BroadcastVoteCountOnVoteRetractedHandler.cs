using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Votes.Events;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Votes.RetractVote;

public sealed class BroadcastVoteCountOnVoteRetractedHandler(
    IHubContext<SprintHub> hubContext,
    IServiceScopeFactory scopeFactory)
    : IEventHandler<VoteRetractedEvent>
{
    public async Task HandleAsync(VoteRetractedEvent ev, CancellationToken ct)
    {
        var groupId = ev.Vote.GroupId;
        using var scope = scopeFactory.CreateScope();
        var cardGroupRepository = scope.ServiceProvider.GetRequiredService<CardGroupRepository>();
        var voteRepository = scope.ServiceProvider.GetRequiredService<VoteRepository>();
        var group = await cardGroupRepository.GetByIdAsync(groupId, ct);
        if (group is null) return;

        var newCount = await voteRepository.CountByGroupAsync(groupId, ct);
        await hubContext.Clients.Group($"sprint:{group.SprintId}")
            .SendAsync("VoteRetracted", new { groupId, newCount }, ct);
    }
}
