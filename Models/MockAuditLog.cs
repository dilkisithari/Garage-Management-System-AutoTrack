namespace AutoTrack.Models;

public static class MockAuditLog
{
    private static readonly object Sync = new();
    private static readonly List<AuditLogEntry> Entries = new();

    public static void Add(string username, string action, string details)
    {
        lock (Sync)
        {
            Entries.Add(new AuditLogEntry
            {
                At = DateTime.Now,
                Username = username,
                Action = action,
                Details = details,
                TableName = "APPLICATION"
            });
        }
    }

    public static List<AuditLogEntry> All()
    {
        lock (Sync)
        {
            return Entries.OrderByDescending(entry => entry.At)
                .Select(entry => new AuditLogEntry
                {
                    At = entry.At,
                    Username = entry.Username,
                    Action = entry.Action,
                    TableName = entry.TableName,
                    Details = entry.Details
                })
                .ToList();
        }
    }
}
