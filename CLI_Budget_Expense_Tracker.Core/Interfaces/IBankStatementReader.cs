namespace CLI_Budget_Expense_Tracker.Core.Interfaces;

using Models;

/// <summary>
/// Reads bank statement records without saving expenses.
/// </summary>
public interface IBankStatementReader
{
    IReadOnlyList<BankStatementEntry> Read(string filePath);

    /// <summary>
    /// Reads from the current position and leaves the stream open.
    /// </summary>
    IReadOnlyList<BankStatementEntry> Read(Stream stream);
}