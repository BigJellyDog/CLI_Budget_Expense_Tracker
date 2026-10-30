namespace ExpenseTracker.App.Models;

using ExpenseTracker.App.Services;

public sealed record BudgetListItem(
    Guid CategoryId,
    string CategoryName,
    decimal Limit,
    decimal Spent,
    string Currency = "")
{
    public bool IsOverBudget => Spent > Limit;

    public string Summary =>
        Loc.F("Spent: {0} / Limit: {1}", Money(Spent), Money(Limit));

    public string BalanceText => IsOverBudget
        ? Loc.F("Over budget: {0}", Money(Spent - Limit))
        : Loc.F("Remaining: {0}", Money(Limit - Spent));

    private string Money(decimal amount) => $"{amount:N2} {Currency}".TrimEnd();

    public double Progress => Spent >= Limit
        ? (Spent > 0 ? 1d : 0d)
        : (double)(Spent / Limit);
}