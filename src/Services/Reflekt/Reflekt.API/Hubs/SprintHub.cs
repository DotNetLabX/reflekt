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
        => Groups.AddToGroupAsync(Context.ConnectionId, $"sprint:{sprintId}");

    // TODO(feature=Participants): validate participant belongs to sprint before group join
    public Task LeaveSprint(int sprintId)
        => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sprint:{sprintId}");
}
