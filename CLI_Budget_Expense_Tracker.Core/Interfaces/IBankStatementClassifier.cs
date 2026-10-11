namespace CLI_Budget_Expense_Tracker.Core.Interfaces;

using Models;

public interface IBankStatementClassifier
{
    IReadOnlyList<ClassifiedBankEntry> Classify(
        IReadOnlyList<BankStatementEntry> entries,
        string currency);
}