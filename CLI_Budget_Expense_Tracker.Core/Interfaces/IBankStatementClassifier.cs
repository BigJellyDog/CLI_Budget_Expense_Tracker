namespace CLI_Budget_Expense_Tracker.Core.Interfaces;

using Models;

public interface IBankStatementClassifier
{
    /// <summary>Sorts statement entries into expense candidates, incoming money, entries to review and ones to ignore.</summary>
    IReadOnlyList<ClassifiedBankEntry> Classify(
        IReadOnlyList<BankStatementEntry> entries,
        string currency);
}