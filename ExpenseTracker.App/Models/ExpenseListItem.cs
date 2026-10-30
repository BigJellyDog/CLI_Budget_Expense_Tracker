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
    public string Title => string.IsNullOrWhiteSpace(Description) ? CategoryName : Description;

    public string AmountText => $"{Amount:N2} {Currency}".TrimEnd();

    /// <summary>Amount with a minus sign, as it is shown in the list.</summary>
    public string SpentText => $"−{Amount:N2} {Currency}".TrimEnd();
}

/// <summary>One slice of the spending bar.</summary>
public sealed record BarSegment(string CategoryName, decimal Value);

/// <summary>Expenses of one day, with the heading shown above them.</summary>
public sealed class ExpenseDayGroup(string title, IEnumerable<ExpenseListItem> items)
    : List<ExpenseListItem>(items)
{
    public string Title { get; } = title;

    public bool HasTitle => Title.Length > 0;
}
