namespace CLI_Budget_Expense_Tracker.Core.Services;

using Interfaces;
using Models;

/// <summary>
/// Manages recurring expense schedules.
/// </summary>
public sealed class RecurringExpenseService(
    IRecurringExpenseRepository recurringExpenses,
    ICategoryRepository categories)
{
    public Task<IReadOnlyList<RecurringExpense>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return recurringExpenses.GetAllAsync(cancellationToken);
    }

    /// <summary>
    /// Creates a schedule for an existing category.
    /// </summary>
    public async Task<RecurringExpense> AddAsync(
        decimal amount,
        Guid categoryId,
        RecurrenceFrequency frequency,
        DateOnly startDate,
        string description = "",
        CancellationToken cancellationToken = default)
    {
        var recurringExpense = new RecurringExpense(
            Guid.NewGuid(),
            amount,
            categoryId,
            frequency,
            startDate,
            description);

        var category = await categories.GetByIdAsync(
            categoryId,
            cancellationToken);

        if (category is null)
        {
            throw new InvalidOperationException(
                "Category not found.");
        }

        await recurringExpenses.AddAsync(
            recurringExpense,
            cancellationToken);

        return recurringExpense;
    }

    /// <summary>
    /// Deactivates an existing schedule without deleting its history.
    /// </summary>
    public async Task DeactivateAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var recurringExpense = await recurringExpenses.GetByIdAsync(
            id,
            cancellationToken);

        if (recurringExpense is null)
        {
            throw new InvalidOperationException(
                "Recurring expense not found.");
        }

        if (!recurringExpense.IsActive)
        {
            return;
        }

        recurringExpense.Deactivate();

        await recurringExpenses.UpdateAsync(
            recurringExpense,
            cancellationToken);
    }
}