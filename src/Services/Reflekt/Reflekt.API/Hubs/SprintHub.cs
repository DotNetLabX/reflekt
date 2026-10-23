using System.Security.Claims;
using Blocks.Domain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Reflekt.API.Auth;
using Reflekt.Domain.Sprints;
using Reflekt.Persistence.Repositories;

namespace Reflekt.API.Hubs;

[Authorize(AuthenticationSchemes = GuestTokenDefaults.AuthenticationScheme)]
public class SprintHub(
    ILogger<SprintHub> logger,
    TimerService timerService,
    ReadinessTracker readinessTracker,
    SprintAccess sprintAccess,
    SprintRepository sprintRepository,
    ParticipantRepository participantRepository) : Hub
{
    private const string ParticipantRemovedMessage = "Participant has been removed.";
    private const string ScrumMasterOnlyMessage = "Only the Scrum Master can control the timer.";
    private const string SprintNotFoundMessage = "Sprint not found.";

    public static string ParticipantGroup(int participantId) => $"participant:{participantId}";

    public override Task OnConnectedAsync()
    {
        logger.LogInformation("Client {ConnectionId} connected to SprintHub", Context.ConnectionId);
        return base.OnConnectedAsync();
    }

    public async Task JoinSprint(int sprintId)
    {
        if (Context.User?.GetSprintId() != sprintId)
            throw new HubException("Not authorized to join this sprint.");

        if (Context.User?.GetParticipantId() is not { } participantId)
            throw new HubException("Missing participant claims.");

        await RequireActiveParticipantAsync(participantId);

        readinessTracker.RegisterConnection(Context, sprintId, participantId);

        await Groups.AddToGroupAsync(Context.ConnectionId, ParticipantGroup(participantId));
        await Groups.AddToGroupAsync(Context.ConnectionId, $"sprint:{sprintId}");

        // A kick committed before this read is undone here; one committed after it finds this
        // connection in the registry, so its handler evicts and aborts it.
        if (!await participantRepository.IsActiveAsync(participantId, Context.ConnectionAborted))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sprint:{sprintId}");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ParticipantGroup(participantId));
            readinessTracker.UnregisterConnection(sprintId, Context.ConnectionId);
            throw new HubException(ParticipantRemovedMessage);
        }

        await SendJoinSnapshotsAsync(sprintId);
    }

    private async Task SendJoinSnapshotsAsync(int sprintId)
    {
        var readiness = readinessTracker.GetSnapshot(sprintId);
        await Clients.Caller.SendAsync("ReadinessSnapshot", new
        {
            readyParticipantIds = readiness.ReadyParticipantIds,
            readyCount = readiness.ReadyCount,
            totalCount = readiness.TotalCount,
        });

        var timerSnapshot = timerService.GetSnapshot(sprintId);
        if (timerSnapshot.Status == TimerStatus.Stopped)
        {
            var sprint = await sprintRepository.GetByIdAsync(sprintId, Context.ConnectionAborted);
            if (sprint is not null && sprint.State != SprintState.Closed && sprint.HasTimer)
            {
                var durationSeconds = sprint.GetTimerDefaultForPhase() * 60;
                timerSnapshot = timerService.Start(sprintId, durationSeconds);
                await Clients.Group($"sprint:{sprintId}").SendAsync("TimerUpdated", timerSnapshot.ToPayload());
            }
        }

        await Clients.Caller.SendAsync("TimerSnapshot", timerSnapshot.ToPayload());
    }

    public async Task LeaveSprint(int sprintId)
    {
        readinessTracker.LeaveSprint(sprintId, Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"sprint:{sprintId}");

        if (Context.User?.GetParticipantId() is { } participantId)
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ParticipantGroup(participantId));
    }

    public async Task ToggleReady(int sprintId)
    {
        if (Context.User?.GetSprintId() != sprintId)
            throw new HubException("Not authorized for this sprint.");

        var displayNameClaim = Context.User?.FindFirstValue(GuestClaims.DisplayName);
        if (Context.User?.GetParticipantId() is not { } participantId || displayNameClaim is null)
            throw new HubException("Missing participant claims.");

        await RequireActiveParticipantAsync(participantId);

        var toggle = readinessTracker.ToggleReady(sprintId, participantId, displayNameClaim);

        await Clients.Group($"sprint:{sprintId}").SendAsync("ReadinessChanged", new
        {
            participantId,
            displayName = displayNameClaim,
            isReady = toggle.IsReady,
            readyCount = toggle.ReadyCount,
            totalCount = toggle.TotalCount,
        });
    }

    public async Task StartTimer(int sprintId)
    {
        var sprint = await RequireScrumMasterAsync(sprintId);
        EnsureTimerAvailable(sprint);
        var snapshot = timerService.Start(sprintId, sprint.GetTimerDefaultForPhase() * 60);
        await BroadcastTimerUpdated(sprintId, snapshot);
    }

    public async Task PauseTimer(int sprintId)
    {
        EnsureTimerAvailable(await RequireScrumMasterAsync(sprintId));
        var snapshot = timerService.Pause(sprintId);
        await BroadcastTimerUpdated(sprintId, snapshot);
    }

    public async Task ResumeTimer(int sprintId)
    {
        EnsureTimerAvailable(await RequireScrumMasterAsync(sprintId));
        var snapshot = timerService.Resume(sprintId);
        await BroadcastTimerUpdated(sprintId, snapshot);
    }

    public async Task ResetTimer(int sprintId)
    {
        var sprint = await RequireScrumMasterAsync(sprintId);
        EnsureTimerAvailable(sprint);
        var snapshot = timerService.Reset(sprintId, sprint.GetTimerDefaultForPhase() * 60);
        await BroadcastTimerUpdated(sprintId, snapshot);
    }

    public async Task AddTime(int sprintId, int minutes)
    {
        if (minutes is not (1 or 2 or 5))
            throw new HubException("Invalid minutes value. Allowed: 1, 2, 5.");
        EnsureTimerAvailable(await RequireScrumMasterAsync(sprintId));
        var snapshot = timerService.AddTime(sprintId, minutes);
        await BroadcastTimerUpdated(sprintId, snapshot);
    }

    private async Task<Sprint> RequireScrumMasterAsync(int sprintId)
    {
        if (Context.User?.GetSprintId() != sprintId)
            throw new HubException("Not authorized for this sprint.");

        if (Context.User?.GetParticipantId() is not { } participantId)
            throw new HubException("Missing participant claims.");

        var participant = await participantRepository.GetByIdAsync(participantId, Context.ConnectionAborted);
        if (participant is null || participant.IsRemoved)
            throw new HubException(ParticipantRemovedMessage);

        var resolvedUserId = participant.UserId ?? await FindCookieUserIdAsync();
        if (resolvedUserId is null)
            throw new HubException(ScrumMasterOnlyMessage);

        var access = await sprintAccess.ForSprintAsync(Context.User!, sprintId, Context.ConnectionAborted);
        var sprint = access.Sprint ?? throw new HubException(SprintNotFoundMessage);

        if (!access.IsOwnedBy(resolvedUserId))
            throw new HubException(ScrumMasterOnlyMessage);

        return sprint;
    }

    private async Task<int?> FindCookieUserIdAsync()
    {
        var httpContext = Context.GetHttpContext();
        if (httpContext is null)
            return null;

        var cookieResult = await httpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!cookieResult.Succeeded)
            return null;

        var userIdClaim = cookieResult.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var cookieUserId) ? cookieUserId : null;
    }

    private void EnsureTimerAvailable(Sprint sprint)
    {
        try
        {
            sprint.EnsureOpen();
        }
        catch (DomainException)
        {
            throw new HubException("Sprint is closed.");
        }

        if (!sprint.HasTimer)
            throw new HubException($"Timer is not available in phase {sprint.CurrentPhase}.");
    }

    private async Task RequireActiveParticipantAsync(int participantId)
    {
        if (!await participantRepository.IsActiveAsync(participantId, Context.ConnectionAborted))
            throw new HubException(ParticipantRemovedMessage);
    }

    private Task BroadcastTimerUpdated(int sprintId, TimerSnapshot snapshot) =>
        Clients.Group($"sprint:{sprintId}").SendAsync("TimerUpdated", snapshot.ToPayload());

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        readinessTracker.Forget(Context.ConnectionId);

        if (Context.User?.GetParticipantId() is { } participantId &&
            Context.User?.GetSprintId() is { } sprintId)
        {
            if (readinessTracker.Disconnect(sprintId, Context.ConnectionId, participantId) is { } counts)
            {
                await Clients.Group($"sprint:{sprintId}").SendAsync("ReadinessChanged", new
                {
                    participantId,
                    displayName = (string?)null,
                    isReady = false,
                    readyCount = counts.ReadyCount,
                    totalCount = counts.TotalCount,
                });
            }

            await Clients.Group($"sprint:{sprintId}")
                .SendAsync("ParticipantLeft", new { participantId });
        }

        await base.OnDisconnectedAsync(exception);
    }
}
