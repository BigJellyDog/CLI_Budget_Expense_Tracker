namespace CLI_Budget_Expense_Tracker.Core.Models;

/// <summary>Counts the records that reference a category.</summary>
/// <param name="ActiveExpenses">Expenses shown in the app.</param>
/// <param name="DeletedExpenses">Deleted expenses kept for history and import protection.</param>
public sealed record CategoryUsage(
    int ActiveExpenses,
    int DeletedExpenses,
    int RecurringExpenses,
    int BudgetLimits)
{
    public bool IsUsed =>
        ActiveExpenses + DeletedExpenses + RecurringExpenses + BudgetLimits > 0;
}
