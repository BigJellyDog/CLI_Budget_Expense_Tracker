namespace CLI_Budget_Expense_Tracker.Core.Services;

using Models;

public enum ExpenseSort
{
    DateNewestFirst,
    DateOldestFirst,
    AmountHighestFirst,
    AmountLowestFirst
}

/// <summary>
/// Narrows and orders a list of expenses: description search, amount range and sort order.
/// </summary>
public sealed record ExpenseListQuery(
    string? Text = null,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    ExpenseSort Sort = ExpenseSort.DateNewestFirst)
{
    public IReadOnlyList<Expense> Apply(IEnumerable<Expense> expenses)
    {
        ArgumentNullException.ThrowIfNull(expenses);

        var text = Text?.Trim();

        var matches = expenses.Where(x =>
            (string.IsNullOrEmpty(text)
                || x.Description.Contains(text, StringComparison.OrdinalIgnoreCase))
            && (MinAmount is null || x.Amount >= MinAmount)
            && (MaxAmount is null || x.Amount <= MaxAmount));

        // Id as the last key keeps the order stable between refreshes.
        return (Sort switch
        {
            ExpenseSort.DateOldestFirst => matches.OrderBy(x => x.Date).ThenBy(x => x.Id),
            ExpenseSort.AmountHighestFirst => matches.OrderByDescending(x => x.Amount).ThenByDescending(x => x.Date).ThenBy(x => x.Id),
            ExpenseSort.AmountLowestFirst => matches.OrderBy(x => x.Amount).ThenByDescending(x => x.Date).ThenBy(x => x.Id),
            _ => matches.OrderByDescending(x => x.Date).ThenBy(x => x.Id)
        }).ToList();
    }
}
