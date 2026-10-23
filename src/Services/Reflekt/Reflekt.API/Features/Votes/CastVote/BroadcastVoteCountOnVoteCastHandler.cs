using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Votes.Events;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Votes.CastVote;

public sealed class BroadcastVoteCountOnVoteCastHandler(
    IHubContext<SprintHub> hubContext,
    IServiceScopeFactory scopeFactory)
    : IEventHandler<VoteCastEvent>
{
    public async Task HandleAsync(VoteCastEvent ev, CancellationToken ct)
    {
        var groupId = ev.Vote.GroupId;
        using var scope = scopeFactory.CreateScope();
        var cardGroupRepository = scope.ServiceProvider.GetRequiredService<CardGroupRepository>();
        var voteRepository = scope.ServiceProvider.GetRequiredService<VoteRepository>();
        var group = await cardGroupRepository.GetByIdAsync(groupId, ct);
        if (group is null) return;

        var newCount = await voteRepository.CountByGroupAsync(groupId, ct);
        await hubContext.Clients.Group($"sprint:{group.SprintId}")
            .SendAsync("VoteCast", new { groupId, newCount }, ct);
    }
}
