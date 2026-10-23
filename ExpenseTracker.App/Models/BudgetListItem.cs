namespace ExpenseTracker.App.Models;

public sealed record BudgetListItem(
    Guid CategoryId,
    string CategoryName,
    decimal Limit,
    decimal Spent)
{
    public bool IsOverBudget => Spent > Limit;

    public string Summary =>
        $"Spent: {Spent:N2} / Limit: {Limit:N2}";

    public string BalanceText => IsOverBudget
        ? $"Over budget: {Spent - Limit:N2}"
        : $"Remaining: {Limit - Spent:N2}";

    public double Progress => Spent >= Limit
        ? (Spent > 0 ? 1d : 0d)
        : (double)(Spent / Limit);
}