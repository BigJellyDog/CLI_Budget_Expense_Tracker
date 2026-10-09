using CLI_Budget_Expense_Tracker;
using CLI_Budget_Expense_Tracker.Models;

public class BudgetService
{
    private readonly Dictionary<Categories, BudgetLimit> _limits = [];

    public void SetMonthlyLimit(Categories category, decimal monthlyAmount)
    {
        if (!Enum.IsDefined(category))
            throw new ArgumentOutOfRangeException(nameof(monthlyAmount));

        _limits[category] = new BudgetLimit(category, monthlyAmount);
    }

    public BudgetLimit? GetMonthlyLimit(Categories category)
    {
        return _limits.GetValueOrDefault(category);
    }

    /// <summary>
    /// Calculates how much spending exceeds the monthly category limit.
    /// </summary>
    /// <param name="category">The category to check.</param>
    /// <param name="date">The date that determines the month and year to check.</param>
    /// <param name="expenses">The expenses included in the calculation.</param>
    /// <returns>
    /// The amount exceeding the limit, or zero if no limit is configured
    /// or spending does not exceed it.
    /// </returns>
    public decimal GetMonthlyOverspend(
        Categories category,
        DateTime date,
        IReadOnlyList<Expense> expenses)
    {
        var limit = GetMonthlyLimit(category);

        // A category without a configured limit has no budget restriction.
        if (limit is null) return 0m;

        // Match both month and year to avoid combining expenses from different years.
        var monthlyTotal = expenses.Where(expense =>
            expense.Category == category && expense.Date.Year == date.Year &&
            expense.Date.Month == date.Month).Sum(expense => expense.Amount);

        // Spending below the limit should return zero instead of a negative amount.
        return Math.Max(0m, monthlyTotal - limit.MonthlyAmount);
    }
    
    /// <summary>
    /// Returns a snapshot of all configured category limits.
    /// </summary>
    /// <returns>A collection containing the current budget limits.</returns>
    public IReadOnlyList<BudgetLimit> GetAllLimits()
    {
        // Copy the values so callers cannot modify the internal dictionary.
        return [.. _limits.Values];
    }
}