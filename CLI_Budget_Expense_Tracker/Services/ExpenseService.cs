namespace CLI_Budget_Expense_Tracker.Services;

public class ExpenseService
{
    // Create a list with objects inside called with <>
    // _expenses ist the name of the list 
    private readonly List<Expense> _expenses = [];

    public void AddExpense(Expense expense)
    {
        _expenses.Add(expense);
    }

    public IReadOnlyList<Expense> GetAllExpenses()
    {
        return _expenses.AsReadOnly();
    }

    public decimal GetTotalAmount()
    {
        return _expenses.Sum(expense => expense.Amount);
    }

    public IReadOnlyList<Expense> GetExpensesByCategory(Categories category)
    {
        return [.. _expenses.Where(expense => expense.Category == category)];
    }

    public IReadOnlyList<Expense> GetExpensesByPeriod(
        DateTime startDate,
        DateTime endDate)
    {
        return
        [
            .. _expenses.Where(expense =>
                expense.Date.Date >= startDate.Date &&
                expense.Date.Date <= endDate.Date)];
    }

    public decimal GetTotalAmount(DateTime startDate, DateTime endDate)
    {
        return GetExpensesByPeriod(startDate, endDate).Sum(expense => expense.Amount);
    }

    public IReadOnlyDictionary<Categories, decimal> GetTotalsByCategory(
        DateTime startDate,
        DateTime endDate)
    {
        var expenses = GetExpensesByPeriod(startDate, endDate);

        var groups = expenses.GroupBy(expense => expense.Category);

        return groups.ToDictionary(
            group => group.Key,
            group => group.Sum(expense => expense.Amount));
    }

    public IReadOnlyList<Categories> GetTopCategories(
        DateTime startDate,
        DateTime endDate)
    {
        var totalsByCategory = GetTotalsByCategory(startDate, endDate);

        if (totalsByCategory.Count == 0) return [];

        var maxAmount = totalsByCategory.Values.Max();

        return
        [
            .. totalsByCategory
                .Where(categoryTotal => categoryTotal.Value == maxAmount)
                .Select(categoryTotal => categoryTotal.Key)
        ];
    }

}