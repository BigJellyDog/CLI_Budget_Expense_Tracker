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
        DateOnly? endDate = null,
        CancellationToken cancellationToken = default)
    {
        var recurringExpense = new RecurringExpense(
            Guid.NewGuid(),
            amount,
            categoryId,
            frequency,
            startDate,
            description,
            endDate);

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

    /// <summary>
    /// Changes a schedule's amount, category, frequency, description and end date.
    /// </summary>
    public async Task UpdateAsync(
        Guid id,
        decimal amount,
        Guid categoryId,
        RecurrenceFrequency frequency,
        string description,
        DateOnly? endDate,
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

        if (await categories.GetByIdAsync(categoryId, cancellationToken) is null)
        {
            throw new InvalidOperationException(
                "Category not found.");
        }

        recurringExpense.Update(
            amount,
            categoryId,
            frequency,
            description,
            endDate);

        await recurringExpenses.UpdateAsync(
            recurringExpense,
            cancellationToken);
    }

    /// <summary>
    /// Resumes a deactivated schedule from the first occurrence on or after today.
    /// </summary>
    public async Task ReactivateAsync(
        Guid id,
        DateOnly today,
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

        if (recurringExpense.IsActive)
        {
            return;
        }

        recurringExpense.Reactivate(today);

        if (!recurringExpense.IsActive)
        {
            throw new InvalidOperationException(
                "The end date has passed. Edit the schedule and change the end date first.");
        }

        await recurringExpenses.UpdateAsync(
            recurringExpense,
            cancellationToken);
    }

    /// <summary>
    /// Deletes an inactive schedule. Expenses it already generated are kept.
    /// </summary>
    public async Task DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var recurringExpense = await recurringExpenses.GetByIdAsync(
            id,
            cancellationToken);

        if (recurringExpense is null)
        {
            return;
        }

        if (recurringExpense.IsActive)
        {
            throw new InvalidOperationException(
                "Deactivate the schedule before deleting it.");
        }

        await recurringExpenses.DeleteAsync(id, cancellationToken);
    }
}