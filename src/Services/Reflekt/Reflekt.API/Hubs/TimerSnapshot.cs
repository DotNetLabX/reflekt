namespace Reflekt.API.Hubs;

public enum TimerStatus { Stopped, Running, Paused, Expired }

public sealed record TimerSnapshot(
    TimerStatus Status,
    int RemainingSeconds,
    DateTime? EndTimeUtc)
{
    public static readonly TimerSnapshot Stopped = new(TimerStatus.Stopped, 0, null);

    public static readonly TimerSnapshot Expired = new(TimerStatus.Expired, 0, null);

    public object ToPayload() => new
    {
        status = Status.ToString().ToLowerInvariant(),
        remainingSeconds = RemainingSeconds,
        endTimeUtc = EndTimeUtc,
    };
}
