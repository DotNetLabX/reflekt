using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Cards;
using Reflekt.Domain.Sprints;

namespace Reflekt.API.Features.CardGroups;

public static class CardGroupResponseBuilder
{
    public static CardGroupResponse Build(
        CardGroup group, IEnumerable<CardGroupCardResponse> cards, Sprint sprint, int voteCount = 0)
        => group.Adapt<CardGroupResponse>() with
        {
            Cards = cards.ToList(),
            VoteCount = voteCount,
            IsFocused = sprint.FocusedGroupId == group.Id,
        };

    public static CardGroupResponse Build(CardGroup group, IEnumerable<Card> cards, Sprint sprint)
        => Build(group, cards.Select(c => c.Adapt<CardGroupCardResponse>()), sprint);

    public static CardGroupResponse BuildFromSprintCards(CardGroup group, IEnumerable<Card> sprintCards, Sprint sprint)
        => Build(group, sprintCards.Where(c => c.GroupId == group.Id).OrderBy(c => c.CreatedAt), sprint);
}
