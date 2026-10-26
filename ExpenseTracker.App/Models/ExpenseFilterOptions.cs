namespace ExpenseTracker.App.Models;

using CLI_Budget_Expense_Tracker.Core.Services;

public enum ExpensePeriod
{
    Month,
    AllTime,
    Custom
}

public sealed record PeriodOption(ExpensePeriod Period, string Name);

public sealed record SortOption(ExpenseSort Sort, string Name);
