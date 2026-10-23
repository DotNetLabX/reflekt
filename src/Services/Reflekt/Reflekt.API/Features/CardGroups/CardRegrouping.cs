using Blocks.Exceptions;
using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Cards;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.CardGroups;

public sealed class CardRegrouping(CardRepository cardRepository, CardGroupRepository cardGroupRepository)
{
    public async Task<IReadOnlyList<Card>> FindSprintCardsAsync(
        int sprintId, IReadOnlyCollection<int> cardIds, CancellationToken ct)
    {
        var sprintCards = await cardRepository.GetBySprintIdAsync(sprintId, ct);
        var selectedCards = sprintCards.Where(c => cardIds.Contains(c.Id)).ToList();

        if (selectedCards.Count != cardIds.Count)
            throw new BadRequestException("One or more cards not found in this sprint.");

        return selectedCards;
    }

    public async Task MoveCardsAsync(
        int sprintId, IReadOnlyCollection<Card> cards, CardGroup targetGroup, CancellationToken ct)
    {
        var movedCardIds = cards.Select(c => c.Id).ToHashSet();
        var sourceGroupIds = cards
            .Where(c => c.GroupId.HasValue && c.GroupId != targetGroup.Id)
            .Select(c => c.GroupId!.Value)
            .Distinct()
            .ToList();

        foreach (var card in cards)
            card.SetGroupId(targetGroup.Id);

        foreach (var sourceGroupId in sourceGroupIds)
        {
            if (await cardRepository.HasCardsInGroupAsync(sprintId, sourceGroupId, movedCardIds, ct))
                continue;

            var sourceGroup = await cardGroupRepository.GetByIdAsync(sourceGroupId, ct);
            if (sourceGroup is not null)
                cardGroupRepository.Remove(sourceGroup);
        }
    }
}
