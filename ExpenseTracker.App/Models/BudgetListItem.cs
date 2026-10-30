namespace ExpenseTracker.App.Models;

using ExpenseTracker.App.Services;

/// <summary>A category in the budget grid: with a limit for the month, or without one.</summary>
public sealed record BudgetListItem(
    Guid CategoryId,
    string CategoryName,
    decimal Limit,
    decimal Spent,
    string Currency = "",
    bool HasLimit = true,
    Color? Color = null)
{
    public bool HasNoLimit => !HasLimit;

    public Color? Tint => Color?.WithAlpha(0.16f);

    public bool IsOverBudget => HasLimit && Spent > Limit;

    /// <summary>How much of the limit is used: 1 is the whole limit.</summary>
    public double Ratio => Limit > 0 ? (double)(Spent / Limit) : Spent > 0 ? 1d : 0d;

    public double Progress => Math.Min(Ratio, 1d);

    /// <summary>Danger over the limit, Warning from 90 %, Success below.</summary>
    public string StatusTone => IsOverBudget ? "Danger" : Ratio >= 0.9 ? "Warning" : "Success";

    public string StatusText => IsOverBudget
        ? Loc.T("Over limit")
        : Ratio >= 0.9 ? Loc.T("Almost reached") : Loc.T("Within limit");

    public string AmountText => Money(Spent);

    public string LimitText => Loc.F("of {0}", Money(Limit));

    public string FooterText => IsOverBudget
        ? Loc.F("{0} over the limit", Money(Spent - Limit))
        : Loc.F("{0} left", Money(Limit - Spent));

    public string PercentText => $"{Math.Round(Ratio * 100):0} %";

    public string NoLimitText => Loc.F("No limit set · {0} spent", Money(Spent));

    private string Money(decimal amount) => $"{amount:N2} {Currency}".TrimEnd();
}
