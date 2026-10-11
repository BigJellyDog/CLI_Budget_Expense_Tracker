namespace CLI_Budget_Expense_Tracker.Core.Models;

/// <summary>
/// Represents an expense with an explicit date and category.
/// </summary>
public sealed class Expense
{
    public Guid Id { get; private set; }
    public decimal Amount { get; private set; }
    public Guid CategoryId { get; private set; }
    public DateOnly Date { get; private set; }
    public string Description { get; private set; }
    public bool IsDeleted { get; private set; }

    public Expense(
        Guid id,
        decimal amount,
        Guid categoryId,
        DateOnly date,
        string description = "")
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

        MoneyRules.Validate(amount, nameof(amount), allowZero: false);
        ArgumentNullException.ThrowIfNull(description);

        Id = id;
        Amount = amount;
        CategoryId = categoryId;
        Date = date;
        Description = description;
    }

    /// <summary>
    /// Marks the expense as deleted while preserving its identity.
    /// </summary>
    public void MarkAsDeleted()
    {
        IsDeleted = true;
    }
}
