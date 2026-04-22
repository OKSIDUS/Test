namespace ProzorroAnalytics.Infrastructure.Persistence.Models;

public class SyncStateRow
{
    public int Id { get; init; }
    public DateTimeOffset? LastTenderDateModified { get; init; }
    public DateTimeOffset? LastSyncedAt { get; init; }
}
