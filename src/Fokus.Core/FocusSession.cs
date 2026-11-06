namespace Fokus.Core;

public sealed class FocusSession
{
    public string Mode { get; set; } = "Lernen";
    public DateTimeOffset EndsAt { get; set; }
    public DateTimeOffset? CancelAvailableAt { get; set; }
    public List<string> BlockedExecutables { get; set; } = [];
    public bool IsActive(DateTimeOffset now) => EndsAt > now;
    public TimeSpan Remaining(DateTimeOffset now) => EndsAt > now ? EndsAt - now : TimeSpan.Zero;
    public void RequestCancel(DateTimeOffset now) => CancelAvailableAt ??= now.AddSeconds(30);
    public void KeepFocusing() => CancelAvailableAt = null;
    public bool CanCancel(DateTimeOffset now) => CancelAvailableAt is { } deadline && now >= deadline;
}
