namespace ExpenseTracker.App.Models;

using CLI_Budget_Expense_Tracker.Core.Models;
using ExpenseTracker.App.Services;

/// <summary>A category in the list and in the detail card: its color, what was spent in it this month, its links.</summary>
public sealed record CategoryTile(
    Category Category,
    int Count,
    decimal Amount,
    decimal Share,
    string Currency,
    Color Color,
    decimal? Limit = null,
    int RuleCount = 0,
    bool IsSelected = false)
{
    public string Name => Category.Name;

    public Color Tint => Color.WithAlpha(0.16f);

    public bool IsUnused => Count == 0;

    public string AmountText => $"{Amount:N2} {Currency}".TrimEnd();

    public string ShareText => $"{Math.Round(Share * 100)}%";

    public string CountText => Count.ToString();

    public string AverageText => $"{(Count > 0 ? Amount / Count : 0):N2} {Currency}".TrimEnd();

    public string BudgetText => Limit is { } limit
        ? Loc.F("Limit {0} · spent {1}", $"{limit:N2} {Currency}".TrimEnd(), AmountText)
        : Loc.F("No limit set for {0}", Name);

    public string BudgetButtonText => Loc.T(Limit is null ? "Set limit" : "Open budgets");

    public string RulesText => RuleCount > 0
        ? Loc.F("{0} rules assign entries to {1}", RuleCount, Name)
        : Loc.F("No rules assign entries to {0}", Name);
}

/// <summary>One color of the palette in the detail card.</summary>
public sealed record SwatchItem(int Slot, Color Color, bool IsSelected, bool IsCustom = false, bool ShowPlus = false);
