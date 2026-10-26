namespace CLI_Budget_Expense_Tracker.Core.Interfaces;

using Models;

/// <summary>
/// Provides persistence operations for recurring expense schedules.
/// </summary>
public interface IRecurringExpenseRepository
{
    Task<RecurringExpense?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecurringExpense>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns active schedules due on or before the specified date.
    /// </summary>
    Task<IReadOnlyList<RecurringExpense>> GetDueAsync(
        DateOnly throughDate,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        RecurringExpense recurringExpense,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        RecurringExpense recurringExpense,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a schedule. Expenses it generated are kept but no longer linked to it.
    /// </summary>
    Task<bool> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}