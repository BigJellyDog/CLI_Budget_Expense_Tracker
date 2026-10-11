namespace CLI_Budget_Expense_Tracker.Core.Models;

/// <summary>
/// Preserves the source record and explains its preliminary classification.
/// </summary>
public sealed record ClassifiedBankEntry(
    BankStatementEntry Source,
    BankEntryKind Kind,
    string Reason);