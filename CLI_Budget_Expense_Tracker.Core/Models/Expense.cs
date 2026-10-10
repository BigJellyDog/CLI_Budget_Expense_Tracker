namespace CLI_Budget_Expense_Tracker.Core.Models;

/// <summary>
/// Represents an expense with an explicit date and category.
/// </summary>
public sealed class Expense
{
    private const decimal MaxAmount = long.MaxValue / 100m;

    public Guid Id { get; private set; }
    public decimal Amount { get; private set; }
    public Guid CategoryId { get; private set; }
    public DateOnly Date { get; private set; }
    public string Description { get; private set; }
    public ExpenseSource Source { get; private set; }
    public bool IsDeleted { get; private set; }

    public Expense(
        Guid id,
        decimal amount,
        Guid categoryId,
        DateOnly date,
        string description = "",
        ExpenseSource source = ExpenseSource.Manual)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Expense ID must not be empty.", nameof(id));
        }

        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException(
                "Category ID must not be empty.", nameof(categoryId));
        }

        ValidateAmount(amount);
        ArgumentNullException.ThrowIfNull(description);

        if (!Enum.IsDefined(source))
        {
            throw new ArgumentOutOfRangeException(
                nameof(source), source, "Unknown expense source.");
        }

        Id = id;
        Amount = amount;
        CategoryId = categoryId;
        Date = date;
        Description = description;
        Source = source;
    }

    /// <summary>
    /// Marks the expense as deleted while preserving its identity.
    /// </summary>
    public void MarkAsDeleted()
    {
        IsDeleted = true;
    }

    private static void ValidateAmount(decimal amount)
    {
        if (amount <= 0 || amount > MaxAmount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(amount),
                amount,
                "Amount must be positive and fit into a 64-bit integer in cents.");
        }

        if (decimal.Round(amount, 2) != amount)
        {
            throw new ArgumentException(
                "Amount must represent a whole number of cents.",
                nameof(amount));
        }
    }
}