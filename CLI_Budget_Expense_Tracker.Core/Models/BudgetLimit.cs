namespace CLI_Budget_Expense_Tracker.Core.Models;

/// <summary>
/// Represents a spending limit for one category and one calendar month.
/// </summary>
public sealed class BudgetLimit
{
    private const decimal MaxAmount = long.MaxValue / 100m;

    public Guid Id { get; private set; }
    public Guid CategoryId { get; private set; }

    /// <summary>
    /// Gets the budget month represented by its first day.
    /// </summary>
    public DateOnly Month { get; private set; }

    public decimal MonthlyAmount { get; private set; }

    public BudgetLimit(
        Guid id,
        Guid categoryId,
        DateOnly month,
        decimal monthlyAmount)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Budget limit ID must not be empty.", nameof(id));
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException(
                "Category ID must not be empty.", nameof(categoryId));
        }

        SetMonthlyAmount(monthlyAmount);

        Id = id;
        CategoryId = categoryId;
        Month = new DateOnly(month.Year, month.Month, 1);
    }

    /// <summary>
    /// Updates the amount after validation, preserving the existing value on failure.
    /// </summary>
    public void SetMonthlyAmount(decimal monthlyAmount)
    {
        if (monthlyAmount < 0 || monthlyAmount > MaxAmount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(monthlyAmount),
                monthlyAmount,
                "Monthly amount must be non-negative and fit into a 64-bit integer in cents.");
        }

        if (decimal.Round(monthlyAmount, 2) != monthlyAmount)
        {
            throw new ArgumentException(
                "Monthly amount must represent a whole number of cents.",
                nameof(monthlyAmount));
        }

        MonthlyAmount = monthlyAmount;
    }
}