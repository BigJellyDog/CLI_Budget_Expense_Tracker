namespace CLI_Budget_Expense_Tracker.Core.Models;

public sealed record ExpenseReport(
    IReadOnlyList<Expense> Expenses,
    decimal Total,
    IReadOnlyDictionary<Guid, decimal> TotalsByCategory)
{
    public IReadOnlyList<Guid> TopCategoryIds
    {
        get
        {
            if (TotalsByCategory.Count == 0) return [];
            var maximum = TotalsByCategory.Values.Max();
            return TotalsByCategory.Where(x => x.Value == maximum).Select(x => x.Key).ToArray();
        }
    }
}
