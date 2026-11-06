using System.Text.Json;
using Fokus.Core;

int passed = 0;
void Check(bool condition, string description)
{
    if (!condition) throw new Exception(description);
    Console.WriteLine("PASS " + description);
    passed++;
}
var now = DateTimeOffset.Parse("2026-11-06T10:00:00Z");
var session = new FocusSession { EndsAt = now.AddMinutes(1), BlockedExecutables = ["C:\\Example\\App.exe"] };
Check(session.IsActive(now), "Session starts active");
Check(!session.IsActive(now.AddMinutes(1)), "Session expires at deadline");
Check(session.Remaining(now.AddMinutes(2)) == TimeSpan.Zero, "Remaining time never becomes negative");
Check(!session.CanCancel(now), "Cannot cancel without request");
session.RequestCancel(now);
Check(!session.CanCancel(now.AddSeconds(29)), "Cannot cancel before 30 seconds");
Check(session.CanCancel(now.AddSeconds(30)), "Can cancel at 30 seconds");
session.RequestCancel(now.AddSeconds(10));
Check(session.CanCancel(now.AddSeconds(30)), "Repeated request does not postpone cancellation");
var restored = JsonSerializer.Deserialize<FocusSession>(JsonSerializer.Serialize(session))!;
Check(restored.EndsAt == session.EndsAt && restored.CanCancel(now.AddSeconds(30)) && restored.BlockedExecutables.SequenceEqual(session.BlockedExecutables), "Restart preserves session and cancellation deadlines and block list");
Check(!restored.IsActive(now.AddHours(1)), "Sleep or restart beyond deadline ends session");
session.KeepFocusing();
Check(!session.CanCancel(now.AddHours(1)), "Continuing clears cancellation request");
Console.WriteLine($"{passed} checks passed.");
