namespace ExpenseTracker.App.Models;

public sealed record ExpenseListItem(
    Guid Id,
    DateOnly Date,
    decimal Amount,
    string CategoryName,
    string Description,
    string Currency = "",
    Color? CategoryColor = null)
{
    /// <summary>The category color, softened, as the background of the avatar.</summary>
    public Color? Tint => CategoryColor?.WithAlpha(0.16f);

    public string Title => string.IsNullOrWhiteSpace(Description) ? CategoryName : Description;

    public string AmountText => $"{Amount:N2} {Currency}".TrimEnd();

    /// <summary>Amount with a minus sign, as it is shown in the list.</summary>
    public string SpentText => $"−{Amount:N2} {Currency}".TrimEnd();
}

/// <summary>One slice of the spending bar.</summary>
public sealed record BarSegment(string CategoryName, decimal Value);

/// <summary>Expenses of one day, with the heading shown above them.</summary>
public sealed class ExpenseDayGroup(string title, IEnumerable<ExpenseListItem> items, string currency = "")
    : List<ExpenseListItem>(items)
{
    public string Title { get; } = title;

    public bool HasTitle => Title.Length > 0;

    /// <summary>What was spent that day, shown at the right of the heading.</summary>
    public string TotalText => HasTitle ? $"−{this.Sum(x => x.Amount):N2} {currency}".TrimEnd() : "";
}

/// <summary>A row of the legend under the donut chart.</summary>
public sealed record LegendItem(
    Guid CategoryId,
    string Name,
    string AmountText,
    string PercentText,
    Color Color,
    bool IsActive);

/// <summary>A budget bar of the small card next to the list.</summary>
public sealed record BudgetBarItem(string Name, string PercentText, double Value, string FillKey, string PercentKey);
