using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Cards.Events;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.PreviousActions.SetReviewStatus;

public sealed class BroadcastReviewStatusChangedHandler(
    IHubContext<SprintHub> hubContext,
    IServiceScopeFactory scopeFactory)
    : IEventHandler<ReviewStatusChangedEvent>
{
    public async Task HandleAsync(ReviewStatusChangedEvent ev, CancellationToken ct)
    {
        var card = ev.Card;
        var previousSprintId = card.SprintId;
        using var scope = scopeFactory.CreateScope();
        var cardRepository = scope.ServiceProvider.GetRequiredService<CardRepository>();

        var totalCount = await cardRepository.CountActionItemsBySprintIdAsync(previousSprintId, ct);
        var unreviewedCount = await cardRepository.CountUnreviewedActionItemsBySprintIdAsync(previousSprintId, ct);
        var reviewedCount = totalCount - unreviewedCount;

        await hubContext.Clients
            .Group($"sprint:{card.ReviewedInSprintId}")
            .SendAsync("ReviewStatusChanged", new
            {
                cardId = card.Id,
                reviewStatus = card.ReviewStatus.ToString(),
                reviewedCount,
                totalCount,
            }, ct);
    }
}
