namespace ExpenseTracker.App.Models;

public sealed record BudgetListItem(
    Guid CategoryId,
    string CategoryName,
    decimal Limit,
    decimal Spent,
    string Currency = "")
{
    public bool IsOverBudget => Spent > Limit;

    public string Summary =>
        $"Spent: {Money(Spent)} / Limit: {Money(Limit)}";

    public string BalanceText => IsOverBudget
        ? $"Over budget: {Money(Spent - Limit)}"
        : $"Remaining: {Money(Limit - Spent)}";

    private string Money(decimal amount) => $"{amount:N2} {Currency}".TrimEnd();

    public double Progress => Spent >= Limit
        ? (Spent > 0 ? 1d : 0d)
        : (double)(Spent / Limit);
}