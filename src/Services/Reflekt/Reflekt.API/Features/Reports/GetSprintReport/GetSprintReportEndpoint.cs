using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;

namespace Reflekt.API.Features.Reports.GetSprintReport;

[Authorize(Policy = Auth.Policies.SignedInUserOrGuest)]
[HttpGet("/api/teams/{teamId}/sprints/{sprintId}/report")]
[Tags("Reports")]
public sealed class GetSprintReportEndpoint(SprintAccess sprintAccess, SprintReportBuilder reportBuilder)
    : Endpoint<GetSprintReportRequest, ReportResponse>
{
    public override async Task HandleAsync(GetSprintReportRequest req, CancellationToken ct)
    {
        var access = await sprintAccess.ForTeamAsync(User, req.TeamId, req.SprintId, ct);
        var sprint = access.RequireSprint();

        if (access.IsGuest)
            access.RequireGuestOf(req.SprintId);
        else
            access.RequireOwner();

        await SendOkAsync(await reportBuilder.BuildAsync(sprint, access.Team!, User, ct), ct);
    }
}
