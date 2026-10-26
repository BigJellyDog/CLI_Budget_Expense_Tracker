namespace CLI_Budget_Expense_Tracker.Infrastructure.Data;

/// <summary>
/// Remembers the bank record of an expense that was permanently deleted,
/// so importing the same statement again does not bring it back.
/// </summary>
public sealed class RemovedImportHash
{
    public required string Hash { get; init; }
}
