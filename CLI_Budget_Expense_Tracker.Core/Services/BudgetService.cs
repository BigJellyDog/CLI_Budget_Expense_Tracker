namespace CLI_Budget_Expense_Tracker.Core.Services;
using Models;

public class BudgetService
{
    private readonly Dictionary<LegacyCategories, LegacyBudgetLimit> _limits = [];

    public void SetMonthlyLimit(LegacyCategories legacyCategory, decimal monthlyAmount)
    {
        // Reject negative limits before changing the stored value.
        if (monthlyAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(monthlyAmount),
                monthlyAmount,
                "Monthly limit cannot be negative.");
        }
        if (!Enum.IsDefined(legacyCategory))
            throw new ArgumentOutOfRangeException(nameof(monthlyAmount));

        _limits[legacyCategory] = new LegacyBudgetLimit(legacyCategory, monthlyAmount);
    }

    public LegacyBudgetLimit? GetMonthlyLimit(LegacyCategories legacyCategory)
    {
        return _limits.GetValueOrDefault(legacyCategory);
    }

    /// <summary>
    /// Calculates how much spending exceeds the monthly legacyCategory limit.
    /// </summary>
    /// <param name="legacyCategory">The legacyCategory to check.</param>
    /// <param name="date">The date that determines the month and year to check.</param>
    /// <param name="expenses">The expenses included in the calculation.</param>
    /// <returns>
    /// The amount exceeding the limit, or zero if no limit is configured
    /// or spending does not exceed it.
    /// </returns>
    public decimal GetMonthlyOverspend(
        LegacyCategories legacyCategory,
        DateTime date,
        IReadOnlyList<LegacyExpense> expenses)
    {
        var limit = GetMonthlyLimit(legacyCategory);

        // A legacyCategory without a configured limit has no budget restriction.
        if (limit is null) return 0m;

        // Match both month and year to avoid combining expenses from different years.
        var monthlyTotal = expenses.Where(expense =>
            expense.LegacyCategory == legacyCategory && expense.Date.Year == date.Year &&
            expense.Date.Month == date.Month).Sum(expense => expense.Amount);

        // Spending below the limit should return zero instead of a negative amount.
        return Math.Max(0m, monthlyTotal - limit.MonthlyAmount);
    }
    
    /// <summary>
    /// Returns a snapshot of all configured legacyCategory limits.
    /// </summary>
    /// <returns>A collection containing the current budget limits.</returns>
    public IReadOnlyList<LegacyBudgetLimit> GetAllLimits()
    {
        // Copy the values so callers cannot modify the internal dictionary.
        return [.. _limits.Values];
    }
}