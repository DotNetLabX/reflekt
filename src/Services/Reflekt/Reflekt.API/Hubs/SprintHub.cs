using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Auth;

namespace Reflekt.API.Hubs;

[Authorize(AuthenticationSchemes = GuestTokenDefaults.AuthenticationScheme)]
public class SprintHub(ILogger<SprintHub> logger) : Hub
{
    public override Task OnConnectedAsync()
    {
        logger.LogInformation("Client {ConnectionId} connected to SprintHub", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public Task JoinSprint(int sprintId)
    {
        var claimSprintId = Context.User?.FindFirstValue("sprintId");
        if (!int.TryParse(claimSprintId, out var parsedSprintId) || parsedSprintId != sprintId)
            throw new HubException("Not authorized to join this sprint.");

        return Groups.AddToGroupAsync(Context.ConnectionId, $"sprint:{sprintId}");
    }

    public Task LeaveSprint(int sprintId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sprint:{sprintId}");

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var participantIdClaim = Context.User?.FindFirstValue("participantId");
        var sprintIdClaim = Context.User?.FindFirstValue("sprintId");

        if (int.TryParse(participantIdClaim, out var participantId) &&
            int.TryParse(sprintIdClaim, out var sprintId))
        {
            await Clients.Group($"sprint:{sprintId}")
                .SendAsync("ParticipantLeft", new { participantId });
        }

        await base.OnDisconnectedAsync(exception);
    }
}
