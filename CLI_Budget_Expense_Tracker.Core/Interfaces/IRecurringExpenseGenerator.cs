namespace CLI_Budget_Expense_Tracker.Core.Interfaces;

/// <summary>
/// Generates due expenses and returns the number of newly created records.
/// </summary>
public interface IRecurringExpenseGenerator
{
    Task<int> GenerateAsync(
        DateOnly? throughDate = null,
        CancellationToken cancellationToken = default);
}