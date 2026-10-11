namespace CLI_Budget_Expense_Tracker.Core.Models;

public sealed record BudgetStatus(Guid CategoryId, DateOnly Month, decimal? Limit, decimal Spent)
{
    public decimal Overspend => Limit.HasValue ? Math.Max(0, Spent - Limit.Value) : 0;
}
