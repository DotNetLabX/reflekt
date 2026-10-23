using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Features.Sprints.UpdateColumns;

[Authorize(Policy = Auth.Policies.SignedInUser)]
[HttpPut("/api/sprints/{sprintId}/columns")]
[Tags("Sprints")]
public sealed class UpdateColumnsEndpoint(SprintAccess sprintAccess, SprintRepository sprintRepository)
    : Endpoint<UpdateColumnsCommand, SprintResponse>
{
    public override async Task HandleAsync(UpdateColumnsCommand command, CancellationToken ct)
    {
        var access = await sprintAccess.ForSprintAsync(User, command.SprintId, ct);
        var sprint = access.RequireSprint();
        access.RequireOwner();

        sprint.EnsureOpen();

        sprint.UpdateColumnConfig(
            command.Column1Name, command.Column1Color,
            command.Column2Name, command.Column2Color,
            command.Column3Name, command.Column3Color);

        await sprintRepository.SaveChangesAsync(ct);

        await SendOkAsync(sprint.Adapt<SprintResponse>(), ct);
    }
}
