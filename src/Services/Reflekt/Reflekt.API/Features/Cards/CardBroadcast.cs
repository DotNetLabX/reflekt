using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Hubs;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Cards;

public static class CardBroadcast
{
    public static async Task BroadcastAsync(
        IHubClients clients, IServiceScopeFactory scopeFactory, Card card, string message, CancellationToken ct)
    {
        var payload = card.Adapt<CardResponse>();

        var recipients = await ReadRecipientsAsync(scopeFactory, card, ct);
        if (!recipients.IsWritingPhase)
        {
            await clients.Group($"sprint:{card.SprintId}").SendAsync(message, payload, ct);
            return;
        }

        await clients.Group(SprintHub.ParticipantGroup(card.ParticipantId))
            .SendAsync(message, payload, ct);
        if (recipients.RedactedGroups.Count > 0)
            await clients.Groups(recipients.RedactedGroups)
                .SendAsync(message, WritingPhaseRedaction.Redact(payload), ct);
    }

    public static async Task<bool> IsWritingPhaseCardAsync(IServiceScopeFactory scopeFactory, Card card, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var sprint = await FindSprintAsync(scope.ServiceProvider, card, ct);
        return sprint is not null && WritingPhaseRedaction.IsWritingPhaseCard(card, sprint);
    }

    private static async Task<CardRecipients> ReadRecipientsAsync(IServiceScopeFactory scopeFactory, Card card, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var sprint = await FindSprintAsync(scope.ServiceProvider, card, ct);
        if (sprint is null || !WritingPhaseRedaction.IsWritingPhaseCard(card, sprint))
            return new CardRecipients(false, []);

        var participants = await scope.ServiceProvider.GetRequiredService<ParticipantRepository>()
            .GetBySprintIdAsync(card.SprintId, ct);
        var redactedGroups = participants
            .Where(p => WritingPhaseRedaction.IsRedacted(card, sprint, p.Id))
            .Select(p => SprintHub.ParticipantGroup(p.Id))
            .ToList();
        return new CardRecipients(true, redactedGroups);
    }

    private static Task<Sprint?> FindSprintAsync(IServiceProvider services, Card card, CancellationToken ct)
        => services.GetRequiredService<SprintRepository>().GetByIdReadOnlyAsync(card.SprintId, ct);

    private sealed record CardRecipients(bool IsWritingPhase, IReadOnlyList<string> RedactedGroups);
}
