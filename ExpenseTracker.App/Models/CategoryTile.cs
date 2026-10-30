namespace ExpenseTracker.App.Models;

using CLI_Budget_Expense_Tracker.Core.Models;
using ExpenseTracker.App.Services;

/// <summary>A category as a tile: its color, and what was spent in it this month.</summary>
public sealed record CategoryTile(
    Category Category,
    int Count,
    decimal Amount,
    decimal Share,
    string Currency,
    Color Color)
{
    public string Name => Category.Name;

    public Color Tint => Color.WithAlpha(0.16f);

    public string CountText => Loc.F("{0} entries", Count);

    public string AmountText => $"{Amount:N2} {Currency}".TrimEnd();

    public string ShareText => Loc.F("{0} % of the month", Math.Round(Share * 100));
}

/// <summary>One color of the palette in the "new category" row.</summary>
public sealed record SwatchItem(int Slot, Color Color, bool IsSelected, bool IsCustom = false, bool ShowPlus = false);
