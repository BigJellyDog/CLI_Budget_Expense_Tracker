namespace CLI_Budget_Expense_Tracker.Core.Models;

/// <summary>
/// Represents a parsed bank statement record before expense classification.
/// </summary>
public sealed record BankStatementEntry(
    int RowNumber,
    string Type,
    string Product,
    DateTime StartedAt,
    DateTime? CompletedAt,
    string Description,
    decimal Amount,
    decimal Fee,
    string Currency,
    string Status,
    decimal? Balance);