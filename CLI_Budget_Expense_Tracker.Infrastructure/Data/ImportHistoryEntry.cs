namespace CLI_Budget_Expense_Tracker.Infrastructure.Data;

/// <summary>
/// Records the successful completion of a one-time data import.
/// </summary>
public sealed class ImportHistoryEntry
{
    public string Id { get; private set; }
    public DateTimeOffset CompletedAtUtc { get; private set; }

    public ImportHistoryEntry(
        string id,
        DateTimeOffset completedAtUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        Id = id;
        CompletedAtUtc = completedAtUtc.ToUniversalTime();
    }
}