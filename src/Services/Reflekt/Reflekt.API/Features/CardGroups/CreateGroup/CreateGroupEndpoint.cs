using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.API.Features.PhaseActions;
using Reflekt.Domain.CardGroups;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.CardGroups.CreateGroup;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpPost("/api/sprints/{sprintId}/card-groups")]
[Tags("CardGroups")]
public sealed class CreateGroupEndpoint(
    SprintAccess sprintAccess,
    CardGroupRepository cardGroupRepository,
    CardRegrouping cardRegrouping,
    PhaseActionPolicyProvider phaseActionPolicyProvider)
    : Endpoint<CreateGroupRequest, CardGroupResponse>
{
    public override async Task HandleAsync(CreateGroupRequest req, CancellationToken ct)
    {
        var access = await sprintAccess.ForSprintAsync(User, req.SprintId, ct);
        var sprint = access.RequireSprint();
        access.RequireOwner();

        sprint.EnsureOpen();

        (await phaseActionPolicyProvider.GetAsync()).EnsureAllowed(sprint.CurrentPhase, SprintAction.GroupCards);

        CardGroup.EnsureEnoughCardsToGroup(req.CardIds.Count);
        var selectedCards = await cardRegrouping.FindSprintCardsAsync(req.SprintId, req.CardIds, ct);
        var columnType = CardGroup.GetGroupableColumn(selectedCards);

        await using var transaction = await cardGroupRepository.BeginTransactionAsync(ct);

        var newGroup = CardGroup.Create(req.SprintId, columnType, req.Title);
        await cardGroupRepository.AddAsync(newGroup, ct);
        await cardGroupRepository.SaveChangesAsync(ct);

        await cardRegrouping.MoveCardsAsync(req.SprintId, selectedCards, newGroup, ct);

        await cardGroupRepository.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        await SendAsync(CardGroupResponseBuilder.Build(newGroup, selectedCards, sprint), 201, ct);
    }
}
