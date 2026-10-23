using Microsoft.AspNetCore.Authorization;
using Reflekt.API.Auth;

namespace Reflekt.API.Features.Reports.GetParticipantReport;

[Authorize(Policy = Auth.Policies.SignedInUserOrGuest)]
[HttpGet("/api/sprints/{sprintId}/report")]
[Tags("Reports")]
public sealed class GetParticipantReportEndpoint(SprintAccess sprintAccess, SprintReportBuilder reportBuilder)
    : Endpoint<GetParticipantReportRequest, ReportResponse>
{
    public override async Task HandleAsync(GetParticipantReportRequest req, CancellationToken ct)
    {
        var access = await sprintAccess.ForSprintAsync(User, req.SprintId, ct);
        var sprint = access.RequireSprint();

        if (access.IsGuest)
            access.RequireGuestOf(req.SprintId);
        else
            access.RequireOwner();

        await SendOkAsync(await reportBuilder.BuildAsync(sprint, access.Team!, User, ct), ct);
    }
}
